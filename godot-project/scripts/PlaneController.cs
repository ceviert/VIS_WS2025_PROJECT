using Godot;
using System.Collections.Generic;

public partial class PlaneController : Node3D
{
    [ExportGroup("References")]
    // YENİ: Uçağın kendi Mesh'ini buraya sürükleyip bırakmalısın
    [Export] public MeshInstance3D AircraftMesh; 

    [ExportGroup("Visuals")]
    [Export] public float TrailWidth = 0.5f; 
    [Export] public Gradient TrailGradient; 
    [Export] public float MaxAltitudeFt = 40000f; 

    [ExportGroup("Settings")]
    [Export] public int PointUpdateFrequency = 2; 
    // Performans limiti
    [Export] public int MaxHistoryPoints = 1000; 

    private MeshInstance3D _trailInstance;
    private ImmediateMesh _trailMesh;
    private StandardMaterial3D _aircraftMaterial; // Uçağın materyalini hafızada tutacağız

    // Veri Yapıları
    private struct TrailPoint
    {
        public Vector3 Position;
        public float Altitude;
        public TrailPoint(Vector3 pos, float alt) { Position = pos; Altitude = alt; }
    }
    private readonly List<TrailPoint> _historyPoints = new();
    
    private Vector3 _targetPosition;
    private float _currentHeading;
    public float CurrentAltFt { get; private set; }
    private int _updateCounter = 0; 

    public override void _Ready()
    {
        // Gradient yoksa varsayılan oluştur
        if (TrailGradient == null)
        {
            TrailGradient = new Gradient();
            TrailGradient.AddPoint(0.0f, Colors.Blue);
            TrailGradient.AddPoint(1.0f, Colors.Red);
        }

        SetupTrailSystem();
        SetupAircraftMaterial();
    }

    private void SetupAircraftMaterial()
    {
        // Eğer editörden AircraftMesh atandıysa
        if (AircraftMesh != null)
        {
            // Yeni bir materyal oluştur (Böylece diğer uçakları etkilemez)
            _aircraftMaterial = new StandardMaterial3D();
            
            // Trail ile uyumlu olsun diye "Unshaded" yapabilirsin. 
            // Işık alsın istiyorsan bu satırı sil.
            _aircraftMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded; 
            
            // Uçağa bu özel materyali ata
            AircraftMesh.MaterialOverride = _aircraftMaterial;
        }
        else
        {
            GD.PrintErr($"[PlaneController] '{Name}' uçağında AircraftMesh atanmamış! Renk değişmeyecek.");
        }
    }

    private void SetupTrailSystem()
    {
        _trailInstance = new MeshInstance3D();
        _trailMesh = new ImmediateMesh();
        _trailInstance.Mesh = _trailMesh;
        
        _trailInstance.TopLevel = true; 
        _trailInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        
        AddChild(_trailInstance);

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true, // Gradient için şart
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            UsePointSize = false
        };
        _trailInstance.MaterialOverride = mat;
    }

    public override void _Process(double delta)
    {
        Position = Position.Lerp(_targetPosition, (float)delta * 3f);
        ApplyHeadingOnSphere();
    }

    public void UpdatePlaneData(float lat, float lon, float alt, float heading, float offset)
    {
        CurrentAltFt = alt;
        _currentHeading = heading;
        
        Vector3 newTargetPos = GeoUtils.LatLonToVector3(lat, lon, alt, offset, 1.0f);

        // --- UÇAK RENGİNİ GÜNCELLE ---
        UpdateAircraftColor(alt);

        // Işınlanma Kontrolü
        if (Position.LengthSquared() < 1.0f || Position.DistanceTo(newTargetPos) > 50.0f)
        {
            Position = newTargetPos;
            _targetPosition = newTargetPos;
            ResetTrail();
            ApplyHeadingOnSphere();
            AddTrailPoint(newTargetPos, alt);
            _updateCounter = 0; 
            return;
        }

        _targetPosition = newTargetPos;

        // Seyrek Trail Mantığı
        _updateCounter++;
        if (_updateCounter >= PointUpdateFrequency)
        {
            AddTrailPoint(GlobalPosition, CurrentAltFt); 
            _updateCounter = 0; 
            DrawTrail();
        }
    }

    private void UpdateAircraftColor(float alt)
    {
        if (_aircraftMaterial == null) return;

        // İrtifaya göre 0..1 arası oran
        float t = Mathf.Clamp(alt / MaxAltitudeFt, 0f, 1f);
        
        // Gradient'ten rengi al
        Color color = TrailGradient.Sample(t);
        
        // Materyale ata
        _aircraftMaterial.AlbedoColor = color;
    }

    private void AddTrailPoint(Vector3 pos, float alt)
    {
        if (_historyPoints.Count > MaxHistoryPoints) _historyPoints.RemoveAt(0);
        _historyPoints.Add(new TrailPoint(pos, alt));
    }

    private void DrawTrail()
    {
        if (_historyPoints.Count < 2) return;

        _trailMesh.ClearSurfaces();
        _trailMesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);

        for (int i = 0; i < _historyPoints.Count - 1; i++)
        {
            TrailPoint currPt = _historyPoints[i];
            TrailPoint nextPt = _historyPoints[i + 1];

            Vector3 curr = currPt.Position;
            Vector3 next = nextPt.Position;

            // Renk Hesaplama (Trail için)
            float t1 = Mathf.Clamp(currPt.Altitude / MaxAltitudeFt, 0f, 1f);
            Color c1 = TrailGradient.Sample(t1);

            float t2 = Mathf.Clamp(nextPt.Altitude / MaxAltitudeFt, 0f, 1f);
            Color c2 = TrailGradient.Sample(t2);

            // Geometri
            Vector3 dir = (next - curr).Normalized();
            Vector3 up = curr.Normalized(); 
            Vector3 right = dir.Cross(up).Normalized() * TrailWidth * 0.01f;

            _trailMesh.SurfaceSetColor(c1);
            _trailMesh.SurfaceAddVertex(curr - right);
            _trailMesh.SurfaceAddVertex(curr + right);
            
            if (i == _historyPoints.Count - 2)
            {
                _trailMesh.SurfaceSetColor(c2);
                _trailMesh.SurfaceAddVertex(next - right);
                _trailMesh.SurfaceAddVertex(next + right);
            }
        }
        _trailMesh.SurfaceEnd();
    }

    private void ApplyHeadingOnSphere()
    {
        if (Position.LengthSquared() < 0.1f) return;
        Vector3 surfaceNormal = Position.Normalized();
        Vector3 north = Vector3.Up.Slide(surfaceNormal).Normalized();
        Vector3 forwardVector = north.Rotated(surfaceNormal, Mathf.DegToRad(-_currentHeading));
        if (forwardVector.LengthSquared() > 0.001f) LookAt(Position + forwardVector, surfaceNormal);
    }

    private void ResetTrail()
    {
        _historyPoints.Clear();
        _updateCounter = 0;
        if (_trailMesh != null) _trailMesh.ClearSurfaces();
    }

    public override void _ExitTree()
    {
        if (_trailInstance != null && IsInstanceValid(_trailInstance)) _trailInstance.QueueFree();
    }
}