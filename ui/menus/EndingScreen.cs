using Godot;

public partial class EndingScreen : Control
{
	private GameSession _session;
	private Village _village;
	private GameClock _clock;

	private Label _title;
	private Label _body;
	private Label _epitaph;
	private Button _menu;
	private Button _quit;

	public override void _Ready()
	{
		_session = GetNode<GameSession>("/root/GameSession");
		_village = GetNode<Village>("/root/Village");
		_clock = GetNode<GameClock>("/root/GameClock");

		_title = GetNode<Label>("Center/Layout/Title");
		_body = GetNode<Label>("Center/Layout/Body");
		_epitaph = GetNode<Label>("Center/Layout/Epitaph");
		_menu = GetNode<Button>("Center/Layout/MainMenu");
		_quit = GetNode<Button>("Center/Layout/Quit");

		_menu.Pressed += () => _session.GoToMainMenu();
		_quit.Pressed += () => _session.QuitGame();

		Input.MouseMode = Input.MouseModeEnum.Visible;
		Show(_session.LastEnding);
		_menu.GrabFocus();
	}

	private void Show(GameSession.Ending ending)
	{
		if (ending == GameSession.Ending.Survived)
		{
			_title.Text = "THE SEASON ENDS";
			_body.Text =
				$"{_session.PlayerName} worked the plot to the last day and the field never took more than it was given.";
			_epitaph.Text = _village.Population == 12
				? "Everyone is still here. Nobody will believe you."
				: $"{_village.Population} still live in the town. They do not talk about the rest.";
			return;
		}

		_title.Text = "THE TOWN IS EMPTY";
		_body.Text =
			$"There was nobody left to give it, so on the {Ordinal(_clock.Day)} night the field came to the house instead.";
		_epitaph.Text = $"{_session.PlayerName} fed the soil for {_clock.Day} days. Then the soil fed itself.";
	}

	private static string Ordinal(int value)
	{
		if (value % 100 is >= 11 and <= 13)
		{
			return $"{value}th";
		}

		return (value % 10) switch
		{
			1 => $"{value}st",
			2 => $"{value}nd",
			3 => $"{value}rd",
			_ => $"{value}th",
		};
	}
}
