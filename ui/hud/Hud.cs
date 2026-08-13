using Godot;

/// <summary>
/// Reads game state each frame and prints it. Deliberately dumb — no game
/// logic lives here, so the HUD can be replaced without touching systems.
/// </summary>
public partial class Hud : CanvasLayer
{
	private Label _status;
	private Label _plots;
	private Label _inventory;
	private Label _prompt;

	private GameClock _clock;
	private Village _village;
	private Player _player;

	/// <summary>Seconds left showing the night's news.</summary>
	private float _noticeTimer;

	public override void _Ready()
	{
		_status = GetNode<Label>("Status");
		_plots = GetNode<Label>("Plots");
		_inventory = GetNode<Label>("Inventory");
		_prompt = GetNode<Label>("Prompt");

		_clock = GetNode<GameClock>("/root/GameClock");
		_village = GetNode<Village>("/root/Village");

		// Without this the player never learns anyone died — it was console-only.
		_village.VillagerConsumed += OnVillagerConsumed;
	}

	/// <summary>
	/// One line per plot: whether it eats tonight, and how far gone it is. With
	/// meat rationed, this is the information the whole day's decision rests on.
	/// </summary>
	private string PlotSummary()
	{
		var lines = new System.Collections.Generic.List<string> { "PLOTS" };
		var crops = new System.Collections.Generic.List<Potato>();

		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			if (node is Potato crop)
			{
				crops.Add(crop);
			}
		}

		crops.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

		foreach (Potato crop in crops)
		{
			string label = crop.Name.ToString().Replace("Potato", "Plot ");

			if (crop.IsAwake)
			{
				lines.Add($"{label}  AWAKE");
				continue;
			}

			int filled = Mathf.RoundToInt(crop.Corruption * 6.0f);
			string bar = new string('#', filled) + new string('.', 6 - filled);
			string fed = crop.FedTonight ? "fed" : "---";

			lines.Add($"{label}  {fed}  [{bar}]  {crop.TimesFed}/{crop.FeedingsToGrow}");
		}

		return string.Join("\n", lines);
	}

	private void OnVillagerConsumed(string villagerName, int remaining)
	{
		_prompt.Text = $"In the night, the soil took {villagerName}.";
		_noticeTimer = 6.0f;
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
		_plots.Text = PlotSummary();

		// A death notice outranks the interact prompt while it is up.
		if (_noticeTimer > 0.0f)
		{
			_noticeTimer -= (float)delta;
			return;
		}

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
