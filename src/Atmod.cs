﻿using BepInEx;

namespace Atmo;

/// <summary>
/// Main plugin class.
/// </summary>
[BepInPlugin("thalber.atmod", "Atmo", "0.4")]
public sealed partial class Atmod : BaseUnityPlugin
{
	#region fields
	/// <summary>
	/// Static singleton
	/// </summary>
	public static Atmod inst;
	/// <summary>
	/// omg rain world reference
	/// </summary>
	public RainWorld? rw;
	/// <summary>
	/// publicized logger
	/// </summary>
	internal BepInEx.Logging.ManualLogSource Plog => Logger;
	private bool setupRan = false;
	/// <summary>
	/// Currently active <see cref="HappenSet"/>. Null if not in session, if in arena session, or if failed to read from session.
	/// </summary>
	public HappenSet? CurrentSet { get; private set; }
	#endregion
	/// <summary>
	/// Applies hooks and sets <see cref="inst"/>.
	/// </summary>
	public void OnEnable()
	{
		try
		{
			string x = "eee";
			Func<string, object> c;
			object a = x switch
			{
				"eee" => (c = (x) => new() ).Invoke("eee"),
			};

			On.AbstractRoom.Update += RunHappensAbstUpd;
			On.RainWorldGame.Update += DoBodyUpdates;
			On.Room.Update += RunHappensRealUpd;
			On.World.LoadWorld += FetchHappenSet;
			HappenBuilding.InitBuiltins();
		}
		catch (Exception ex)
		{
			Logger.LogFatal($"Error on enable!\n{ex}");
		}
		finally
		{
			inst = this;
		}
	}
	#region lifecycle
	private void FetchHappenSet(On.World.orig_LoadWorld orig, World self, int slugcatNumber, List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
	{
		orig(self, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);
		if (self.singleRoomWorld) return;
		try
		{
			CurrentSet = HappenSet.TryCreate(self);
		}
		catch (Exception e)
		{
			Logger.LogError($"Could not create a happenset: {e}");
		}
	}
	/// <summary>
	/// Sends an Update call to all events for loaded world
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private void DoBodyUpdates(On.RainWorldGame.orig_Update orig, RainWorldGame self)
	{
		orig(self);
		if (CurrentSet is null) return;
		if (self.pauseMenu != null) return;
		foreach (var ha in CurrentSet.AllHappens)
		{
			if (ha is null) continue;
			try
			{
				ha.CoreUpdate();
			}
			catch (Exception e)
			{
				Logger.LogError($"Error doing body update for {ha.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs abstract world update for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="timePassed"></param>
	private void RunHappensAbstUpd(On.AbstractRoom.orig_Update orig, AbstractRoom self, int timePassed)
	{
		orig(self, timePassed);
		if (CurrentSet is null) return;
		var haps = CurrentSet.GetHappensForRoom(self.name);
		foreach (var ha in haps)
		{
			if (ha is null) continue;
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.AbstUpdate(self, timePassed);
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"Error running event abstupdate for room {self.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs realized updates for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private void RunHappensRealUpd(On.Room.orig_Update orig, Room self)
	{
		//#warning issue: for some reason geteventsforroom always returns none on real update
		//in my infinite wisdom i set SU_S04 as test room instead of SU_C04. everything worked as intended except for my brain

		orig(self);
		//DBG.Stopwatch sw = DBG.Stopwatch.StartNew();
		if (CurrentSet is null) return;
		var haps = CurrentSet.GetHappensForRoom(self.abstractRoom.name);
		foreach (var ha in haps)
		{
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.RealUpdate(self);
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
			}
		}
	}
	//todo: make sure everything works with region switching
	#endregion lifecycle
	/// <summary>
	/// Cleans up set if not ingame.
	/// </summary>
	public void Update()
	{
		rw ??= FindObjectOfType<RainWorld>();
		if (!setupRan && rw is not null)
		{
			//maybe put something here
			setupRan = true;
		}
		if (rw is null || CurrentSet is null) return;
		if (rw.processManager.currentMainLoop is RainWorldGame) return;
		foreach (var proc in rw.processManager.sideProcesses) if (proc is RainWorldGame) return;
		Logger.LogDebug("No RainWorldGame in processmanager, erasing currentset");
		CurrentSet = null;
	}
	/// <summary>
	/// Undoes hooks and spins up a static cleanup member cleanup procedure.
	/// </summary>
	public void OnDisable()
	{
		try
		{
			//On.World.ctor -= FetchHappenSet;
			On.Room.Update -= RunHappensRealUpd;
			On.RainWorldGame.Update -= DoBodyUpdates;
			On.AbstractRoom.Update -= RunHappensAbstUpd;
			On.World.LoadWorld -= FetchHappenSet;
			BepInEx.Logging.ManualLogSource? cleanup_logger = 
				BepInEx.Logging.Logger.CreateLogSource("Atmo_Purge");
			System.Diagnostics.Stopwatch sw = new();
			sw.Start();
			cleanup_logger.LogMessage("Spooling cleanup thread.");
			System.Threading.ThreadPool.QueueUserWorkItem((_) =>
			{
				foreach (var t in typeof(Atmod).Assembly.GetTypes())
				{
					try { t.CleanupStatic(); }
					catch (Exception ex)
					{
						cleanup_logger.LogError($"{t}: Error cleaning up static fields:" +
							$"\n{ex}");
					}
				}
				sw.Stop();
				cleanup_logger.LogMessage($"Finished statics cleanup: {sw.Elapsed}");
			});
		}
		catch (Exception ex)
		{
			Logger.LogFatal($"Error on disable!\n{ex}");
		}
		finally
		{
			inst = null;
		}
	}
}
