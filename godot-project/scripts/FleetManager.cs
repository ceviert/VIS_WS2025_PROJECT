using Godot;
using System.Collections.Generic;
using System.Globalization;

public partial class FleetManager : Node3D
{
    [ExportCategory("Settings")]
    [Export] public PackedScene PlaneScene { get; set; }
    [Export] public double UpdateInterval = 5.0;

    [ExportCategory("Map Config")]
    [Export] public float CenterLat { get => _centerLat; set { _centerLat = value; RequestMapUpdate(); } }
    private float _centerLat = 41.2599f;

    [Export] public float CenterLon { get => _centerLon; set { _centerLon = value; RequestMapUpdate(); } }
    private float _centerLon = 28.7427f;

    [Export] public int RadiusKM { get => _radiusKM; set { _radiusKM = value; RequestMapUpdate(); } }
    private int _radiusKM = 460;

    [Export] public float TextureRotationOffset { get => _textureRotationOffset; set { _textureRotationOffset = value; RequestMapUpdate(); } }
    private float _textureRotationOffset = -90.0f;

    private HttpRequest _httpRequest;
    private Timer _timer;
    private readonly Dictionary<string, PlaneController> _spawnedPlanes = new();
    
    private MeshInstance3D _radiusRingInstance;
    private ImmediateMesh _radiusMesh;

    public override void _Ready()
    {
        _httpRequest = GetNode<HttpRequest>("HTTPRequest");
        _httpRequest.RequestCompleted += OnRequestCompleted;

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = UpdateInterval;
        _timer.Timeout += FetchData;
        _timer.Start();

        SetupRadiusRing();
        DrawMapRadius();
        FetchData();
    }

    private void RequestMapUpdate()
    {
        if (_radiusMesh != null) DrawMapRadius();
    }

    private void FetchData()
    {
        int radiusNm = (int)(RadiusKM * 0.539957);
        string latStr = CenterLat.ToString(CultureInfo.InvariantCulture);
        string lonStr = CenterLon.ToString(CultureInfo.InvariantCulture);
        string url = $"https://api.airplanes.live/v2/point/{latStr}/{lonStr}/{radiusNm}";

        GD.Print($"requesting: {url}");
        _httpRequest.Request(url);
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode != 200) { GD.PrintErr($"API Error: {responseCode}"); return; }

        var json = new Json();
        if (json.Parse(System.Text.Encoding.UTF8.GetString(body)) != Error.Ok) return;

        var data = json.Data.AsGodotDictionary();
        if (!data.ContainsKey("ac")) return;

        var aircraftArray = data["ac"].AsGodotArray();
        GD.Print($"planes returned: {aircraftArray.Count}");

        ProcessAircraftList(aircraftArray);
    }

    private void ProcessAircraftList(Godot.Collections.Array aircraftList)
    {
        HashSet<string> currentFrameIcaos = new HashSet<string>();

        foreach (var acVariant in aircraftList)
        {
            var ac = acVariant.AsGodotDictionary();
            if (!ac.TryGetValue("hex", out var hexVar) || 
                !ac.TryGetValue("lat", out var latVar) || 
                !ac.TryGetValue("lon", out var lonVar)) continue;

            string icao = hexVar.AsString();
            currentFrameIcaos.Add(icao);

            float lat = (float)latVar.AsDouble();
            float lon = (float)lonVar.AsDouble();
            float track = ac.TryGetValue("track", out var trk) ? (float)trk.AsDouble() : 0.0f;
            
            float alt = 0;
            if (ac.TryGetValue("alt_baro", out var ab)) alt = (float)ab.AsDouble();
            else if (ac.TryGetValue("alt_geom", out var ag)) alt = (float)ag.AsDouble();

            if (_spawnedPlanes.TryGetValue(icao, out PlaneController plane))
            {
                plane.UpdatePlaneData(lat, lon, alt, track, TextureRotationOffset);
            }
            else
            {
                SpawnPlane(icao, lat, lon, alt, track);
            }
        }

        List<string> toRemove = new List<string>();
        foreach (var kvp in _spawnedPlanes)
        {
            if (!currentFrameIcaos.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var icao in toRemove)
        {
            _spawnedPlanes[icao].QueueFree(); 
            _spawnedPlanes.Remove(icao);      
        }
    }

    private void SpawnPlane(string icao, float lat, float lon, float alt, float track)
    {
        if (PlaneScene == null) return;
        
        var newPlane = PlaneScene.Instantiate<PlaneController>();
        AddChild(newPlane);
        newPlane.Name = icao;
        newPlane.UpdatePlaneData(lat, lon, alt, track, TextureRotationOffset);
        _spawnedPlanes.Add(icao, newPlane);
    }

    private void SetupRadiusRing()
    {
        _radiusRingInstance = new MeshInstance3D { Name = "DebugRadiusRing" };
        AddChild(_radiusRingInstance);
        _radiusMesh = new ImmediateMesh();
        _radiusRingInstance.Mesh = _radiusMesh;
        var mat = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = Colors.Yellow };
        _radiusRingInstance.MaterialOverride = mat;
    }

    private void DrawMapRadius()
    {
        if (_radiusMesh == null) return;
        _radiusMesh.ClearSurfaces();
        _radiusMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        int segments = 128;
        double angularDistance = RadiusKM / 6371.0;
        float centerLatRad = Mathf.DegToRad(CenterLat);
        float centerLonRad = Mathf.DegToRad(CenterLon);
        for (int i = 0; i <= segments; i++)
        {
            float bearing = Mathf.DegToRad(i * (360.0f / segments));
            float lat2 = Mathf.Asin(Mathf.Sin(centerLatRad) * Mathf.Cos((float)angularDistance) + Mathf.Cos(centerLatRad) * Mathf.Sin((float)angularDistance) * Mathf.Cos(bearing));
            float lon2 = centerLonRad + Mathf.Atan2(Mathf.Sin(bearing) * Mathf.Sin((float)angularDistance) * Mathf.Cos(centerLatRad), Mathf.Cos((float)angularDistance) - Mathf.Sin(centerLatRad) * Mathf.Sin(lat2));
            Vector3 pos = GeoUtils.LatLonToVector3(Mathf.RadToDeg(lat2), Mathf.RadToDeg(lon2), 0, TextureRotationOffset, 1.0f);
            _radiusMesh.SurfaceAddVertex(pos);
        }
        _radiusMesh.SurfaceEnd();
    }
}