using Godot;
using System.Collections.Generic;

/// <summary>
/// Settles the night for the whole farm at once. Crops used to each resolve
/// themselves, which meant six neglected plots cost six villagers in one night.
/// The town pays a single price per night, however much you left unfed.
/// </summary>
public partial class NightResolver : Node
{
	/// <summary>Corruption multiplier once there is no priest left to bless the field.</summary>
	[Export] public float UnblessedMultiplier = 1.5f;

	private GameClock _clock;
	private Village _village;

	public override void _Ready()
	{
		_clock = GetNode<GameClock>("/root/GameClock");
		_village = GetNode<Village>("/root/Village");

		_clock.NightFell += OnNightFell;
		_clock.DayBroke += OnDayBroke;
	}

	private void OnNightFell(int day)
	{
		List<Potato> neglected = new();

		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			if (node is Potato crop && crop.NeedsFeeding)
			{
				neglected.Add(crop);
			}
		}

		if (neglected.Count == 0)
		{
			return;
		}

		// Without the priest, the field turns faster.
		float multiplier = _village.IsAlive(Village.Priest) ? 1.0f : UnblessedMultiplier;

		foreach (Potato crop in neglected)
		{
			crop.ResolveNight(multiplier);
		}

		// One price, however many plots went hungry.
		if (_village.ConsumeVillager() != null)
		{
			return;
		}

		// Nothing left in town to take. Everything hungry wakes up instead.
		GD.Print("There was nobody left to take. The field settles it another way.");
		foreach (Potato crop in neglected)
		{
			crop.Starve();
		}
	}

	private void OnDayBroke(int day)
	{
		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			(node as Potato)?.BeginDay();
		}
	}
}
