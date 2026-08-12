using Godot;

/// <summary>
/// Lights itself at dusk and goes out at dawn, driven by the <see cref="GameClock"/>.
/// The only thing that makes checking the field after dark survivable.
/// </summary>
public partial class LampPost : Node3D
{
	[Export] public float NightEnergy = 3.0f;
	[Export] public float FadeSpeed = 1.2f;

	private GameClock _clock;
	private OmniLight3D _light;
	private MeshInstance3D _glass;
	private StandardMaterial3D _glassMaterial;

	private float _lit;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");
		_light = GetNode<OmniLight3D>("Light");
		_glass = GetNode<MeshInstance3D>("Glass");

		// Own material per lamp so they can flicker independently later.
		_glassMaterial = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.9f, 0.72f, 0.42f),
			EmissionEnabled = true,
			Emission = new Color(1.0f, 0.78f, 0.45f),
		};
		_glass.MaterialOverride = _glassMaterial;

		_lit = _clock.IsNight ? 1.0f : 0.0f;
	}

	public override void _Process(double delta)
	{
		float target = _clock.IsNight ? 1.0f : 0.0f;
		_lit = Mathf.MoveToward(_lit, target, FadeSpeed * (float)delta);

		_light.LightEnergy = _lit * NightEnergy;
		_light.Visible = _lit > 0.01f;
		_glassMaterial.EmissionEnergyMultiplier = _lit * 2.0f;
	}
}
