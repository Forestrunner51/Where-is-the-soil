using Godot;

public partial class MainMenu : Control
{
	private GameSession _session;
	private SettingsPanel _settings;
	private QuitConfirm _quitConfirm;

	private Label _greeting;
	private Button _continue;
	private Button _newGame;
	private Button _settingsButton;
	private Button _switchProfile;
	private Button _quit;

	public override void _Ready()
	{
		_session = GetNode<GameSession>("/root/GameSession");
		_settings = GetNode<SettingsPanel>("SettingsPanel");
		_quitConfirm = GetNode<QuitConfirm>("QuitConfirm");

		_greeting = GetNode<Label>("Center/Layout/Greeting");
		_continue = GetNode<Button>("Center/Layout/Continue");
		_newGame = GetNode<Button>("Center/Layout/NewGame");
		_settingsButton = GetNode<Button>("Center/Layout/Settings");
		_switchProfile = GetNode<Button>("Center/Layout/SwitchProfile");
		_quit = GetNode<Button>("Center/Layout/Quit");

		_continue.Pressed += () => _session.ContinueGame();
		_newGame.Pressed += () => _session.StartNewGame();
		_settingsButton.Pressed += () => _settings.Open();
		_switchProfile.Pressed += () => _session.GoToLogin();
		_quit.Pressed += () => _quitConfirm.Open("Quit to desktop?");

		_quitConfirm.Confirmed += () => _session.QuitGame();
		_settings.Closed += () => _newGame.GrabFocus();

		Input.MouseMode = Input.MouseModeEnum.Visible;

		_greeting.Text = $"{_session.PlayerName}'s farm";

		// Nothing to resume on a fresh install, or after a run has ended.
		bool hasSave = SaveGame.Exists();
		_continue.Disabled = !hasSave;
		_continue.Visible = hasSave;

		if (hasSave)
		{
			_continue.GrabFocus();
		}
		else
		{
			_newGame.GrabFocus();
		}
	}
}
