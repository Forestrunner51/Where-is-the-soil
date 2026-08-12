using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>One stack of items in a slot.</summary>
public class ItemStack
{
	public ItemData Item;
	public int Count;
}

/// <summary>
/// Slot-based inventory with stacking. Attach as a child of whoever carries
/// things — the player now, the butcher's stall later.
/// </summary>
public partial class Inventory : Node
{
	[Signal] public delegate void ChangedEventHandler();

	[Export] public int SlotCount = 12;

	private readonly List<ItemStack> _stacks = new();

	public IReadOnlyList<ItemStack> Stacks => _stacks;

	public int CountOf(string itemId) =>
		_stacks.Where(s => s.Item.Id == itemId).Sum(s => s.Count);

	public bool Has(string itemId, int amount = 1) => CountOf(itemId) >= amount;

	/// <summary>
	/// Adds items, filling partial stacks first. Returns how many did NOT fit.
	/// </summary>
	public int Add(ItemData item, int amount = 1)
	{
		if (item == null || amount <= 0)
		{
			return amount;
		}

		int remaining = amount;

		foreach (ItemStack stack in _stacks.Where(s => s.Item.Id == item.Id && s.Count < s.Item.MaxStack))
		{
			int space = stack.Item.MaxStack - stack.Count;
			int moved = Mathf.Min(space, remaining);
			stack.Count += moved;
			remaining -= moved;

			if (remaining == 0)
			{
				break;
			}
		}

		while (remaining > 0 && _stacks.Count < SlotCount)
		{
			int moved = Mathf.Min(item.MaxStack, remaining);
			_stacks.Add(new ItemStack { Item = item, Count = moved });
			remaining -= moved;
		}

		if (remaining != amount)
		{
			EmitSignal(SignalName.Changed);
		}

		return remaining;
	}

	/// <summary>
	/// Removes items only if the full amount is present. All-or-nothing, so a
	/// failed feed never eats half a stack.
	/// </summary>
	public bool TryConsume(string itemId, int amount = 1)
	{
		if (!Has(itemId, amount))
		{
			return false;
		}

		int remaining = amount;

		for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
		{
			if (_stacks[i].Item.Id != itemId)
			{
				continue;
			}

			int taken = Mathf.Min(_stacks[i].Count, remaining);
			_stacks[i].Count -= taken;
			remaining -= taken;

			if (_stacks[i].Count == 0)
			{
				_stacks.RemoveAt(i);
			}
		}

		EmitSignal(SignalName.Changed);
		return true;
	}

	/// <summary>
	/// Best available feed — the strongest one you're carrying. Null if none.
	/// </summary>
	public ItemData BestFeed() =>
		_stacks.Where(s => s.Item.IsFeed)
			.OrderByDescending(s => s.Item.FeedValue)
			.Select(s => s.Item)
			.FirstOrDefault();

	public string Summary()
	{
		if (_stacks.Count == 0)
		{
			return "(empty)";
		}

		return string.Join("\n", _stacks.Select(s => $"{s.Item.DisplayName} x{s.Count}"));
	}
}
