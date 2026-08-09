using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 0.002f;

	/// <summary>How far up/down you can look, in degrees.</summary>
	[Export] public float PitchLimit = 89.0f;

	private Camera3D _camera;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			// Yaw turns the whole body, so movement follows where you look.
			RotateY(-motion.Relative.X * MouseSensitivity);

			// Pitch only tilts the camera — tilting the body would tip the collider.
			float limit = Mathf.DegToRad(PitchLimit);
			Vector3 rotation = _camera.Rotation;
			rotation.X = Mathf.Clamp(rotation.X - motion.Relative.Y * MouseSensitivity, -limit, limit);
			_camera.Rotation = rotation;
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			// Escape releases the mouse; click to recapture.
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (@event is InputEventMouseButton { Pressed: true } && Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		// Rotate the raw input into the direction the body is facing.
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
