using Godot;
using System;

public partial class FPPlatformerController : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export] public float WalkSpeed = 6.0f;
	[Export] public float SprintSpeed = 9.0f;
	[Export] public float Acceleration = 8.0f;
	[Export] public float AirControl = 0.6f;
	
	[ExportGroup("Jump")]
	[Export] public float JumpForce = 4.5f;
	[Export] public float CoyoteTimeWindow = 0.15f;
	[Export] public float JumpBufferWindow = 0.2f;
	[Export] public int AirJumps = 1;
	
	[ExportGroup("Camera")]
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float BobFrequency = 2.0f;
	[Export] public float BobAmplitude = 0.08f;
	
	private Camera3D _camera;
	private float _cameraRotationX;
	private float _bobTime;
	private float _coyoteTimeLeft;
	private float _jumpBufferTimeLeft;
	private int _remainingAirJumps;
	private float _originalCameraY;
	private bool _wasOnFloor;
	
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_camera = GetNode<Camera3D>("Head/Camera");
		_originalCameraY = _camera.Position.Y;
		_remainingAirJumps = AirJumps;
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;
		var onFloor = IsOnFloor();
		
		// Handle coyote time
		if (_wasOnFloor && !onFloor)
		{
			_coyoteTimeLeft = CoyoteTimeWindow;
		}
		else if (!_wasOnFloor && onFloor)
		{
			_remainingAirJumps = AirJumps;
		}
		_wasOnFloor = onFloor;
		
		if (_coyoteTimeLeft > 0)
			_coyoteTimeLeft -= (float)delta;
			
		if (_jumpBufferTimeLeft > 0)
			_jumpBufferTimeLeft -= (float)delta;

		// Handle jump
		if (Input.IsActionJustPressed("jump"))
		{
			_jumpBufferTimeLeft = JumpBufferWindow;
		}

		if (_jumpBufferTimeLeft > 0 && (onFloor || _coyoteTimeLeft > 0 || _remainingAirJumps > 0))
		{
			velocity.Y = JumpForce;
			_jumpBufferTimeLeft = 0;
			_coyoteTimeLeft = 0;
			
			if (!onFloor && _coyoteTimeLeft <= 0)
				_remainingAirJumps--;
		}

		// Apply gravity
		if (!onFloor)
		{
			velocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * (float)delta;
			
			// Higher gravity when falling for better feel
			if (velocity.Y < 0)
				velocity.Y *= 1.1f;
			
			// Cut jump short if button released
			if (!Input.IsActionPressed("jump") && velocity.Y > 0)
				velocity.Y *= 0.5f;
		}

		// Get movement input
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		// Target speed based on sprint
		float targetSpeed = Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;
		
		// Ground movement
		if (onFloor)
		{
			if (direction != Vector3.Zero)
			{
				float targetVelocityX = direction.X * targetSpeed;
				float targetVelocityZ = direction.Z * targetSpeed;
				
				velocity.X = Mathf.MoveToward(velocity.X, targetVelocityX, Acceleration * (float)delta);
				velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocityZ, Acceleration * (float)delta);
			}
			else
			{
				// Friction
				velocity.X = Mathf.MoveToward(velocity.X, 0, Acceleration * (float)delta);
				velocity.Z = Mathf.MoveToward(velocity.Z, 0, Acceleration * (float)delta);
			}
		}
		// Air movement
		else
		{
			if (direction != Vector3.Zero)
			{
				velocity.X += direction.X * targetSpeed * AirControl * (float)delta;
				velocity.Z += direction.Z * targetSpeed * AirControl * (float)delta;
				
				// Cap air speed
				float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
				if (horizontalSpeed > targetSpeed)
				{
					float scale = targetSpeed / horizontalSpeed;
					velocity.X *= scale;
					velocity.Z *= scale;
				}
			}
		}

		// Head bob when moving on ground
		if (onFloor && direction != Vector3.Zero)
		{
			_bobTime += (float)delta * BobFrequency;
			var bobOffset = Mathf.Sin(_bobTime) * BobAmplitude;
			_camera.Position = new Vector3(
				_camera.Position.X,
				_originalCameraY + bobOffset,
				_camera.Position.Z
			);
		}
		else
		{
			// Reset head bob
			_bobTime = 0;
			_camera.Position = new Vector3(
				_camera.Position.X,
				Mathf.Lerp(_camera.Position.Y, _originalCameraY, 0.2f),
				_camera.Position.Z
			);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (Input.MouseMode != Input.MouseModeEnum.Captured)
				return;

			// Rotate player (horizontal)
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);

			// Rotate camera (vertical)
			_cameraRotationX = Mathf.Clamp(
				_cameraRotationX - mouseMotion.Relative.Y * MouseSensitivity,
				-Mathf.Pi/2,
				Mathf.Pi/2
			);
			GetNode<Node3D>("Head").Rotation = new Vector3(_cameraRotationX, 0, 0);
		}
		else if (@event is InputEventKey eventKey)
		{
			if (eventKey.Keycode == Key.Escape)
				Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? 
					Input.MouseModeEnum.Visible : 
					Input.MouseModeEnum.Captured;
		}
	}
}
