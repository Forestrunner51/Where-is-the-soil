/// <summary>
/// Anything the player can trigger with the interact key. Implementors must be
/// a Node3D (the player measures distance from their global position) and add
/// themselves to the "interactable" group so the player can find them.
/// </summary>
public interface IInteractable
{
	/// <summary>Short text describing what interacting would do right now.</summary>
	string InteractPrompt { get; }

	/// <summary>False when there is nothing useful to do — e.g. already awake.</summary>
	bool CanInteract { get; }

	/// <summary>
	/// The player is passed in so interactables can charge them — take feed out
	/// of their inventory, hand them a harvest, take their money.
	/// </summary>
	void Interact(Player player);
}
