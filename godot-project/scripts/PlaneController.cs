using Godot;
using System.Collections.Generic;

public partial class PlaneController : Node3D
{
	private MeshInstance3D _trailMeshInstance;
	private ImmediateMesh _trailMesh;

	public string IcaoHex { get; set; }
	private Vector3 _targetPosition;
	private List<Vector3> _trailPoints = new List<Vector3>();
	private List<Color> _trailColors = new List<Color>();

	private int _maxTrailLength = 50;
	private float _earthRadius = 50.0f;

	public override void _Ready()
	{
		_trailMeshInstance = GetNode<MeshInstance3D>("Trail");
		_trailMesh = new ImmediateMesh();
		_trailMeshInstance.Mesh = _trailMesh;
		
		var mat = new StandardMaterial3D();
		mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		mat.VertexColorUseAsAlbedo = true; 
		mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		_trailMeshInstance.MaterialOverride = mat;
	}

	public override void _Process(double delta)
	{
		Position = Position.Lerp(_targetPosition, (float)delta * 2.0f);
		
		if (Position.DistanceTo(_targetPosition) > 0.01f)
		{
			Vector3 up = Position.Normalized(); 
			LookAt(_targetPosition, up);
		}
	}

	public void UpdateData(float lat, float lon, float alt)
	{
		_targetPosition = LatLonToVector3(lat, lon, alt);
		
		AddTrailPoint(_targetPosition, alt);
		
		DrawTrail();
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
			_trailMesh.SurfaceAddVertex(_trailPoints[i] - Position);
		}

		_trailMesh.SurfaceEnd();
	}

	private Color GetColorByAltitude(float altFt)
	{
		float normalizedAlt = Mathf.Clamp(altFt / 40000.0f, 0, 1);
		
		if (normalizedAlt < 0.5f)
			return new Color(0, 0, 1).Lerp(new Color(0, 1, 0), normalizedAlt * 2);
		else
			return new Color(0, 1, 0).Lerp(new Color(1, 0, 0), (normalizedAlt - 0.5f) * 2);
	}

	private Vector3 LatLonToVector3(float lat, float lon, float alt)
	{
		float phi = Mathf.DegToRad(lat);
		float theta = Mathf.DegToRad(lon - 90);
		float radius = _earthRadius + (alt * 0.001f); 
		
		float x = radius * Mathf.Cos(phi) * Mathf.Cos(theta);
		float y = radius * Mathf.Sin(phi);
		float z = radius * Mathf.Cos(phi) * Mathf.Sin(theta);
		return new Vector3(x, y, z);
	}
}
