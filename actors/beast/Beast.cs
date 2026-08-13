using Godot;

/// <summary>
/// What a plot becomes. Rooted and motionless by day; after dark it walks.
/// Spawned by <see cref="Potato"/> when the crop wakes — the Potato node stays
/// behind as the bookkeeping half so saves and the nightly toll keep working.
/// </summary>
public partial class Beast : CharacterBody3D
{
	[Export] public float Speed = 2.2f;

	/// <summary>How close it has to get before it takes what you're carrying.</summary>
	[Export] public float CatchRange = 1.6f;

	/// <summary>It only notices you within this range. Outside it, it tends the rows.</summary>
	[Export] public float SenseRange = 22.0f;

	private GameClock _clock;
	private Player _player;
	private float _sway;

	public override void _Ready()
	{
		AddToGroup("beast");
		_clock = GetNode<GameClock>("/root/GameClock");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsOnFloor())
		{
			Velocity += GetGravity() * (float)delta;
		}

		// Dormant by day. Whatever it is, it does not like the light.
		if (!_clock.IsNight)
		{
			Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
			MoveAndSlide();
			return;
		}

		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			return;
		}

		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		toPlayer.Y = 0.0f;
		float distance = toPlayer.Length();

		if (distance > SenseRange)
		{
			Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
			MoveAndSlide();
			return;
		}

		if (distance <= CatchRange)
		{
			_player.Collapse();
			return;
		}

		// A slight weave, so it doesn't track you like a turret.
		_sway += (float)delta;
		Vector3 direction = toPlayer.Normalized().Rotated(Vector3.Up, Mathf.Sin(_sway * 0.8f) * 0.25f);

		Velocity = new Vector3(direction.X * Speed, Velocity.Y, direction.Z * Speed);
		LookAt(new Vector3(_player.GlobalPosition.X, GlobalPosition.Y, _player.GlobalPosition.Z), Vector3.Up);
		MoveAndSlide();
	}
}
