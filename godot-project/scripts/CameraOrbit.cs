using Godot;
using System;

public partial class CameraOrbit : Node3D
{
    [Export] public float MinSensitivity = 0.23f;
    [Export] public float MaxSensitivity = 230.0f; 

    private const float SpeedFactor = 0.0001f;

    [Export] public float MinZoomStep = 0.5f;
    [Export] public float MaxZoomStep = 50.0f;

    [Export] public float MinZoom = 51.0f;
    [Export] public float MaxZoom = 1000.0f;

    private Camera3D _camera;

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

        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                RotateY(-mouseMotion.Relative.X * currentRotSpeed);

                float currentRotX = Rotation.X;
                currentRotX -= mouseMotion.Relative.Y * currentRotSpeed;
                currentRotX = Mathf.Clamp(currentRotX, Mathf.DegToRad(-85), Mathf.DegToRad(85));
                
                Rotation = new Vector3(currentRotX, Rotation.Y, Rotation.Z);
            }
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomCamera(-currentZoomStep);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomCamera(currentZoomStep);
            }
        }
    }

    private void ZoomCamera(float amount)
    {
        Vector3 pos = _camera.Position;
        pos.Z += amount;
        pos.Z = Mathf.Clamp(pos.Z, MinZoom, MaxZoom);
        _camera.Position = pos;
    }
}