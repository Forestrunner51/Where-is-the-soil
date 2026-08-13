using Godot;

/// <summary>
/// Owns who is playing and which scene is up. All screen transitions go through
/// here so state that must be reset between runs is reset in exactly one place.
/// </summary>
public partial class GameSession : Node
{
	public const string LoginScene = "res://ui/menus/login.tscn";
	public const string MenuScene = "res://ui/menus/main_menu.tscn";
	public const string GameScene = "res://world/main.tscn";
	public const string EndingScene = "res://ui/menus/ending.tscn";

	/// <summary>How a run finished.</summary>
	public enum Ending
	{
		/// <summary>Reached the last day with the town still standing.</summary>
		Survived,

		/// <summary>The town ran out. There was nothing left to feed the field but you.</summary>
		Consumed,
	}

	public string PlayerName { get; set; } = "Farmer";

	public Ending LastEnding { get; private set; } = Ending.Consumed;

	/// <summary>Guards against a second ending firing while the first is mid-transition.</summary>
	private bool _ending;

	public void StartNewGame()
	{
		// Autoloads survive scene changes, so a second run would inherit the
		// first one's dead villagers and day count unless we clear them here.
		GetNode<Village>("/root/Village").Reset();
		GetNode<GameClock>("/root/GameClock").Reset();
		GetNode<SaveGame>("/root/SaveGame").Delete();

		_ending = false;
		GetTree().Paused = false;
		GetNode<GameClock>("/root/GameClock").Running = true;
		GetTree().ChangeSceneToFile(GameScene);
	}

	/// <summary>Resumes the autosaved run. Returns false if there was nothing to load.</summary>
	public bool ContinueGame()
	{
		if (!GetNode<SaveGame>("/root/SaveGame").BeginLoad())
		{
			return false;
		}

		_ending = false;
		GetTree().Paused = false;
		GetNode<GameClock>("/root/GameClock").Running = true;
		GetTree().ChangeSceneToFile(GameScene);
		return true;
	}

	public void EndGame(Ending ending)
	{
		if (_ending)
		{
			return;
		}

		_ending = true;
		LastEnding = ending;
		GD.Print($"=== Ending: {ending} ===");

		// The run is over — no resuming it.
		GetNode<SaveGame>("/root/SaveGame").Delete();

		GetTree().Paused = false;
		GetNode<GameClock>("/root/GameClock").Running = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToFile(EndingScene);
	}

	public void GoToMainMenu()
	{
		GetTree().Paused = false;
		GetNode<GameClock>("/root/GameClock").Running = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToFile(MenuScene);
	}

	public void GoToLogin()
	{
		GetTree().Paused = false;
		GetNode<GameClock>("/root/GameClock").Running = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToFile(LoginScene);
	}

	public void QuitGame()
	{
		GetTree().Paused = false;
		GetTree().Quit();
	}
}
