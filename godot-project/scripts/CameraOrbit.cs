using Godot;

public partial class CameraOrbit : Node3D
{
    [Export] public float MinSensitivity = 0.23f;
    [Export] public float MaxSensitivity = 230.0f;
    private const float SpeedFactor = 0.0001f;

    [Export] public float MinZoomStep = 0.5f;
    [Export] public float MaxZoomStep = 50.0f;

    [Export] public float MinZoom = 51.0f;
    [Export] public float MaxZoom = 1000.0f;
    [Export] public float CtrlOrbitMultiplier = 3.5f;


    private Camera3D _camera;

    private bool _ctrlDragging = false;
    private Vector3? _dragPivot = null;
    private Vector3 _lastCtrlPivot = Vector3.Zero;


    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        float t = Mathf.InverseLerp(MinZoom, MaxZoom, _camera.Position.Z);
        float rawSensitivity = Mathf.Lerp(MinSensitivity, MaxSensitivity, t);
        float currentRotSpeed = rawSensitivity * SpeedFactor;
        float currentZoomStep = Mathf.Lerp(MinZoomStep, MaxZoomStep, t);

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed && Input.IsKeyPressed(Key.Ctrl))
                {
                    _ctrlDragging = true;

                    Vector2 screenCenter =
                        GetViewport().GetVisibleRect().Size * 0.5f;

                    _dragPivot = GetSphereHitPoint(screenCenter);

                    if (_dragPivot.HasValue)
                        _lastCtrlPivot = _dragPivot.Value;
                }

                else if (!mb.Pressed)
                {
                    _ctrlDragging = false;
                    _dragPivot = null;
                }
            }

            if (mb.ButtonIndex == MouseButton.WheelUp)
                ZoomCamera(-currentZoomStep);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                ZoomCamera(currentZoomStep);
        }

        if (@event is InputEventMouseMotion mm)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                if (_ctrlDragging && _dragPivot.HasValue)
                {
                    OrbitAroundPivot(
                        _dragPivot.Value,
                        mm.Relative,
                        currentRotSpeed * CtrlOrbitMultiplier
                    );
                }
                else
                {
                    RotateY(-mm.Relative.X * currentRotSpeed);

                    float rotX = Rotation.X - mm.Relative.Y * currentRotSpeed;
                    rotX = Mathf.Clamp(rotX, Mathf.DegToRad(-85), Mathf.DegToRad(85));
                    Rotation = new Vector3(rotX, Rotation.Y, 0);
                }
            }
        }

        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.R && key.CtrlPressed)
        {
            ResetToCenter();
        }
    }

    private void OrbitAroundPivot(Vector3 pivot, Vector2 mouseDelta, float speed)
    {
        Vector3 camOffset = _camera.GlobalPosition - pivot;

        camOffset = camOffset.Rotated(Vector3.Up, -mouseDelta.X * speed);
        camOffset = camOffset.Rotated(_camera.GlobalTransform.Basis.X, -mouseDelta.Y * speed);

        _camera.GlobalPosition = pivot + camOffset;
        _camera.LookAt(pivot, Vector3.Up);
    }

    private Vector3? GetSphereHitPoint(Vector2 screenPos)
    {
        var from = _camera.ProjectRayOrigin(screenPos);
        var dir = _camera.ProjectRayNormal(screenPos);

        var space = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, from + dir * 5000);
        var result = space.IntersectRay(query);

        if (result.Count > 0)
            return (Vector3)result["position"];

        return null;
    }

    private void ZoomCamera(float amount)
    {
        Vector3 pos = _camera.Position;
        pos.Z = Mathf.Clamp(pos.Z + amount, MinZoom, MaxZoom);
        _camera.Position = pos;
    }

    private void ResetToCenter()
    {
        Vector3 center = Vector3.Zero;

        float distance = (_camera.GlobalPosition - center).Length();

        _camera.GlobalPosition = center + new Vector3(0, 0, distance);

        _camera.LookAt(center, Vector3.Up);

        Rotation = Vector3.Zero;
    }


}
