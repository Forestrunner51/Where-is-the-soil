using Godot;

/// <summary>
/// Settings overlay, reused by the main menu and the pause menu. Runs with
/// ProcessMode.Always so it still works while the game is paused.
/// </summary>
public partial class SettingsPanel : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private GameSettings _settings;

	private HSlider _sensitivity;
	private HSlider _volume;
	private CheckBox _fullscreen;
	private CheckBox _invertY;
	private Label _sensitivityValue;
	private Label _volumeValue;
	private Button _back;

	public override void _Ready()
	{
		_settings = GetNode<GameSettings>("/root/GameSettings");

		_sensitivity = GetNode<HSlider>("Panel/Layout/Sensitivity");
		_volume = GetNode<HSlider>("Panel/Layout/Volume");
		_fullscreen = GetNode<CheckBox>("Panel/Layout/Fullscreen");
		_invertY = GetNode<CheckBox>("Panel/Layout/InvertY");
		_sensitivityValue = GetNode<Label>("Panel/Layout/SensitivityLabel");
		_volumeValue = GetNode<Label>("Panel/Layout/VolumeLabel");
		_back = GetNode<Button>("Panel/Layout/Back");

		_sensitivity.ValueChanged += OnSensitivityChanged;
		_volume.ValueChanged += OnVolumeChanged;
		_fullscreen.Toggled += OnFullscreenToggled;
		_invertY.Toggled += OnInvertYToggled;
		_back.Pressed += Close;

		Visible = false;
	}

	public void Open()
	{
		// Push current values in without re-firing the change handlers.
		_sensitivity.SetValueNoSignal(_settings.MouseSensitivity);
		_volume.SetValueNoSignal(_settings.MasterVolume);
		_fullscreen.SetPressedNoSignal(_settings.Fullscreen);
		_invertY.SetPressedNoSignal(_settings.InvertY);

		UpdateLabels();
		Visible = true;
		_back.GrabFocus();
	}

	public void Close()
	{
		_settings.Save();
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnSensitivityChanged(double value)
	{
		_settings.MouseSensitivity = (float)value;
		UpdateLabels();
	}

	private void OnVolumeChanged(double value)
	{
		_settings.MasterVolume = (float)value;
		_settings.Apply();
		UpdateLabels();
	}

	private void OnFullscreenToggled(bool pressed)
	{
		_settings.Fullscreen = pressed;
		_settings.Apply();
	}

	private void OnInvertYToggled(bool pressed)
	{
		_settings.InvertY = pressed;
	}

	private void UpdateLabels()
	{
		// Sensitivity is stored in radians-per-pixel; show something readable.
		_sensitivityValue.Text = $"Mouse sensitivity: {_settings.MouseSensitivity * 1000.0f:0.0}";
		_volumeValue.Text = $"Master volume: {_settings.MasterVolume * 100.0f:0}%";
	}
}
