using Godot;

/// <summary>
/// Reads game state each frame and prints it. Deliberately dumb — no game
/// logic lives here, so the HUD can be replaced without touching systems.
/// </summary>
public partial class Hud : CanvasLayer
{
	private Label _status;
	private Label _inventory;
	private Label _prompt;

	private GameClock _clock;
	private Village _village;
	private Player _player;

	public override void _Ready()
	{
		_status = GetNode<Label>("Status");
		_inventory = GetNode<Label>("Inventory");
		_prompt = GetNode<Label>("Prompt");

		_clock = GetNode<GameClock>("/root/GameClock");
		_village = GetNode<Village>("/root/Village");
	}

	public override void _Process(double delta)
	{
		_status.Text = $"Day {_clock.Day} — {(_clock.IsNight ? "NIGHT" : "day")}\nTown: {_village.Population}";

		if (_player != null)
		{
			_status.Text += $"\nGold: {_player.Gold}";
		}

		// The player may not be in the tree yet on the first frame.
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			return;
		}

		_inventory.Text = _player.Inventory.Summary();

		IInteractable target = _player.FindNearestInteractable();
		if (target == null)
		{
			_prompt.Text = "";
			return;
		}

		// Only warn about feed when feeding is actually what E would do.
		bool needsFeed = target is Potato { IsFullyGrown: false };
		_prompt.Text = needsFeed && _player.Inventory.BestFeed() == null
			? "Nothing to feed it."
			: $"[E] {target.InteractPrompt}";
	}
}
