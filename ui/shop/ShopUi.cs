using Godot;

/// <summary>
/// The butcher's menu. Buying and selling both run through <see cref="ItemData.Price"/>:
/// positive means he charges you, negative means he pays.
/// </summary>
public partial class ShopUi : CanvasLayer
{
	private Player _player;
	private Village _village;

	private Label _gold;
	private Label _message;
	private Button _buyRaw;
	private Button _buyCured;
	private Button _sellPotato;
	private Button _close;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		AddToGroup("shop");

		_village = GetNode<Village>("/root/Village");

		_gold = GetNode<Label>("Panel/Layout/Gold");
		_message = GetNode<Label>("Panel/Layout/Message");
		_buyRaw = GetNode<Button>("Panel/Layout/BuyRaw");
		_buyCured = GetNode<Button>("Panel/Layout/BuyCured");
		_sellPotato = GetNode<Button>("Panel/Layout/SellPotato");
		_close = GetNode<Button>("Panel/Layout/Close");

		_buyRaw.Pressed += () => Buy(ItemDatabase.RawMeat);
		_buyCured.Pressed += () => Buy(ItemDatabase.CuredMeat);
		_sellPotato.Pressed += () => Sell(ItemDatabase.Potato);
		_close.Pressed += Close;

		Visible = false;
	}

	public void Open(Player player)
	{
		_player = player;
		_player.UiOpen = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Visible = true;
		_message.Text = Greeting();
		Refresh();
	}

	public void Close()
	{
		Visible = false;

		if (_player != null)
		{
			_player.UiOpen = false;
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
		{
			return;
		}

		if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("interact"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>Who is behind the counter, and how much they have stopped pretending.</summary>
	private string Greeting()
	{
		if (!_village.IsAlive(Village.Butcher))
		{
			return _village.Population <= 4
				? "Meru does not look up from the counter."
				: "\"Father is... he isn't here. I can serve you.\"";
		}

		return _village.Population <= 4
			? "He does not ask how the crop is doing."
			: "\"Fresh in this morning.\"";
	}

	private void Buy(ItemData item)
	{
		if (!_player.TrySpend(item.Price))
		{
			_message.Text = "You cannot afford that.";
			return;
		}

		int leftover = _player.Inventory.Add(item);
		if (leftover > 0)
		{
			// Refund rather than silently eating the coin.
			_player.AddGold(item.Price);
			_message.Text = "You have no room.";
			return;
		}

		_message.Text = $"Bought {item.DisplayName}.";
		Refresh();
	}

	private void Sell(ItemData item)
	{
		int payout = -item.Price;
		if (payout <= 0)
		{
			_message.Text = "He does not want that.";
			return;
		}

		if (!_player.Inventory.TryConsume(item.Id))
		{
			_message.Text = $"You have no {item.DisplayName.ToLower()}.";
			return;
		}

		_player.AddGold(payout);
		_message.Text = $"Sold {item.DisplayName} for {payout}g.";
		Refresh();
	}

	private void Refresh()
	{
		_gold.Text = $"Gold: {_player.Gold}     Potatoes: {_player.Inventory.CountOf(ItemDatabase.Potato.Id)}";
		_buyRaw.Text = $"Buy {ItemDatabase.RawMeat.DisplayName}  —  {ItemDatabase.RawMeat.Price}g";
		_buyCured.Text = $"Buy {ItemDatabase.CuredMeat.DisplayName}  —  {ItemDatabase.CuredMeat.Price}g";
		_sellPotato.Text = $"Sell {ItemDatabase.Potato.DisplayName}  —  {-ItemDatabase.Potato.Price}g";
	}
}
