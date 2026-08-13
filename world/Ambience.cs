using Godot;

/// <summary>
/// Wind bed for the whole farm. Louder and colder after dark, so the shift into
/// night is audible before it is visible.
/// </summary>
public partial class Ambience : AudioStreamPlayer
{
	[Export] public float DayVolumeDb = -22.0f;
	[Export] public float NightVolumeDb = -12.0f;
	[Export] public float FadeSpeed = 0.4f;

	private GameClock _clock;
	private float _night;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");

		// Looping is set in the .import, not here — mutating the shared cached
		// stream at runtime leaks it and affects every other user of the file.
		_night = _clock.IsNight ? 1.0f : 0.0f;
		Play();
	}

	public override void _Process(double delta)
	{
		_night = Mathf.MoveToward(_night, _clock.IsNight ? 1.0f : 0.0f, FadeSpeed * (float)delta);
		VolumeDb = Mathf.Lerp(DayVolumeDb, NightVolumeDb, _night);
	}

	public override void _ExitTree()
	{
		// A looping stream never ends on its own; leave it playing and its
		// playback is still holding the resource when the process tears down.
		Stop();
	}
}
