using Godot;

/// <summary>
/// Drives the sun from the <see cref="GameClock"/>. Attach to the scene's
/// DirectionalLight3D.
/// </summary>
public partial class DayNightLight : DirectionalLight3D
{
	[Export] public float DayEnergy = 1.0f;
	[Export] public float NightEnergy = 0.04f;
	[Export] public Color DayColor = new(1.0f, 0.97f, 0.9f);
	[Export] public Color NightColor = new(0.35f, 0.4f, 0.62f);

	/// <summary>Seconds to fade between day and night. Slow is scarier.</summary>
	[Export] public float FadeSpeed = 0.35f;

	private GameClock _clock;
	private float _darkness;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");
		_darkness = _clock.IsNight ? 1.0f : 0.0f;
	}

	public override void _Process(double delta)
	{
		float target = _clock.IsNight ? 1.0f : 0.0f;
		_darkness = Mathf.MoveToward(_darkness, target, FadeSpeed * (float)delta);

		LightEnergy = Mathf.Lerp(DayEnergy, NightEnergy, _darkness);
		LightColor = DayColor.Lerp(NightColor, _darkness);

		// Swing the sun down past the horizon over the course of the fade.
		Rotation = new Vector3(Mathf.Lerp(-Mathf.Pi / 3.0f, -Mathf.Pi * 0.95f, _darkness), Rotation.Y, Rotation.Z);
	}
}
