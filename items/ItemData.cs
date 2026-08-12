using Godot;

/// <summary>
/// Definition of one kind of item. A Resource, so you can also author variants
/// as .tres files in the editor (right-click → New Resource → ItemData) and
/// drop them into <see cref="ItemDatabase"/>.
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public int MaxStack { get; set; } = 20;

	/// <summary>Corruption removed when this is fed to the soil. 0 = not feed.</summary>
	[Export] public float FeedValue { get; set; }

	/// <summary>What the butcher charges. Negative means he buys it from you.</summary>
	[Export] public int Price { get; set; }

	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

	public bool IsFeed => FeedValue > 0.0f;
}
