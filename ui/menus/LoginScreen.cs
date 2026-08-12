using Godot;

/// <summary>
/// Names the farmer. No accounts and no server — this is a local profile, so
/// there is nothing to authenticate against; it just carries a name forward.
/// </summary>
public partial class LoginScreen : Control
{
	private const string LastNameKey = "last_name";
	private const string ProfilePath = "user://profile.cfg";

	private LineEdit _nameField;
	private Button _continue;
	private Label _hint;

	private GameSession _session;

	public override void _Ready()
	{
		_session = GetNode<GameSession>("/root/GameSession");

		_nameField = GetNode<LineEdit>("Center/Layout/NameField");
		_continue = GetNode<Button>("Center/Layout/Continue");
		_hint = GetNode<Label>("Center/Layout/Hint");

		_continue.Pressed += Submit;
		_nameField.TextSubmitted += _ => Submit();

		Input.MouseMode = Input.MouseModeEnum.Visible;

		_nameField.Text = LoadLastName();
		_nameField.GrabFocus();
		_nameField.SelectAll();
	}

	private void Submit()
	{
		string name = _nameField.Text.StripEdges();

		if (string.IsNullOrEmpty(name))
		{
			_hint.Text = "The soil wants a name.";
			return;
		}

		_session.PlayerName = name;
		SaveLastName(name);
		GetTree().ChangeSceneToFile(GameSession.MenuScene);
	}

	private static string LoadLastName()
	{
		var config = new ConfigFile();
		return config.Load(ProfilePath) == Error.Ok
			? config.GetValue("profile", LastNameKey, "").AsString()
			: "";
	}

	private static void SaveLastName(string name)
	{
		var config = new ConfigFile();
		config.Load(ProfilePath);
		config.SetValue("profile", LastNameKey, name);
		config.Save(ProfilePath);
	}
}
