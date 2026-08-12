using Godot;

/// <summary>
/// Builds a rectangular post-and-rail fence in code, with a gap for the gate.
/// Generated rather than hand-placed so the perimeter can be resized from the
/// inspector without re-authoring 60 nodes.
/// </summary>
public partial class Fence : Node3D
{
	/// <summary>Full width and depth of the enclosure, in metres.</summary>
	[Export] public Vector2 Size = new(40.0f, 40.0f);

	[Export] public float PostSpacing = 3.0f;
	[Export] public float PostHeight = 1.6f;

	/// <summary>Opening on the +Z side, centred on x = 0.</summary>
	[Export] public float GateWidth = 6.0f;

	[Export] public Color WoodColor = new(0.24f, 0.18f, 0.13f);

	private StandardMaterial3D _material;
	private BoxMesh _postMesh;

	public override void _Ready()
	{
		_material = new StandardMaterial3D { AlbedoColor = WoodColor, Roughness = 1.0f };
		_postMesh = new BoxMesh { Size = new Vector3(0.16f, PostHeight, 0.16f) };

		float halfX = Size.X * 0.5f;
		float halfZ = Size.Y * 0.5f;
		float halfGate = GateWidth * 0.5f;

		// The +Z run is split so the gate stays open.
		BuildRun(new Vector3(-halfX, 0.0f, -halfZ), new Vector3(halfX, 0.0f, -halfZ));
		BuildRun(new Vector3(halfX, 0.0f, -halfZ), new Vector3(halfX, 0.0f, halfZ));
		BuildRun(new Vector3(-halfX, 0.0f, halfZ), new Vector3(-halfX, 0.0f, -halfZ));
		BuildRun(new Vector3(-halfX, 0.0f, halfZ), new Vector3(-halfGate, 0.0f, halfZ));
		BuildRun(new Vector3(halfGate, 0.0f, halfZ), new Vector3(halfX, 0.0f, halfZ));
	}

	private void BuildRun(Vector3 from, Vector3 to)
	{
		Vector3 span = to - from;
		float length = span.Length();
		if (length < 0.01f)
		{
			return;
		}

		Vector3 direction = span / length;
		Vector3 centre = from + span * 0.5f;
		float yaw = Mathf.Atan2(direction.X, direction.Z);

		int posts = Mathf.Max(2, Mathf.RoundToInt(length / PostSpacing) + 1);
		for (int i = 0; i < posts; i++)
		{
			Vector3 at = from + direction * (length * i / (posts - 1));
			AddChild(new MeshInstance3D
			{
				Mesh = _postMesh,
				MaterialOverride = _material,
				Position = at + new Vector3(0.0f, PostHeight * 0.5f, 0.0f),
			});
		}

		// Two rails per run, at knee and shoulder of the post.
		foreach (float height in new[] { PostHeight * 0.35f, PostHeight * 0.8f })
		{
			AddChild(new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.1f, length) },
				MaterialOverride = _material,
				Position = centre + new Vector3(0.0f, height, 0.0f),
				Rotation = new Vector3(0.0f, yaw, 0.0f),
			});
		}

		// One collider for the whole run rather than one per post.
		var body = new StaticBody3D
		{
			Position = centre + new Vector3(0.0f, PostHeight * 0.5f, 0.0f),
			Rotation = new Vector3(0.0f, yaw, 0.0f),
		};
		body.AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(0.2f, PostHeight, length) },
		});
		AddChild(body);
	}
}
