using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;

	/// <summary>How much faster Shift makes you. The farm is 60 m across.</summary>
	[Export] public float SprintMultiplier = 1.8f;

	[Export] public float JumpVelocity = 4.5f;

	/// <summary>How close you must be to an interactable to use it, in metres.</summary>
	[Export] public float InteractRange = 2.0f;

	/// <summary>How far up/down you can look, in degrees.</summary>
	[Export] public float PitchLimit = 89.0f;

	/// <summary>Meat you start the first day with, so day one isn't already lost.</summary>
	[Export] public int StartingMeat = 5;

	[Export] public int StartingGold = 40;

	/// <summary>Seconds between footfalls at walking pace.</summary>
	[Export] public float StepInterval = 0.52f;

	private Camera3D _camera;
	private GameSettings _settings;

	private AudioStreamPlayer _stepPlayer;
	private AudioStreamPlayer _stingPlayer;
	private AudioStream[] _steps;
	private float _stepTimer;
	private int _stepIndex;

	public Inventory Inventory { get; private set; }

	public int Gold { get; private set; }

	/// <summary>True while a menu owns the mouse. Freezes look, movement and interact.</summary>
	public bool UiOpen { get; set; }

	public void AddGold(int amount) => Gold += amount;

	public bool TrySpend(int amount)
	{
		if (amount > Gold)
		{
			return false;
		}

		Gold -= amount;
		return true;
	}

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		_settings = GetNode<GameSettings>("/root/GameSettings");
		Input.MouseMode = Input.MouseModeEnum.Captured;

		AddToGroup("player");

		// The clock runs whenever someone is actually in the field — this also
		// makes running world/main.tscn directly (F6) behave.
		GetNode<GameClock>("/root/GameClock").Running = true;

		Inventory = new Inventory { Name = "Inventory" };
		AddChild(Inventory);
		Inventory.Add(ItemDatabase.RawMeat, StartingMeat);
		Gold = StartingGold;

		// A loaded run overwrites the fresh-start kit.
		GetNode<SaveGame>("/root/SaveGame").ApplyToPlayer(this);

		SetUpAudio();
	}

	private void SetUpAudio()
	{
		_steps = new AudioStream[]
		{
			GD.Load<AudioStream>("res://audio/step_1.wav"),
			GD.Load<AudioStream>("res://audio/step_2.wav"),
			GD.Load<AudioStream>("res://audio/step_3.wav"),
		};

		_stepPlayer = new AudioStreamPlayer { Name = "StepPlayer", VolumeDb = -14.0f };
		AddChild(_stepPlayer);

		_stingPlayer = new AudioStreamPlayer
		{
			Name = "StingPlayer",
			Stream = GD.Load<AudioStream>("res://audio/caught.wav"),
			VolumeDb = -4.0f,
		};
		AddChild(_stingPlayer);
	}

	/// <summary>Footfalls paced by how fast you are actually moving.</summary>
	private void UpdateFootsteps(double delta, bool sprinting)
	{
		var flat = new Vector2(Velocity.X, Velocity.Z);

		if (!IsOnFloor() || flat.Length() < 0.5f)
		{
			// Land the next step immediately when you start moving again.
			_stepTimer = 0.0f;
			return;
		}

		_stepTimer -= (float)delta * (sprinting ? 1.7f : 1.0f);
		if (_stepTimer > 0.0f)
		{
			return;
		}

		_stepTimer = StepInterval;
		_stepPlayer.Stream = _steps[_stepIndex];
		_stepIndex = (_stepIndex + 1) % _steps.Length;
		_stepPlayer.PitchScale = (float)GD.RandRange(0.92, 1.08);
		_stepPlayer.Play();
	}

	/// <summary>Restores saved gold, bypassing the spend/earn path.</summary>
	public void LoadGold(int amount) => Gold = amount;

	private bool _collapsing;

	/// <summary>
	/// Caught in the dark. You lose whatever you were carrying and the night
	/// finishes without you — the run continues, but the trip was for nothing.
	/// </summary>
	public void Collapse()
	{
		// Several beasts can reach you on the same frame.
		if (_collapsing)
		{
			return;
		}

		_collapsing = true;

		GD.Print("Something takes hold of you in the dark. You wake at dawn, empty-handed.");
		_stingPlayer?.Play();
		Inventory.Clear();

		// Wake up where you should have stayed.
		if (GetTree().GetFirstNodeInGroup("bed") is Node3D bed)
		{
			GlobalPosition = bed.GlobalPosition + new Vector3(0.0f, 1.2f, 1.6f);
			Velocity = Vector3.Zero;
		}

		GetNode<GameClock>("/root/GameClock").SleepThroughNight();
		_collapsing = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// A menu is up — it owns input until it closes.
		if (UiOpen)
		{
			return;
		}

		if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			float sensitivity = _settings.MouseSensitivity;

			// Yaw turns the whole body, so movement follows where you look.
			RotateY(-motion.Relative.X * sensitivity);

			// Pitch only tilts the camera — tilting the body would tip the collider.
			float pitchDelta = motion.Relative.Y * sensitivity * (_settings.InvertY ? -1.0f : 1.0f);
			float limit = Mathf.DegToRad(PitchLimit);
			Vector3 rotation = _camera.Rotation;
			rotation.X = Mathf.Clamp(rotation.X - pitchDelta, -limit, limit);
			_camera.Rotation = rotation;
		}
		else if (@event.IsActionPressed("interact"))
		{
			FindNearestInteractable()?.Interact(this);
		}
	}

	/// <summary>
	/// Closest usable interactable within range, or null. Picking the nearest
	/// keeps two overlapping crops from both reacting to one key press.
	/// </summary>
	public IInteractable FindNearestInteractable()
	{
		IInteractable nearest = null;
		float nearestDistance = InteractRange;

		foreach (Node node in GetTree().GetNodesInGroup("interactable"))
		{
			if (node is not Node3D spatial || node is not IInteractable candidate)
			{
				continue;
			}

			if (!candidate.CanInteract)
			{
				continue;
			}

			float distance = GlobalPosition.DistanceTo(spatial.GlobalPosition);
			if (distance <= nearestDistance)
			{
				nearestDistance = distance;
				nearest = candidate;
			}
		}

		return nearest;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (!UiOpen && Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Still fall while a menu is open, just don't walk.
		Vector2 inputDir = UiOpen
			? Vector2.Zero
			: Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		// Rotate the raw input into the direction the body is facing.
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		bool sprinting = !UiOpen && Input.IsActionPressed("sprint");
		float speed = sprinting ? Speed * SprintMultiplier : Speed;

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * speed;
			velocity.Z = direction.Z * speed;
		}
		else
		{
			// Decelerate at the base rate so letting go of Shift doesn't slide.
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		UpdateFootsteps(delta, sprinting);
	}
}
