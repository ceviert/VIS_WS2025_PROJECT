using Godot;
using System;

public partial class CameraOrbit : Node3D
{
    [Export] public float RotationSpeed = 0.005f;
    [Export] public float ZoomSpeed = 2.0f;
    [Export] public float MinZoom = 55.0f;
    [Export] public float MaxZoom = 300.0f;

    private Camera3D _camera;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                RotateY(-mouseMotion.Relative.X * RotationSpeed);

                float currentRotX = Rotation.X;
                currentRotX -= mouseMotion.Relative.Y * RotationSpeed;
                currentRotX = Mathf.Clamp(currentRotX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
                
                Rotation = new Vector3(currentRotX, Rotation.Y, Rotation.Z);
            }
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomCamera(-ZoomSpeed);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomCamera(ZoomSpeed);
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