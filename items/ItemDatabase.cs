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
		Price = -12, // negative: the butcher pays you for these
		Description = "Heavier than it looks.",
	};

	private static readonly Dictionary<string, ItemData> ById = new()
	{
		[RawMeat.Id] = RawMeat,
		[CuredMeat.Id] = CuredMeat,
		[Potato.Id] = Potato,
	};

	public static ItemData Get(string id) => ById.GetValueOrDefault(id);

	public static IEnumerable<ItemData> All => ById.Values;
}
