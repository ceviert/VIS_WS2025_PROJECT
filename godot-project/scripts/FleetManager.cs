using Godot;
using System.Collections.Generic;
using System.Globalization;

public partial class FleetManager : Node3D
{
    [Export] public PackedScene PlaneScene { get; set; }
    [Export] public double UpdateInterval = 5.0;

    private float _centerLat = 41.2599f;
    [Export] public float CenterLat { get => _centerLat; set { _centerLat = value; if (_radiusMesh != null) DrawMapRadius(); } }

    private float _centerLon = 28.7427f;
    [Export] public float CenterLon { get => _centerLon; set { _centerLon = value; if (_radiusMesh != null) DrawMapRadius(); } }

    private int _radiusKM = 460;
    [Export] public int RadiusKM { get => _radiusKM; set { _radiusKM = value; if (_radiusMesh != null) DrawMapRadius(); } }

    private float _textureRotationOffset = -90.0f;
    [Export] public float TextureRotationOffset { get => _textureRotationOffset; set { _textureRotationOffset = value; if (_radiusMesh != null) DrawMapRadius(); } }

    private HttpRequest _httpRequest;
    private Timer _timer;
    private Dictionary<string, PlaneController> _spawnedPlanes = new Dictionary<string, PlaneController>();

    private MeshInstance3D _radiusRingInstance;
    private ImmediateMesh _radiusMesh;
    private float _earthRadius = 50.0f;

    public override void _Ready()
    {
        _httpRequest = new HttpRequest();
        AddChild(_httpRequest);
        _httpRequest.RequestCompleted += OnRequestCompleted;

        _timer = new Timer();
        AddChild(_timer);
        _timer.WaitTime = UpdateInterval;
        _timer.Timeout += FetchData;
        _timer.Start();

        SetupRadiusRing();
        DrawMapRadius();

        FetchData();
    }

    private void FetchData()
    {
        int radiusNm = (int)(RadiusKM * 0.539957);
        string latStr = CenterLat.ToString(CultureInfo.InvariantCulture);
        string lonStr = CenterLon.ToString(CultureInfo.InvariantCulture);
        string url = $"https://api.airplanes.live/v2/point/{latStr}/{lonStr}/{radiusNm}";

        GD.Print($"Veri isteniyor: {url}");
        _httpRequest.Request(url);
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode != 200) { GD.PrintErr($"API Hatası: {responseCode}"); return; }

        string jsonStr = System.Text.Encoding.UTF8.GetString(body);
        var json = new Json();
        if (json.Parse(jsonStr) != Error.Ok) return;

        var data = json.Data.AsGodotDictionary();
        if (!data.ContainsKey("ac")) return;

        var aircraftList = data["ac"].AsGodotArray();
        ProcessAircraftList(aircraftList);
    }

    private void ProcessAircraftList(Godot.Collections.Array aircraftList)
    {
        HashSet<string> currentFrameIcaos = new HashSet<string>();

        foreach (var acVariant in aircraftList)
        {
            var ac = acVariant.AsGodotDictionary();
            if (!ac.ContainsKey("hex") || !ac.ContainsKey("lat") || !ac.ContainsKey("lon")) continue;

            string icao = ac["hex"].AsString();
            currentFrameIcaos.Add(icao);

            float lat = (float)ac["lat"].AsDouble();
            float lon = (float)ac["lon"].AsDouble();
            float track = ac.ContainsKey("track") ? (float)ac["track"].AsDouble() : 0.0f;

            float alt = 0;
            if (ac.ContainsKey("alt_baro")) alt = (float)ac["alt_baro"].AsDouble();
            else if (ac.ContainsKey("alt_geom")) alt = (float)ac["alt_geom"].AsDouble();

            if (_spawnedPlanes.ContainsKey(icao))
            {
                _spawnedPlanes[icao].UpdateData(icao, lat, lon, alt, track, TextureRotationOffset);
            }
            else
            {
                SpawnPlane(icao, lat, lon, alt, track);
            }
        }

        List<string> toRemove = new List<string>();
        foreach (var icao in _spawnedPlanes.Keys)
        {
            if (!currentFrameIcaos.Contains(icao)) toRemove.Add(icao);
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
        
        PlaneController newPlane = PlaneScene.Instantiate<PlaneController>();
        AddChild(newPlane);
        newPlane.Name = icao;
        newPlane.UpdateData(icao, lat, lon, alt, track, TextureRotationOffset);
        _spawnedPlanes.Add(icao, newPlane);
    }

    private void SetupRadiusRing()
    {
        _radiusRingInstance = new MeshInstance3D();
        _radiusRingInstance.Name = "DebugRadiusRing";
        AddChild(_radiusRingInstance);
        _radiusMesh = new ImmediateMesh();
        _radiusRingInstance.Mesh = _radiusMesh;

        var mat = new StandardMaterial3D();
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.AlbedoColor = new Color(1, 1, 0); // Sarı
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
            float lat2 = Mathf.Asin(Mathf.Sin(centerLatRad) * Mathf.Cos((float)angularDistance) +
                         Mathf.Cos(centerLatRad) * Mathf.Sin((float)angularDistance) * Mathf.Cos(bearing));
            float lon2 = centerLonRad + Mathf.Atan2(Mathf.Sin(bearing) * Mathf.Sin((float)angularDistance) * Mathf.Cos(centerLatRad),
                         Mathf.Cos((float)angularDistance) - Mathf.Sin(centerLatRad) * Mathf.Sin(lat2));

            Vector3 pos = LatLonToVector3(Mathf.RadToDeg(lat2), Mathf.RadToDeg(lon2), 2000);
            _radiusMesh.SurfaceAddVertex(pos);
        }
        _radiusMesh.SurfaceEnd();
    }

    private Vector3 LatLonToVector3(float lat, float lon, float alt)
    {
        float phi = Mathf.DegToRad(lat);
        float theta = Mathf.DegToRad(-lon + TextureRotationOffset);
        float altScale = alt * 0.001f * 0.3048f * 0.01f;
        float radius = _earthRadius + altScale;

        float x = radius * Mathf.Cos(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi);
        float z = radius * Mathf.Cos(phi) * Mathf.Sin(theta);
        return new Vector3(x, y, z);
    }
}