using Godot;

/// <summary>
/// Skips the rest of the day. The night still resolves in full — you simply
/// aren't awake to see which part of it came for the town.
/// </summary>
public partial class Bed : StaticBody3D, IInteractable
{
	private GameClock _clock;

	public bool CanInteract => true;

	public string InteractPrompt => "Sleep until morning";

	public override void _Ready()
	{
		AddToGroup("interactable");

		// Where the player wakes up if something catches them in the dark.
		AddToGroup("bed");

		_clock = GetNode<GameClock>("/root/GameClock");
	}

	public void Interact(Player player)
	{
		GD.Print("You lie down and close your eyes.");
		_clock.SleepThroughNight();
	}
}
