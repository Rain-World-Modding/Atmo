﻿
namespace Atmo;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks. Every happen is read from within a <c>HAPPEN:...END HAPPEN</c> block in an .ATMO file. The following example block:
/// <para>
/// <code>
/// HAPPEN: test
/// WHAT: palette 15
/// WHERE: first + SU_A41 SU_A42 - SU_A22
/// WHERE: SU_C04
/// WHEN: karma 1 2 10
/// END HAPPEN
/// </code>
/// Will result in a happen that has the following properties:
/// <list type="table">
/// <listheader><term>Property</term> <description>Contents and meaning</description></listheader>
/// <item>
///		<term><see cref="name"/></term> 
///		<description>
///			Unique string that identifies an instance. 
///			This happen will be called <c>test</c>.
///		</description>
///	</item>
/// <item>
///		<term>Behaviour</term> 
///		<description>
///			The instance's lifetime events are populated with callbacks taken from <see cref="API.EV_MakeNewHappen"/>.
///			For registering your behaviours, see <seealso cref="API"/>.
///			To see examples of how some of the builtin behaviours work, see <seealso cref="HappenBuilding.InitBuiltins"/>.
///			This happen will change main palette of affected rooms to 15.
///		</description>
///	</item>
/// <item>
///		<term>Grouping</term> 
///		<description>
///			The set of rooms an instance is active in.
///			This Happen will activate in group called <c>first</c>, and additionally in rooms <c>SU_A41</c> and <c>SU_A42</c>, 
///			but will not be activated in <c>SU_A22</c> if <c>SU_A22</c> is present in the group. 
///			See <seealso cref="HappenSet"/> to see how Happens and Rooms are grouped together.
///		</description>
///	</item>
/// <item>
///		<term><see cref="conditions"/></term> 
///		<description>
///			A <seealso cref="HappenTrigger"/> created from the WHEN expression,
///			which determines when the Happen should be active or not. 
///			This happen will be active when player's karma level is 1, 2 or 10. 
///			See code of <seealso cref="PredicateInlay"/> if you want to know how the expression is parsed.
///		</description>
///	</item>
/// </list>
/// </para>
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen>
{
	internal const int PROFILER_CYCLE_COREUP = 200;
	internal const int PROFILER_CYCLE_REALUP = 400;
	internal const int STORE_CYCLES = 12;
	#region fields/props
	#region perfrec
	internal readonly LinkedList<double> realup_readings = new();
	internal readonly List<TimeSpan> realup_times = new(PROFILER_CYCLE_REALUP);
	internal readonly LinkedList<double> haeval_readings = new();
	internal readonly List<TimeSpan> haeval_times = new(PROFILER_CYCLE_COREUP);
	#endregion perfrec
	/// <summary>
	/// Displays whether a happen is active during the current frame. Updated on <see cref="Atmod.DoBodyUpdates(On.RainWorldGame.orig_Update, RainWorldGame)"/>.
	/// </summary>
	public bool Active { get; private set; }
	/// <summary>
	/// Whether the init callbacks have been invoked or not.
	/// </summary>
	public bool InitRan { get; internal set; }
	/// <summary>
	/// HappenSet this Happen is associated with.
	/// Ownership may change when merging atmo files from different regpacks.
	/// </summary>
	public HappenSet set { get; internal set; }
	/// <summary>
	/// Used internally for sorting.
	/// </summary>
	private readonly Guid guid = Guid.NewGuid();
	/// <summary>
	/// Activation expression. Populated by <see cref="HappenTrigger.ShouldRunUpdates"/> callbacks of items in <see cref="triggers"/>.
	/// </summary>
	internal PredicateInlay? conditions;
	/// <summary>
	/// Used for frame time profiling.
	/// </summary>
	private readonly DBG.Stopwatch sw = new();
	#region fromcfg
	/// <summary>
	/// All triggers associated with the happen.
	/// </summary>
	public readonly HappenTrigger[] triggers;
	/// <summary>
	/// name of the happen.
	/// </summary>
	public readonly string name;
	/// <summary>
	/// A set of actions with their parameters.
	/// </summary>
	public readonly Dictionary<string, string[]> actions;
	/// <summary>
	/// Current game instance.
	/// </summary>
	public readonly RainWorldGame game;

	#endregion fromcfg
	#endregion fields/props
	/// <summary>
	/// Creates a new instance from given config, set and game reference.
	/// </summary>
	/// <param name="cfg">A config containing basic setup info.
	/// Make sure it is properly instantiated, and none of the fields are unexpectedly null.</param>
	/// <param name="owner">HappenSet this happen will belong to. Must not be null.</param>
	/// <param name="game">Current game instance. Must not be null.</param>
	public Happen(HappenConfig cfg, HappenSet owner!!, RainWorldGame game!!)
	{
		set = owner;
		name = cfg.name;
		this.game = game;
		actions = cfg.actions;
		conditions = cfg.conditions;
		List<HappenTrigger> list_triggers = new();
		conditions?.Populate((id, args) =>
		{
			HappenTrigger? nt = HappenBuilding.CreateTrigger(id, args, game, this);
			list_triggers.Add(nt);
			return nt.ShouldRunUpdates;
		});
		triggers = list_triggers.ToArray();
		HappenBuilding.NewEvent(this);

		if (actions.Count is 0) inst.Plog.LogWarning($"Happen {this}: no actions! Possible missing 'WHAT:' clause");
		if (conditions is null) inst.Plog.LogWarning($"Happen {this}: did not receive conditions! Possible missing 'WHEN:' clause");
	}
	#region lifecycle cbs
	internal void AbstUpdate(AbstractRoom absroom, int time)
	{
		if (On_AbstUpdate is null) return;
		foreach (API.lc_AbstractUpdate cb in On_AbstUpdate.GetInvocationList())
		{
			try
			{
				cb?.Invoke(absroom, time);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(ErrorMessage(lc_event.abstup, cb, ex));
				On_AbstUpdate -= cb;
			}
		}
	}
	/// <summary>
	/// Attach to this to receive a call once per abstract update, for every affected room.
	/// </summary>
	public event API.lc_AbstractUpdate? On_AbstUpdate;
	internal void RealUpdate(Room room)
	{
		sw.Start();
		if (On_RealUpdate is null) return;
		foreach (API.lc_RealizedUpdate cb in On_RealUpdate.GetInvocationList())
		{
			try
			{
				cb?.Invoke(room);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(ErrorMessage(lc_event.realup, cb, ex));
				On_RealUpdate -= cb;
			}
		}
		LogFrameTime(realup_times, sw.Elapsed, realup_readings, STORE_CYCLES);
		sw.Reset();
	}
	/// <summary>
	/// Attach to this to receive a call once per realized update, for every affected room.
	/// </summary>
	public event API.lc_RealizedUpdate? On_RealUpdate;
	internal void Init(World world)
	{
		InitRan = true;
		if (On_Init is null) return;
		foreach (API.lc_Init cb in On_Init.GetInvocationList())
		{
			try
			{
				cb?.Invoke(world);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(ErrorMessage(lc_event.init, cb, ex, error_response.none));
			}
		}
	}
	/// <summary>
	/// Subscribe to this to receive one call before abstract or realized update is first ran.
	/// </summary>
	public event API.lc_Init? On_Init;
	internal void CoreUpdate()
	{
		sw.Start();
		for (var tin = 0; tin < triggers.Length; tin++)
		{
			try
			{
				triggers[tin].Update();
			}
			catch (Exception ex)
			{
				//todo: add a way to void a trigger
				inst.Plog.LogError(ErrorMessage(
					lc_event.triggerupdate,
					triggers[tin].Update,
					ex,
					error_response.none));
			}
		}
		try
		{
			Active = conditions?.Eval() ?? true;
			foreach (HappenTrigger t in triggers) t.EvalResults(Active);
		}
		catch (Exception ex)
		{
			inst.Plog.LogError(ErrorMessage(
				lc_event.eval,
				conditions is null ? null : conditions.Eval,
				ex,
				error_response.none));
		}
		if (On_CoreUpdate is null) return;
		foreach (API.lc_CoreUpdate cb in On_CoreUpdate.GetInvocationList())
		{
			try
			{
				cb(game);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(ErrorMessage(lc_event.coreup, cb, ex));
				On_CoreUpdate -= cb;
			}
		}
		LogFrameTime(haeval_times, sw.Elapsed, haeval_readings, STORE_CYCLES);
		sw.Reset();
	}
	/// <summary>
	/// Subscribe to this to receive an update once per frame.
	/// </summary>
	public event API.lc_CoreUpdate? On_CoreUpdate;
	#endregion
	/// <summary>
	/// Returns a performance report struct.
	/// </summary>
	/// <returns></returns>
	public Perf PerfRecord()
	{
		var perf = new Perf
		{
			name = name,
			samples_eval = haeval_readings.Count,
			samples_realup = realup_readings.Count
		};

		double
			realuptotal = 0d,
			evaltotal = 0d;
		if (perf.samples_realup is not 0)
		{
			foreach (var rec in realup_readings) realuptotal += rec;
			perf.avg_realup = realuptotal / (double)realup_readings.Count;
		}
		else
		{
			perf.avg_realup = float.NaN;
		}
		if (perf.samples_eval is not 0)
		{
			foreach (var rec in haeval_readings) evaltotal += rec;
			perf.avg_eval = evaltotal / (double)haeval_readings.Count;
		}
		else
		{
			perf.avg_eval = float.NaN;
		}
		return perf;
	}
	#region general
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public int CompareTo(Happen other)
	{
		return guid.CompareTo(other.guid);
	}
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(Happen other)
	{
		return guid.Equals(other.guid);
	}
	/// <summary>
	/// Returns a string representation of the happen.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return $"{name}" +
			$"[{(actions.Count == 0
				? string.Empty
				: actions.Select(x => $"{x.Key}").Aggregate(Utils.JoinWithComma))}]" +
			$"({triggers.Length} triggers)";
	}
	#endregion
	#region nested
	/// <summary>
	/// Carries performance report from the happen.
	/// </summary>
	public struct Perf
	{
		/// <summary>
		/// Happen name
		/// </summary>
		public string name;
		/// <summary>
		/// Average real update frame time
		/// </summary>
		public double avg_realup;
		/// <summary>
		/// Number of recorded real update frame time samples
		/// </summary>
		public int samples_realup;
		/// <summary>
		/// Average eval invocation time
		/// </summary>
		public double avg_eval;
		/// <summary>
		/// Number of recorded eval frame time samples
		/// </summary>
		public int samples_eval;
	}
	private enum lc_event
	{
		abstup,
		realup,
		coreup,
		init,
		eval,
		triggerupdate,
	}
	private enum error_response
	{
		none,
		remove_cb,
		void_trigger
	}
	#endregion
	private string ErrorMessage(lc_event where, Delegate? cb, Exception ex, error_response resp = error_response.remove_cb)
	{
		return $"Happen {this}: {where}: " +
			$"Error on invoke {cb}//{cb?.Method}:" +
			$"\n{ex}" +
			$"\nAction taken: " + resp switch
			{
				error_response.none => "none.",
				error_response.remove_cb => "removing problematic callback.",
				error_response.void_trigger => "voiding trigger.",
				_ => "???",
			};
	}
}
