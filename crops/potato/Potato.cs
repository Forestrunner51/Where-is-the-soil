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

	/// <summary>Past this much corruption the harvest comes up dark, and sells for far more.</summary>
	[Export] public float CorruptHarvestThreshold = 0.5f;

	/// <summary>Feeding after dark is worth this much more. The reason to be out there.</summary>
	[Export] public float NightFeedBonus = 2.0f;

	[Export] public string BeastScenePath = "res://actors/beast/beast.tscn";

	[Export] public Color DryColor = new(0.55f, 0.42f, 0.28f);
	[Export] public Color FedColor = new(0.35f, 0.65f, 0.25f);
	[Export] public Color CorruptColor = new(0.35f, 0.05f, 0.12f);

	private int _timesFed;
	private MeshInstance3D _mesh;
	private StandardMaterial3D _material;
	/// <summary>Corruption at which the plot starts breathing audibly.</summary>
	[Export] public float BreathThreshold = 0.5f;

	private float _baseScale = 1.0f;
	private GameClock _clock;
	private AudioStreamPlayer3D _breath;

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

			// Harvesting doesn't feed it, and the plot still eats tonight — say so.
			if (IsFullyGrown)
			{
				string crop = Corruption >= CorruptHarvestThreshold ? "dark harvest" : "harvest";
				return FedTonight ? $"Harvest ({crop})" : $"Harvest ({crop}), then feed again";
			}

			string bonus = _clock is { IsNight: true } ? "  [night: double]" : "";

			if (Corruption >= 0.66f) return "Feed me." + bonus;
			if (Corruption >= 0.33f) return "Feed" + bonus;

			return FedTonight
				? $"Fed ({_timesFed}/{FeedingsToGrow})"
				: $"Feed ({_timesFed}/{FeedingsToGrow}){bonus}";
		}
	}

	public override void _Ready()
	{
		AddToGroup("interactable");

		// NightResolver settles the whole field through this group.
		AddToGroup("crop");

		_clock = GetNode<GameClock>("/root/GameClock");

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

		SetUpBreathing();

		GetNode<SaveGame>("/root/SaveGame").ApplyToCrop(this);
	}

	/// <summary>
	/// A corrupted plot breathes. Positional, so you can hear which row it is
	/// coming from before you can see anything wrong.
	/// </summary>
	private void SetUpBreathing()
	{
		// Looping comes from the .import; six plots share this stream, so
		// mutating it here would leak it and stomp the other five.
		var stream = GD.Load<AudioStream>("res://audio/breathing_loop.wav");

		_breath = new AudioStreamPlayer3D
		{
			Name = "Breath",
			Stream = stream,
			UnitSize = 6.0f,
			MaxDistance = 18.0f,
			VolumeDb = -80.0f,
			Position = new Vector3(0.0f, 0.5f, 0.0f),
		};

		AddChild(_breath);
		_breath.Play();
	}

	public override void _ExitTree()
	{
		// Looping playback holds the shared stream open past teardown otherwise.
		_breath?.Stop();
	}

	public override void _Process(double delta)
	{
		if (_breath == null)
		{
			return;
		}

		// Silent below the threshold, rising to full as it turns.
		bool audible = !IsAwake && Corruption >= BreathThreshold;
		float target = audible
			? Mathf.Lerp(-24.0f, -6.0f, Mathf.InverseLerp(BreathThreshold, 1.0f, Corruption))
			: -80.0f;

		_breath.VolumeDb = Mathf.MoveToward(_breath.VolumeDb, target, 30.0f * (float)delta);
	}

	public void Interact(Player player)
	{
		if (IsAwake)
		{
			return;
		}

		// If the harvest can't fit, fall through to feeding — otherwise a full
		// inventory would silently leave the plot unfed and cost a villager.
		if (IsFullyGrown && Harvest(player))
		{
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

		// Tending it in the dark is worth more — and far more dangerous.
		float relief = feed.FeedValue * (_clock.IsNight ? NightFeedBonus : 1.0f);
		Corruption = Mathf.Max(0.0f, Corruption - relief);

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
	private bool Harvest(Player player)
	{
		// The temptation: a crop that has turned pays far better than a clean one.
		int amount = Yield + (int)(CorruptBonusYield * Corruption);
		ItemData crop = Corruption >= CorruptHarvestThreshold
			? ItemDatabase.CorruptPotato
			: ItemDatabase.Potato;

		int leftover = player.Inventory.Add(crop, amount);

		if (leftover == amount)
		{
			LastRefusal = "No room to carry it.";
			GD.Print(LastRefusal);
			return false;
		}

		LastRefusal = null;
		_timesFed = 0;
		GD.Print($"Harvested {amount - leftover} × {crop.DisplayName} from {Name}.");
		Refresh();
		return true;
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

	/// <summary>
	/// Corruption forced on this crop from outside — a beast working the rows.
	/// Unlike neglect, feeding it that day does not prevent this.
	/// </summary>
	public void Tend(float amount)
	{
		if (IsAwake)
		{
			return;
		}

		Corruption = Mathf.Min(1.0f, Corruption + amount);

		if (Corruption >= 1.0f)
		{
			Awaken();
		}

		Refresh();
	}

	/// <summary>
	/// Puts a woken plot back in the ground. The beast is destroyed and the plot
	/// becomes workable again — but the soil keeps half its corruption, so a
	/// buried plot is always closer to turning than one that never did.
	/// </summary>
	public void Bury()
	{
		if (!IsAwake)
		{
			return;
		}

		GetParent()?.GetNodeOrNull($"{Name}Beast")?.QueueFree();

		IsAwake = false;
		Corruption = 0.5f;
		_timesFed = 0;
		FedTonight = false;
		AddToGroup("interactable");

		GD.Print($"{Name} is back in the ground. The soil remembers.");
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

	private void Awaken(bool announce = true)
	{
		if (IsAwake)
		{
			return;
		}

		IsAwake = true;
		RemoveFromGroup("interactable");
		SpawnBeast();

		if (announce)
		{
			GD.Print($"{Name} has woken up.");
		}

		Refresh();
	}

	/// <summary>
	/// The crop stays as the bookkeeping half — saves and the nightly toll read
	/// it — while the beast becomes the thing that actually moves.
	/// </summary>
	private void SpawnBeast()
	{
		var scene = GD.Load<PackedScene>(BeastScenePath);
		if (scene == null)
		{
			GD.PushWarning($"{Name}: no beast scene at {BeastScenePath}.");
			return;
		}

		Node3D beast = scene.Instantiate<Node3D>();
		beast.Name = $"{Name}Beast";

		// Same parent, so the crop's local position transfers directly.
		beast.Position = Position;
		GetParent().CallDeferred(Node.MethodName.AddChild, beast);
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

		// Once it wakes, the beast is the visible half — hide the crop.
		_mesh.Visible = !IsAwake;

		// Scale the mesh, not the body — scaling a physics body upsets Jolt.
		_baseScale = Mathf.Lerp(1.0f, 1.5f, growth) + Corruption * 0.4f;
		_mesh.Scale = Vector3.One * _baseScale;
	}

	/// <summary>Restores a crop from a save. Called before the first night.</summary>
	public void LoadState(int timesFed, float corruption, bool awake, bool fedTonight)
	{
		_timesFed = timesFed;
		Corruption = Mathf.Clamp(corruption, 0.0f, 1.0f);
		FedTonight = fedTonight;

		if (awake)
		{
			Awaken(announce: false);
		}

		Refresh();
	}

	public int TimesFed => _timesFed;
}
