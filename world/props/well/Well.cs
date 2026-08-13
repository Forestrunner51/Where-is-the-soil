using Godot;

/// <summary>
/// The only way back from a woken plot. The well takes what the field made —
/// pay it in dark potatoes and it puts one beast back in the ground.
///
/// The loop is deliberately closed: dark potatoes only come from plots you let
/// rot, so the cure is made of the disease.
/// </summary>
public partial class Well : StaticBody3D, IInteractable
{
	[Export] public int DarkPotatoCost = 3;

	private Player _player;

	public bool CanInteract => FindNearestBeastCrop() != null;

	public string InteractPrompt
	{
		get
		{
			if (FindNearestBeastCrop() == null)
			{
				return "The well is quiet.";
			}

			int held = _player?.Inventory.CountOf(ItemDatabase.CorruptPotato.Id) ?? 0;
			return held >= DarkPotatoCost
				? $"Give the well {DarkPotatoCost} dark potatoes"
				: $"The well wants {DarkPotatoCost} dark potatoes ({held})";
		}
	}

	public override void _Ready() => AddToGroup("interactable");

	public void Interact(Player player)
	{
		_player = player;

		Potato target = FindNearestBeastCrop();
		if (target == null)
		{
			return;
		}

		if (!player.Inventory.TryConsume(ItemDatabase.CorruptPotato.Id, DarkPotatoCost))
		{
			GD.Print($"The well wants {DarkPotatoCost} dark potatoes. It does not take coin.");
			return;
		}

		GD.Print("You tip them in. Something below accepts the trade.");
		target.Bury();
	}

	/// <summary>
	/// The woken plot closest to the well. Deterministic, so the player can
	/// predict which one they are buying back.
	/// </summary>
	private Potato FindNearestBeastCrop()
	{
		Potato nearest = null;
		float best = float.MaxValue;

		foreach (Node node in GetTree().GetNodesInGroup("crop"))
		{
			if (node is not Potato { IsAwake: true } crop)
			{
				continue;
			}

			float distance = GlobalPosition.DistanceSquaredTo(crop.GlobalPosition);
			if (distance < best)
			{
				best = distance;
				nearest = crop;
			}
		}

		return nearest;
	}

	public override void _Process(double delta)
	{
		// Keep a player reference so the prompt can show what you're carrying.
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
	}
}
