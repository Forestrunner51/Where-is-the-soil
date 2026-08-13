using Godot;

/// <summary>
/// Thickens the fog after dark so the world collapses to lamp range. Attach to
/// the WorldEnvironment.
/// </summary>
public partial class Atmosphere : WorldEnvironment
{
	[Export] public float DayFogDensity = 0.004f;
	[Export] public float NightFogDensity = 0.05f;
	[Export] public Color DayFogColor = new(0.62f, 0.63f, 0.66f);
	[Export] public Color NightFogColor = new(0.05f, 0.06f, 0.09f);
	[Export] public float FadeSpeed = 0.3f;

	private GameClock _clock;
	private float _night;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");

		if (Environment == null)
		{
			GD.PushWarning("Atmosphere: no Environment assigned.");
			return;
		}

		// Own the environment so tweaking it can't leak into the saved resource.
		Environment = (Godot.Environment)Environment.Duplicate();
		Environment.FogEnabled = true;

		_night = _clock.IsNight ? 1.0f : 0.0f;
	}

	public override void _Process(double delta)
	{
		if (Environment == null)
		{
			return;
		}

		_night = Mathf.MoveToward(_night, _clock.IsNight ? 1.0f : 0.0f, FadeSpeed * (float)delta);

		Environment.FogDensity = Mathf.Lerp(DayFogDensity, NightFogDensity, _night);
		Environment.FogLightColor = DayFogColor.Lerp(NightFogColor, _night);
	}
}
