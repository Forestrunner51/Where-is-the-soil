using Godot;

/// <summary>
/// Autoloaded singleton that drives the day/night cycle. Everything that cares
/// about time subscribes to <see cref="NightFell"/> / <see cref="DayBroke"/>
/// rather than counting seconds itself.
/// </summary>
public partial class GameClock : Node
{
	/// <summary>Emitted the moment darkness falls, before anything resolves.</summary>
	[Signal] public delegate void NightFellEventHandler(int day);

	/// <summary>Emitted at sunrise, once the night's consequences have landed.</summary>
	[Signal] public delegate void DayBrokeEventHandler(int day);

	[Export] public float DayLength = 120.0f;
	[Export] public float NightLength = 45.0f;

	public int Day { get; private set; } = 1;
	public bool IsNight { get; private set; }

	/// <summary>0 at the start of the current phase, 1 at its end.</summary>
	public float PhaseProgress { get; private set; }

	private float _elapsed;

	/// <summary>Back to the morning of day one. Called when a new run starts.</summary>
	public void Reset()
	{
		Day = 1;
		IsNight = false;
		PhaseProgress = 0.0f;
		_elapsed = 0.0f;
	}

	public override void _Process(double delta)
	{
		float length = IsNight ? NightLength : DayLength;
		_elapsed += (float)delta;
		PhaseProgress = Mathf.Clamp(_elapsed / length, 0.0f, 1.0f);

		if (_elapsed < length)
		{
			return;
		}

		_elapsed = 0.0f;

		if (IsNight)
		{
			IsNight = false;
			Day++;
			GD.Print($"--- Day {Day} ---");
			EmitSignal(SignalName.DayBroke, Day);
		}
		else
		{
			IsNight = true;
			GD.Print($"--- Night falls on day {Day} ---");
			// Crops resolve on this signal. Whatever wasn't fed, feeds itself.
			EmitSignal(SignalName.NightFell, Day);
		}
	}
}
