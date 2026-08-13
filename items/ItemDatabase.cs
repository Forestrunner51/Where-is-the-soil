using System.Collections.Generic;

/// <summary>
/// Every item in the game, keyed by id. Defined in code so there are no
/// resource files to keep in sync while the design is still moving.
/// </summary>
public static class ItemDatabase
{
	public static readonly ItemData RawMeat = new()
	{
		Id = "raw_meat",
		DisplayName = "Raw meat",
		MaxStack = 20,
		FeedValue = 0.2f,
		Price = 8,
		Description = "The butcher never says what animal. You have stopped asking.",
	};

	public static readonly ItemData CuredMeat = new()
	{
		Id = "cured_meat",
		DisplayName = "Cured meat",
		MaxStack = 20,
		FeedValue = 0.5f,
		Price = 25,
		Description = "Keeps longer. The soil prefers it, which is its own kind of answer.",
	};

	public static readonly ItemData Potato = new()
	{
		Id = "potato",
		DisplayName = "Potato",
		MaxStack = 20,
		FeedValue = 0.0f,
		// Negative: the butcher pays you. 15 against 24g of feed per harvest cycle
		// leaves a thin honest margin, so corrupting a crop is a choice, not the
		// only way to stay solvent.
		Price = -15,
		Description = "Heavier than it looks.",
	};

	/// <summary>
	/// What a plot gives up once it has turned. Worth three clean harvests —
	/// letting one plot rot is how you afford to feed the rest.
	/// </summary>
	public static readonly ItemData CorruptPotato = new()
	{
		Id = "corrupt_potato",
		DisplayName = "Dark potato",
		MaxStack = 20,
		FeedValue = 0.0f,
		Price = -45,
		Description = "Heavy, and warm on one side. The butcher takes it without a word.",
	};

	private static readonly Dictionary<string, ItemData> ById = new()
	{
		[RawMeat.Id] = RawMeat,
		[CuredMeat.Id] = CuredMeat,
		[Potato.Id] = Potato,
		[CorruptPotato.Id] = CorruptPotato,
	};

	public static ItemData Get(string id) => ById.GetValueOrDefault(id);

	public static IEnumerable<ItemData> All => ById.Values;
}
