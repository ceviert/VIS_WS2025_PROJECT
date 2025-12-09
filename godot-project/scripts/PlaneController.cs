using Godot;
using System.Collections.Generic;

public partial class PlaneController : Node3D
{
    public string IcaoHex { get; private set; }
    public float CurrentLat { get; private set; }
    public float CurrentLon { get; private set; }
    public float CurrentAltFt { get; private set; }

    private float _earthRadius = 50.0f; 
    private int _maxTrailLength = 100;
    private float _textureRotationOffset = 0f; // Manager'dan gelecek

    private Vector3 _targetPosition;
    private MeshInstance3D _trailMeshInstance;
    private ImmediateMesh _trailMesh;
    private List<Vector3> _trailPoints = new List<Vector3>();
    private List<Color> _trailColors = new List<Color>();

    public override void _Ready()
    {
        if (HasNode("TrailLine"))
        {
            _trailMeshInstance = GetNode<MeshInstance3D>("TrailLine");
        }
        else
        {
            _trailMeshInstance = new MeshInstance3D();
            _trailMeshInstance.Name = "TrailLine";
            AddChild(_trailMeshInstance);
        }

        _trailMesh = new ImmediateMesh();
        _trailMeshInstance.Mesh = _trailMesh;

        _trailMeshInstance.CustomAabb = new Aabb(new Vector3(-50000, -50000, -50000), new Vector3(100000, 100000, 100000));

        var mat = new StandardMaterial3D();
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.VertexColorUseAsAlbedo = true;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.UsePointSize = true;
        mat.PointSize = 5.0f;
        _trailMeshInstance.MaterialOverride = mat;
    }

    public override void _Process(double delta)
    {
        Position = Position.Lerp(_targetPosition, (float)delta * 2.0f);

        if (Position.DistanceTo(_targetPosition) > 0.1f) 
        {
            Vector3 direction = (_targetPosition - Position).Normalized();
            Vector3 upVector = Position.Normalized();

            if (Mathf.Abs(direction.Dot(upVector)) < 0.99f) 
            {
                LookAt(_targetPosition, upVector);
            }
        }
    }

    public void UpdateData(string hex, float lat, float lon, float alt, float track, float offset)
    {
        _textureRotationOffset = offset;
        
        if (string.IsNullOrEmpty(IcaoHex)) IcaoHex = hex;
        CurrentLat = lat;
        CurrentLon = lon;
        CurrentAltFt = alt;

        _targetPosition = LatLonToVector3(lat, lon, alt);

        AddTrailPoint(_targetPosition, alt);
        DrawTrail();

        if (Position.DistanceTo(_targetPosition) < 0.2f)
        {
            SetHeading(track);
        }
    }

    public void SetHeading(float headingDeg)
    {
        Vector3 up = Position.Normalized();
        Vector3 north = Vector3.Up.Slide(up).Normalized();
        Vector3 forward = north.Rotated(up, Mathf.DegToRad(-headingDeg));
        LookAt(Position + forward, up);
    }

    private void AddTrailPoint(Vector3 pos, float alt)
    {
        _trailPoints.Add(pos);
        _trailColors.Add(GetColorByAltitude(alt));

        if (_trailPoints.Count > _maxTrailLength)
        {
            _trailPoints.RemoveAt(0);
            _trailColors.RemoveAt(0);
        }
    }

    private void DrawTrail()
    {
        _trailMesh.ClearSurfaces();
        _trailMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        for (int i = 0; i < _trailPoints.Count; i++)
        {
            _trailMesh.SurfaceSetColor(_trailColors[i]);
            _trailMesh.SurfaceAddVertex(_trailPoints[i] - Position); // Local Space dönüşümü
        }

        _trailMesh.SurfaceEnd();
    }

    private Color GetColorByAltitude(float altFt)
    {
        float t = Mathf.Clamp(altFt / 40000.0f, 0, 1);
        if (t < 0.5f)
            return new Color(0, 1, 1).Lerp(new Color(1, 1, 0), t * 2);
        else
            return new Color(1, 1, 0).Lerp(new Color(1, 0, 1), (t - 0.5f) * 2);
    }

    private Vector3 LatLonToVector3(float lat, float lon, float alt)
    {
        float phi = Mathf.DegToRad(lat);
        float theta = Mathf.DegToRad(-lon + _textureRotationOffset); 
        float altScale = alt * 0.001f * 0.3048f * 0.01f; 
        float radius = _earthRadius + altScale;
        
        float x = radius * Mathf.Cos(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi);
        float z = radius * Mathf.Cos(phi) * Mathf.Sin(theta);
        return new Vector3(x, y, z);
    }
}