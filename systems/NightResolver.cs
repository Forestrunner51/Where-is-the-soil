using Godot;
using System.Collections.Generic;

/// <summary>
/// Settles the night for the whole farm at once, then decides whether the run
/// is over. Crops used to each resolve themselves, which meant six neglected
/// plots cost six villagers in one night.
/// </summary>
public partial class NightResolver : Node
{
	/// <summary>Corruption multiplier once there is no priest left to bless the field.</summary>
	[Export] public float UnblessedMultiplier = 1.5f;

	/// <summary>How many awake beasts share one villager between them.</summary>
	[Export] public int VillagersPerBeast = 2;

	/// <summary>Corruption an awake beast forces on a living crop each night.</summary>
	[Export] public float BeastTending = 0.15f;

	/// <summary>Survive this many days with the town alive and you are done.</summary>
	[Export] public int SurviveDays = 20;

	private GameClock _clock;
	private Village _village;
	private GameSession _session;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");
		_village = GetNode<Village>("/root/Village");
		_session = GetNode<GameSession>("/root/GameSession");

		_clock.NightFell += OnNightFell;
		_clock.DayBroke += OnDayBroke;
	}

	private void OnNightFell(int day)
	{
		List<Potato> crops = new();
		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			if (node is Potato crop)
			{
				crops.Add(crop);
			}
		}

		if (crops.Count == 0)
		{
			// No field loaded — we are sitting in a menu.
			return;
		}

		List<Potato> neglected = crops.FindAll(c => c.NeedsFeeding);
		List<Potato> beasts = crops.FindAll(c => c.IsAwake);

		// Without the priest, the field turns faster.
		float multiplier = _village.IsAlive(Village.Priest) ? 1.0f : UnblessedMultiplier;

		foreach (Potato crop in neglected)
		{
			crop.ResolveNight(multiplier);
		}

		// The beasts work the field while you sleep. They are farming it back.
		TendByBeasts(beasts, crops);

		int toll = neglected.Count > 0 ? 1 : 0;
		toll += Mathf.CeilToInt((float)beasts.Count / VillagersPerBeast);

		for (int i = 0; i < toll; i++)
		{
			if (_village.ConsumeVillager() != null)
			{
				continue;
			}

			// Nothing left in town to take. Everything hungry wakes up instead.
			GD.Print("There was nobody left to take. The field settles it another way.");
			foreach (Potato crop in neglected)
			{
				crop.Starve();
			}

			_session.EndGame(GameSession.Ending.Consumed);
			return;
		}

		if (_village.IsEmpty)
		{
			_session.EndGame(GameSession.Ending.Consumed);
		}
	}

	/// <summary>Each beast forces corruption on one crop that is still a crop.</summary>
	private void TendByBeasts(List<Potato> beasts, List<Potato> crops)
	{
		if (beasts.Count == 0)
		{
			return;
		}

		List<Potato> living = crops.FindAll(c => !c.IsAwake);
		if (living.Count == 0)
		{
			return;
		}

		foreach (Potato unused in beasts)
		{
			Potato target = living[(int)(GD.Randi() % (uint)living.Count)];
			target.Tend(BeastTending);
		}

		GD.Print($"Something moved between the rows in the night. ({beasts.Count} awake)");
	}

	private void OnDayBroke(int day)
	{
		bool hasField = false;

		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			if (node is Potato crop)
			{
				crop.BeginDay();
				hasField = true;
			}
		}

		if (!hasField)
		{
			return;
		}

		if (day > SurviveDays)
		{
			_session.EndGame(GameSession.Ending.Survived);
			return;
		}

		GetNode<SaveGame>("/root/SaveGame").Save(GetTree());
	}
}
