using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoloaded singleton holding the town's remaining population. Unfed crops
/// take from here at night. When it empties, they have to look elsewhere.
/// </summary>
public partial class Village : Node
{
	[Signal] public delegate void VillagerConsumedEventHandler(string villagerName, int remaining);

	/// <summary>Emitted when the last villager is taken — nothing left to feed the soil but you.</summary>
	[Signal] public delegate void VillageEmptiedEventHandler();

	private static readonly string[] StartingVillagers =
	{
		"Aldis the butcher",
		"Meru, his daughter",
		"Old Hesk",
		"Bramwell",
		"the well-keeper",
		"Sada",
		"the boy who runs errands",
		"Corrin",
		"the widow Vale",
		"Tam",
		"the priest",
		"Nell",
	};

	private readonly List<string> _villagers = new(StartingVillagers);

	/// <summary>Repopulates the town. Called when a new run starts.</summary>
	public void Reset()
	{
		_villagers.Clear();
		_villagers.AddRange(StartingVillagers);
	}

	public int Population => _villagers.Count;
	public bool IsEmpty => _villagers.Count == 0;

	/// <summary>
	/// Takes one villager. Returns their name, or null if the town is already empty.
	/// </summary>
	public string ConsumeVillager()
	{
		if (IsEmpty)
		{
			return null;
		}

		// Random so you never learn a safe order — anyone can be next.
		int index = (int)(GD.Randi() % (uint)_villagers.Count);
		string name = _villagers[index];
		_villagers.RemoveAt(index);

		GD.Print($"In the night, the soil took {name}. {Population} left.");
		EmitSignal(SignalName.VillagerConsumed, name, Population);

		if (IsEmpty)
		{
			GD.Print("The town is empty. The shop does not open.");
			EmitSignal(SignalName.VillageEmptied);
		}

		return name;
	}
}
