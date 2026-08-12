using Godot;

/// <summary>
/// The stall at the edge of the farm. Opens the shop — and closes for good once
/// there is nobody left in town to run it.
/// </summary>
public partial class Butcher : StaticBody3D, IInteractable
{
	private Village _village;

	/// <summary>Aldis runs the stall; Meru can take it over. Lose both and it never opens again.</summary>
	private bool HasShopkeeper =>
		_village != null && (_village.IsAlive(Village.Butcher) || _village.IsAlive(Village.ButcherHeir));

	public bool CanInteract => HasShopkeeper;

	public string InteractPrompt
	{
		get
		{
			if (HasShopkeeper)
			{
				return _village.IsAlive(Village.Butcher) ? "Aldis, the butcher" : "Meru, minding her father's stall";
			}

			return "The stall is shuttered. Nobody is coming.";
		}
	}

	public override void _Ready()
	{
		AddToGroup("interactable");
		_village = GetNode<Village>("/root/Village");
	}

	public void Interact(Player player)
	{
		if (!CanInteract)
		{
			return;
		}

		if (GetTree().GetFirstNodeInGroup("shop") is ShopUi shop)
		{
			shop.Open(player);
		}
		else
		{
			GD.PushWarning("No ShopUi in the 'shop' group — is hud/shop.tscn in the scene?");
		}
	}
}
