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

	public string PlayerName { get; set; } = "Farmer";

	public void StartNewGame()
	{
		// Autoloads survive scene changes, so a second run would inherit the
		// first one's dead villagers unless we clear them here.
		GetNode<Village>("/root/Village").Reset();
		GetNode<GameClock>("/root/GameClock").Reset();

		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(GameScene);
	}

	public void GoToMainMenu()
	{
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToFile(MenuScene);
	}

	public void GoToLogin()
	{
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToFile(LoginScene);
	}

	public void QuitGame()
	{
		GetTree().Paused = false;
		GetTree().Quit();
	}
}
