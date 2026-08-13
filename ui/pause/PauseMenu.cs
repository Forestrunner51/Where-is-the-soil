using Godot;

/// <summary>
/// In-game pause. Runs with ProcessMode.Always so it keeps responding while
/// the rest of the tree is frozen.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	private GameSession _session;
	private SettingsPanel _settings;
	private QuitConfirm _quitConfirm;

	private Button _resume;
	private Button _saveNow;
	private Button _settingsButton;
	private Button _mainMenu;
	private Button _quit;

	public bool IsPaused => Visible;

	public override void _Ready()
	{
		_session = GetNode<GameSession>("/root/GameSession");
		_settings = GetNode<SettingsPanel>("SettingsPanel");
		_quitConfirm = GetNode<QuitConfirm>("QuitConfirm");

		_resume = GetNode<Button>("Panel/Layout/Resume");
		_saveNow = GetNode<Button>("Panel/Layout/SaveNow");
		_settingsButton = GetNode<Button>("Panel/Layout/Settings");
		_mainMenu = GetNode<Button>("Panel/Layout/MainMenu");
		_quit = GetNode<Button>("Panel/Layout/Quit");

		_resume.Pressed += Resume;

		// Autosave only fires at dawn — without this, quitting mid-day loses it.
		_saveNow.Pressed += () =>
		{
			GetNode<SaveGame>("/root/SaveGame").Save(GetTree());
			_saveNow.Text = "Saved";
		};

		_settingsButton.Pressed += () => _settings.Open();

		_mainMenu.Pressed += () =>
		{
			_pendingExit = ExitKind.MainMenu;
			_quitConfirm.Open("Abandon this farm and return to the menu?");
		};

		_quit.Pressed += () =>
		{
			_pendingExit = ExitKind.Desktop;
			_quitConfirm.Open("Quit to desktop?");
		};

		_settings.Closed += () => _resume.GrabFocus();
		_quitConfirm.Confirmed += OnQuitConfirmed;

		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
		{
			return;
		}

		// Sub-overlays own Escape while they are up.
		if (_settings.Visible || _quitConfirm.Visible)
		{
			return;
		}

		// The shop closes on Escape before the pause menu gets a say.
		if (GetTree().GetFirstNodeInGroup("shop") is ShopUi { IsOpen: true })
		{
			return;
		}

		if (Visible)
		{
			Resume();
		}
		else
		{
			Pause();
		}

		GetViewport().SetInputAsHandled();
	}

	public void Pause()
	{
		_saveNow.Text = "Save";
		Visible = true;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_resume.GrabFocus();
	}

	public void Resume()
	{
		Visible = false;
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	/// <summary>Which button opened the confirm dialog — decides what "yes" means.</summary>
	private enum ExitKind
	{
		MainMenu,
		Desktop,
	}

	private ExitKind _pendingExit = ExitKind.Desktop;

	private void OnQuitConfirmed()
	{
		if (_pendingExit == ExitKind.MainMenu)
		{
			_session.GoToMainMenu();
		}
		else
		{
			_session.QuitGame();
		}
	}
}
