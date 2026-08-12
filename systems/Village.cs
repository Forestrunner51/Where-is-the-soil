using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>One named resident. Ids exist only for the few who matter mechanically.</summary>
public class Villager
{
	public string Name;
	public string Role;

	/// <summary>Printed the morning after they are taken.</summary>
	public string Epitaph;

	/// <summary>Lookup key for villagers with a mechanical role. Empty for the rest.</summary>
	public string Id = "";
}

/// <summary>
/// Autoloaded singleton holding the town's remaining residents. The soil takes
/// at most one per night — see <see cref="NightResolver"/>.
/// </summary>
public partial class Village : Node
{
	[Signal] public delegate void VillagerConsumedEventHandler(string villagerName, int remaining);

	/// <summary>Emitted when the last resident is taken — nothing left to feed the soil but you.</summary>
	[Signal] public delegate void VillageEmptiedEventHandler();

	public const string Butcher = "butcher";
	public const string ButcherHeir = "butcher_heir";
	public const string Priest = "priest";

	private static readonly Villager[] StartingVillagers =
	{
		new()
		{
			Name = "Aldis", Role = "the butcher", Id = Butcher,
			Epitaph = "His apron is folded on the counter. Still warm.",
		},
		new()
		{
			Name = "Meru", Role = "the butcher's daughter", Id = ButcherHeir,
			Epitaph = "She had already learned the trade. She was going to run the stall one day.",
		},
		new()
		{
			Name = "Father Osric", Role = "the priest", Id = Priest,
			Epitaph = "The chapel door stands open. Nobody will say the words over the field now.",
		},
		new()
		{
			Name = "Hesk", Role = "the old ploughman",
			Epitaph = "He warned your grandfather about this plot. Nobody listened then either.",
		},
		new()
		{
			Name = "Bramwell", Role = "the miller",
			Epitaph = "The wheel turns all night with nothing to grind.",
		},
		new()
		{
			Name = "Corrin", Role = "the well-keeper",
			Epitaph = "The bucket came up full of soil.",
		},
		new()
		{
			Name = "Sada", Role = "the weaver",
			Epitaph = "Her loom is strung with something that is not thread.",
		},
		new()
		{
			Name = "Pip", Role = "the errand boy",
			Epitaph = "He was fast. It did not matter.",
		},
		new()
		{
			Name = "the widow Vale", Role = "the herbalist",
			Epitaph = "She stopped selling seed years ago. She knew what grew here.",
		},
		new()
		{
			Name = "Tam", Role = "the fencewright",
			Epitaph = "He built the posts around your field. They did not hold.",
		},
		new()
		{
			Name = "Nell", Role = "the innkeeper",
			Epitaph = "The lamps are lit and the tables are set for nobody.",
		},
		new()
		{
			Name = "Garrow", Role = "the gravedigger",
			Epitaph = "There was no grave. There is never a grave.",
		},
	};

	private readonly List<Villager> _villagers = new(StartingVillagers);

	public int Population => _villagers.Count;
	public bool IsEmpty => _villagers.Count == 0;

	public IReadOnlyList<Villager> Residents => _villagers;

	/// <summary>Repopulates the town. Called when a new run starts.</summary>
	public void Reset()
	{
		_villagers.Clear();
		_villagers.AddRange(StartingVillagers);
	}

	public bool IsAlive(string id) => _villagers.Any(v => v.Id == id);

	/// <summary>
	/// Takes one resident. Returns them, or null if the town is already empty.
	/// </summary>
	public Villager ConsumeVillager()
	{
		if (IsEmpty)
		{
			return null;
		}

		// Random so you never learn a safe order — anyone can be next.
		int index = (int)(GD.Randi() % (uint)_villagers.Count);
		Villager taken = _villagers[index];
		_villagers.RemoveAt(index);

		GD.Print($"In the night, the soil took {taken.Name}, {taken.Role}. {Population} left.");
		GD.Print($"  {taken.Epitaph}");
		EmitSignal(SignalName.VillagerConsumed, taken.Name, Population);

		if (IsEmpty)
		{
			GD.Print("The town is empty. Nothing is left to take but you.");
			EmitSignal(SignalName.VillageEmptied);
		}

		return taken;
	}
}
