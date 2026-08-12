using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Writes and restores a run. Autosaves every dawn, so the thing you lose by
/// quitting is at most one day.
/// </summary>
public partial class SaveGame : Node
{
	private const string SavePath = "user://save.cfg";

	/// <summary>Parsed save waiting to be applied by nodes as they enter the tree.</summary>
	private ConfigFile _pending;

	public bool HasPendingLoad => _pending != null;

	public static bool Exists() => FileAccess.FileExists(SavePath);

	public void Delete()
	{
		if (Exists())
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
		}

		_pending = null;
	}

	public void Save(SceneTree tree)
	{
		var config = new ConfigFile();

		var clock = GetNode<GameClock>("/root/GameClock");
		var village = GetNode<Village>("/root/Village");
		var session = GetNode<GameSession>("/root/GameSession");

		config.SetValue("meta", "name", session.PlayerName);
		config.SetValue("meta", "day", clock.Day);
		config.SetValue("village", "survivors", village.SurvivorNames());

		if (tree.GetFirstNodeInGroup("player") is Player player)
		{
			config.SetValue("player", "gold", player.Gold);

			var items = new Godot.Collections.Dictionary();
			foreach (ItemStack stack in player.Inventory.Stacks)
			{
				items[stack.Item.Id] = items.TryGetValue(stack.Item.Id, out Variant had)
					? had.AsInt32() + stack.Count
					: stack.Count;
			}

			config.SetValue("player", "items", items);
		}

		foreach (Node node in tree.GetNodesInGroup("crop"))
		{
			if (node is not Potato crop)
			{
				continue;
			}

			config.SetValue("crops", crop.Name, new Godot.Collections.Dictionary
			{
				{ "fed", crop.TimesFed },
				{ "corruption", crop.Corruption },
				{ "awake", crop.IsAwake },
				{ "fedTonight", crop.FedTonight },
			});
		}

		config.Save(SavePath);
		GD.Print($"Saved — day {clock.Day}, {village.Population} in town.");
	}

	/// <summary>
	/// Reads the file and restores the autoloads immediately. Scene-level state
	/// (player, crops) is held until those nodes call Apply* from _Ready.
	/// </summary>
	public bool BeginLoad()
	{
		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
		{
			GD.PushWarning("No save to load.");
			return false;
		}

		_pending = config;

		GetNode<GameSession>("/root/GameSession").PlayerName =
			config.GetValue("meta", "name", "Farmer").AsString();
		GetNode<GameClock>("/root/GameClock").SetDay(config.GetValue("meta", "day", 1).AsInt32());

		string[] survivors = config.GetValue("village", "survivors", System.Array.Empty<string>())
			.AsStringArray();
		GetNode<Village>("/root/Village").KeepOnly(survivors.ToList());

		return true;
	}

	public void ApplyToPlayer(Player player)
	{
		if (_pending == null)
		{
			return;
		}

		player.LoadGold(_pending.GetValue("player", "gold", 0).AsInt32());
		player.Inventory.Clear();

		var items = _pending.GetValue("player", "items", new Godot.Collections.Dictionary())
			.AsGodotDictionary();

		foreach (KeyValuePair<Variant, Variant> entry in items)
		{
			ItemData item = ItemDatabase.Get(entry.Key.AsString());
			if (item != null)
			{
				player.Inventory.Add(item, entry.Value.AsInt32());
			}
		}
	}

	public void ApplyToCrop(Potato crop)
	{
		if (_pending == null || !_pending.HasSectionKey("crops", crop.Name))
		{
			return;
		}

		var state = _pending.GetValue("crops", crop.Name).AsGodotDictionary();
		crop.LoadState(
			state["fed"].AsInt32(),
			state["corruption"].AsSingle(),
			state["awake"].AsBool(),
			state["fedTonight"].AsBool());
	}

	/// <summary>Called once the scene is fully built, so a later run starts clean.</summary>
	public void EndLoad() => _pending = null;
}
