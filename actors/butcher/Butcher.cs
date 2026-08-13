using Godot;
using System.Collections.Generic;

/// <summary>
/// The stall at the edge of the farm. Holds a limited daily stock — you cannot
/// feed every plot every day, so each morning is a choice about which ones eat.
/// Closes for good once there is nobody left in town to run it.
/// </summary>
public partial class Butcher : StaticBody3D, IInteractable
{
	/// <summary>Cuts of raw meat available each day.</summary>
	[Export] public int DailyRawMeat = 4;

	/// <summary>Cured meat is stronger and far scarcer.</summary>
	[Export] public int DailyCuredMeat = 1;

	private Village _village;
	private GameClock _clock;

	private readonly Dictionary<string, int> _stock = new();

	/// <summary>Aldis runs the stall; Meru can take it over. Lose both and it never opens again.</summary>
	private bool HasShopkeeper =>
		_village != null && (_village.IsAlive(Village.Butcher) || _village.IsAlive(Village.ButcherHeir));

	public bool CanInteract => HasShopkeeper;

	public string InteractPrompt
	{
		get
		{
			if (HasShopkeeper)
			{
				return _village.IsAlive(Village.Butcher) ? "Aldis, the butcher" : "Meru, minding her father's stall";
			}

			return "The stall is shuttered. Nobody is coming.";
		}
	}

	public override void _Ready()
	{
		AddToGroup("interactable");
		AddToGroup("butcher");

		_village = GetNode<Village>("/root/Village");
		_clock = GetNode<GameClock>("/root/GameClock");
		_clock.DayBroke += OnDayBroke;

		Restock();

		// A loaded run restores whatever was left on the counter.
		GetNode<SaveGame>("/root/SaveGame").ApplyToButcher(this);
	}

	/// <summary>Restores saved counter stock.</summary>
	public void SetStock(int raw, int cured)
	{
		_stock[ItemDatabase.RawMeat.Id] = Mathf.Max(0, raw);
		_stock[ItemDatabase.CuredMeat.Id] = Mathf.Max(0, cured);
	}

	private void OnDayBroke(int day) => Restock();

	private void Restock()
	{
		_stock[ItemDatabase.RawMeat.Id] = DailyRawMeat;
		_stock[ItemDatabase.CuredMeat.Id] = DailyCuredMeat;
	}

	public int StockOf(string itemId) => _stock.GetValueOrDefault(itemId, 0);

	/// <summary>Takes one off the counter. False when he has none left today.</summary>
	public bool TryTakeStock(string itemId)
	{
		if (StockOf(itemId) <= 0)
		{
			return false;
		}

		_stock[itemId]--;
		return true;
	}

	/// <summary>Puts one back — used when a sale is rolled back.</summary>
	public void ReturnStock(string itemId)
	{
		if (_stock.ContainsKey(itemId))
		{
			_stock[itemId]++;
		}
	}

	public void Interact(Player player)
	{
		if (!CanInteract)
		{
			return;
		}

		if (GetTree().GetFirstNodeInGroup("shop") is ShopUi shop)
		{
			shop.Open(player, this);
		}
		else
		{
			GD.PushWarning("No ShopUi in the 'shop' group — is ui/shop/shop.tscn in the scene?");
		}
	}
}
