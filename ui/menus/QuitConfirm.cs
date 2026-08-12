using Godot;

/// <summary>
/// "Are you sure" overlay for leaving. Shared by the main menu and the pause
/// menu — it asks, and reports the answer; the caller decides what quitting means.
/// </summary>
public partial class QuitConfirm : CanvasLayer
{
	[Signal] public delegate void ConfirmedEventHandler();
	[Signal] public delegate void CancelledEventHandler();

	private Label _message;
	private Button _yes;
	private Button _no;

	public override void _Ready()
	{
		_message = GetNode<Label>("Panel/Layout/Message");
		_yes = GetNode<Button>("Panel/Layout/Yes");
		_no = GetNode<Button>("Panel/Layout/No");

		_yes.Pressed += () => EmitSignal(SignalName.Confirmed);
		_no.Pressed += Close;

		Visible = false;
	}

	public void Open(string message = "Leave the farm?")
	{
		_message.Text = message;
		Visible = true;
		_no.GrabFocus();
	}

	public void Close()
	{
		Visible = false;
		EmitSignal(SignalName.Cancelled);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
