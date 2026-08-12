using Godot;

/// <summary>
/// Autoloaded player preferences, persisted to user://settings.cfg so they
/// survive a restart. Systems read from here rather than keeping their own copy.
/// </summary>
public partial class GameSettings : Node
{
	private const string ConfigPath = "user://settings.cfg";

	public float MouseSensitivity { get; set; } = 0.002f;

	/// <summary>0 = silent, 1 = full.</summary>
	public float MasterVolume { get; set; } = 0.8f;

	public bool Fullscreen { get; set; }
	public bool InvertY { get; set; }

	public override void _Ready()
	{
		Load();
		Apply();
	}

	public void Apply()
	{
		int bus = AudioServer.GetBusIndex("Master");
		if (bus >= 0)
		{
			// Silence needs an explicit floor — LinearToDb(0) is negative infinity.
			AudioServer.SetBusVolumeDb(bus, MasterVolume <= 0.001f ? -80.0f : Mathf.LinearToDb(MasterVolume));
		}

		DisplayServer.WindowSetMode(Fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);
	}

	public void Save()
	{
		var config = new ConfigFile();
		config.SetValue("input", "mouse_sensitivity", MouseSensitivity);
		config.SetValue("input", "invert_y", InvertY);
		config.SetValue("audio", "master_volume", MasterVolume);
		config.SetValue("video", "fullscreen", Fullscreen);
		config.Save(ConfigPath);
	}

	public void Load()
	{
		var config = new ConfigFile();
		if (config.Load(ConfigPath) != Error.Ok)
		{
			// No file yet — first run. Defaults stand.
			return;
		}

		MouseSensitivity = config.GetValue("input", "mouse_sensitivity", MouseSensitivity).AsSingle();
		InvertY = config.GetValue("input", "invert_y", InvertY).AsBool();
		MasterVolume = config.GetValue("audio", "master_volume", MasterVolume).AsSingle();
		Fullscreen = config.GetValue("video", "fullscreen", Fullscreen).AsBool();
	}
}
