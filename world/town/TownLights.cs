using Godot;
using System.Collections.Generic;

/// <summary>
/// The town, seen only as silhouettes on the horizon. One lit window per
/// villager. When the soil takes someone, their window goes dark and stays
/// dark — the house remains, which is the point.
/// </summary>
public partial class TownLights : Node3D
{
	[Export] public float Distance = 42.0f;
	[Export] public float ArcDegrees = 55.0f;
	[Export] public Color WindowColor = new(1.0f, 0.78f, 0.42f);
	[Export] public float WindowEnergy = 6.0f;

	private readonly List<OmniLight3D> _windows = new();
	private readonly List<MeshInstance3D> _walls = new();

	private StandardMaterial3D _litMaterial;
	private StandardMaterial3D _darkMaterial;

	private Village _village;

	public override void _Ready()
	{
		_village = GetNode<Village>("/root/Village");
		_village.VillagerConsumed += OnVillagerConsumed;

		_litMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.13f, 0.12f, 0.14f), Roughness = 1.0f };
		_darkMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.06f), Roughness = 1.0f };

		Build(_village.Population);
	}

	private void Build(int count)
	{
		float arc = Mathf.DegToRad(ArcDegrees);

		for (int i = 0; i < count; i++)
		{
			// Index-based variation instead of RNG, so the skyline is the same
			// every run — you learn its shape, then watch it go out.
			float t = count > 1 ? (float)i / (count - 1) : 0.5f;
			float angle = Mathf.Lerp(-arc, arc, t);
			float depth = Distance + Mathf.Sin(i * 2.4f) * 6.0f;
			float height = 3.0f + Mathf.Abs(Mathf.Sin(i * 1.7f)) * 4.0f;
			float width = 2.5f + Mathf.Abs(Mathf.Cos(i * 1.1f)) * 2.0f;

			var position = new Vector3(Mathf.Sin(angle) * depth, height * 0.5f, -Mathf.Cos(angle) * depth);

			var house = new MeshInstance3D
			{
				Name = $"House{i}",
				Mesh = new BoxMesh { Size = new Vector3(width, height, width) },
				MaterialOverride = _litMaterial,
				Position = position,
			};
			AddChild(house);
			_walls.Add(house);

			var window = new OmniLight3D
			{
				Name = $"Window{i}",
				LightColor = WindowColor,
				LightEnergy = WindowEnergy,
				OmniRange = 14.0f,
				Position = position + new Vector3(0.0f, height * 0.2f, width * 0.6f),
			};
			AddChild(window);
			_windows.Add(window);
		}
	}

	private void OnVillagerConsumed(string villagerName, int remaining)
	{
		// Darken from the far end inward, so the town retreats toward you.
		for (int i = remaining; i < _windows.Count; i++)
		{
			_windows[i].Visible = false;
			_walls[i].MaterialOverride = _darkMaterial;
		}
	}
}
