using Godot;

public partial class Potato : StaticBody3D, IInteractable
{
	/// <summary>How many feedings it takes to be fully grown.</summary>
	[Export] public int FeedingsToGrow = 3;

	/// <summary>Corruption added by a night that went unfed. At 1.0 it wakes up.</summary>
	[Export] public float NeglectCorruption = 0.34f;

	/// <summary>Fallback relief, used only if something feeds it with no item.</summary>
	[Export] public float FeedRelief = 0.2f;

	/// <summary>Potatoes yielded by a clean harvest.</summary>
	[Export] public int Yield = 2;

	/// <summary>Extra potatoes a corrupted crop gives up. Bigger harvest, worse neighbour.</summary>
	[Export] public int CorruptBonusYield = 2;

	[Export] public Color DryColor = new(0.55f, 0.42f, 0.28f);
	[Export] public Color FedColor = new(0.35f, 0.65f, 0.25f);
	[Export] public Color CorruptColor = new(0.35f, 0.05f, 0.12f);

	private int _timesFed;
	private MeshInstance3D _mesh;
	private StandardMaterial3D _material;

	/// <summary>0 = healthy, 1 = awake.</summary>
	public float Corruption { get; private set; }

	/// <summary>Reset every dawn. Feed before dark or the soil feeds itself.</summary>
	public bool FedTonight { get; private set; }

	public bool IsAwake { get; private set; }

	/// <summary>True if tonight would corrupt it. Read by <see cref="NightResolver"/>.</summary>
	public bool NeedsFeeding => !IsAwake && !FedTonight;

	/// <summary>Why the last feed attempt failed, for the HUD to show. Null if it worked.</summary>
	public string LastRefusal { get; private set; }
	public bool IsFullyGrown => _timesFed >= FeedingsToGrow;

	public bool CanInteract => !IsAwake;

	public string InteractPrompt
	{
		get
		{
			if (IsAwake) return "";
			if (IsFullyGrown) return "Harvest";
			if (Corruption >= 0.66f) return "Feed me.";
			if (Corruption >= 0.33f) return "Feed";
			return FedTonight ? $"Fed ({_timesFed}/{FeedingsToGrow})" : $"Feed ({_timesFed}/{FeedingsToGrow})";
		}
	}

	public override void _Ready()
	{
		AddToGroup("interactable");

		// NightResolver settles the whole field through this group.
		AddToGroup("crop");

		// FindChild searches recursively, so this still works if the mesh gets
		// reparented out of the Area3D later.
		_mesh = FindChild("MeshInstance3D") as MeshInstance3D;
		if (_mesh == null)
		{
			GD.PushWarning($"{Name}: no MeshInstance3D found, feeding will have no visible effect.");
			return;
		}

		// Override rather than editing the mesh's own material — the mesh
		// resource is shared between every potato instance.
		_material = new StandardMaterial3D { AlbedoColor = DryColor };
		_mesh.MaterialOverride = _material;
	}

	public void Interact(Player player)
	{
		if (IsAwake)
		{
			return;
		}

		if (IsFullyGrown)
		{
			Harvest(player);
			return;
		}

		ItemData feed = player.Inventory.BestFeed();
		if (feed == null)
		{
			LastRefusal = "Nothing to feed it.";
			GD.Print(LastRefusal);
			return;
		}

		// All-or-nothing, so a failed feed never silently eats the item.
		if (!player.Inventory.TryConsume(feed.Id))
		{
			return;
		}

		LastRefusal = null;
		FedTonight = true;
		Corruption = Mathf.Max(0.0f, Corruption - feed.FeedValue);

		if (!IsFullyGrown)
		{
			_timesFed++;
		}

		GD.Print($"Fed {Name} {feed.DisplayName} ({_timesFed}/{FeedingsToGrow}). Corruption {Corruption:P0}.");
		Refresh();
	}

	/// <summary>
	/// Pulls the crop and resets it to seed. Corruption stays in the soil — the
	/// plot remembers, so a bad plot keeps demanding meat forever.
	/// </summary>
	private void Harvest(Player player)
	{
		// The temptation: a corrupted crop pays better.
		int amount = Yield + (int)(CorruptBonusYield * Corruption);
		int leftover = player.Inventory.Add(ItemDatabase.Potato, amount);

		if (leftover == amount)
		{
			LastRefusal = "No room to carry it.";
			GD.Print(LastRefusal);
			return;
		}

		LastRefusal = null;
		_timesFed = 0;
		GD.Print($"Harvested {amount - leftover} potato(es) from {Name}.");
		Refresh();
	}

	/// <summary>
	/// A night went unfed. Called by <see cref="NightResolver"/>, which decides
	/// separately what the town pays.
	/// </summary>
	public void ResolveNight(float corruptionMultiplier = 1.0f)
	{
		if (!NeedsFeeding)
		{
			return;
		}

		Corruption = Mathf.Min(1.0f, Corruption + NeglectCorruption * corruptionMultiplier);

		if (Corruption >= 1.0f)
		{
			Awaken();
		}

		Refresh();
	}

	/// <summary>Nothing left in town to take, so it takes what it can reach.</summary>
	public void Starve()
	{
		Corruption = 1.0f;
		Awaken();
		Refresh();
	}

	/// <summary>Dawn — the debt resets and it wants feeding again.</summary>
	public void BeginDay()
	{
		FedTonight = false;
	}

	private void Awaken()
	{
		if (IsAwake)
		{
			return;
		}

		IsAwake = true;
		RemoveFromGroup("interactable");
		GD.Print($"{Name} has woken up.");
		Refresh();
	}

	private void Refresh()
	{
		if (_mesh == null)
		{
			return;
		}

		float growth = FeedingsToGrow > 0 ? (float)_timesFed / FeedingsToGrow : 1.0f;

		// Healthy growth reads green; corruption drags the whole thing red.
		Color healthy = DryColor.Lerp(FedColor, growth);
		_material.AlbedoColor = healthy.Lerp(CorruptColor, Corruption);

		// Scale the mesh, not the body — scaling a physics body upsets Jolt.
		float size = Mathf.Lerp(1.0f, 1.5f, growth) + (IsAwake ? 1.0f : Corruption * 0.4f);
		_mesh.Scale = Vector3.One * size;
	}
}
