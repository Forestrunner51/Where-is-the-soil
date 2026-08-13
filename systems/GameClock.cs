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

	/// <summary>Short on purpose — chores shouldn't be padded with walking.</summary>
	[Export] public float DayLength = 50.0f;

	/// <summary>Long enough that staying out is a real stretch of time to survive.</summary>
	[Export] public float NightLength = 40.0f;

	public int Day { get; private set; } = 1;
	public bool IsNight { get; private set; }

	/// <summary>0 at the start of the current phase, 1 at its end.</summary>
	public float PhaseProgress { get; private set; }

	/// <summary>False in menus, so days don't tick by while nobody is farming.</summary>
	public bool Running { get; set; }

	private float _elapsed;

	/// <summary>
	/// Skips straight through the night to dawn. The night still resolves in
	/// full — you just don't get to watch it happen.
	/// </summary>
	public void SleepThroughNight()
	{
		if (!IsNight)
		{
			IsNight = true;
			GD.Print($"--- Night falls on day {Day} ---");
			EmitSignal(SignalName.NightFell, Day);
		}

		IsNight = false;
		Day++;
		_elapsed = 0.0f;
		PhaseProgress = 0.0f;

		GD.Print($"--- Day {Day} ---");
		EmitSignal(SignalName.DayBroke, Day);
	}

	/// <summary>Restores a saved day, at morning.</summary>
	public void SetDay(int day)
	{
		Day = Mathf.Max(1, day);
		IsNight = false;
		_elapsed = 0.0f;
		PhaseProgress = 0.0f;
	}

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
		if (!Running)
		{
			return;
		}

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
