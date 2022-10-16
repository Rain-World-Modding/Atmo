﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.Utils;

using DBG = System.Diagnostics;

namespace Atmo;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen>
{
    //todo: add frame time profiling
    internal const int PROFILER_CYCLE_COREUP = 200;
    internal const int PROFILER_CYCLE_REALUP = 400;
    internal const int STORE_CYCLES = 12;
    #region fields
    #region perfrec
    internal readonly LinkedList<double> realup_readings = new();
    internal readonly List<TimeSpan> realup_times = new(PROFILER_CYCLE_REALUP);
    internal readonly LinkedList<double> haeval_readings = new();
    internal readonly List<TimeSpan> haeval_times = new(PROFILER_CYCLE_COREUP);
    #endregion perfrec
    private readonly Guid guid = Guid.NewGuid();
    internal PredicateInlay? conditions;
    public bool active { get; private set; }
    private DBG.Stopwatch sw = new();
    /// <summary>
    /// Whether the init callbacks have been invoked or not.
    /// </summary>
    public bool initRan;
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
    internal HappenSet set;
    //public readonly string[] exclude;
    #endregion fromcfg
    #endregion fields
    internal Happen(HappenConfig cfg, HappenSet owner, RainWorldGame rwg)
    {
        set = owner;
        name = cfg.name;
        actions = cfg.actions;//.Select(x => x.Key).ToArray();
        conditions = cfg.conditions;
        List<HappenTrigger> list_triggers = new();
        conditions?.Populate((id, args) =>
        {
            var nt = HappenBuilding.CreateTrigger(id, args, rwg, this);
            list_triggers.Add(nt);
            //inst.Plog.LogWarning($"running pop!!! {nt}, {nt.ShouldRunUpdates()}");
            return nt.ShouldRunUpdates;
        });
        //inst.Plog.LogWarning( conditions?.Eval());
        triggers = list_triggers.ToArray();
        HappenBuilding.NewEvent(this);

        if (actions.Count is 0) inst.Plog.LogWarning($"Happen {this}: no actions! Possible missing 'WHAT:' clause");
        if (conditions is null) inst.Plog.LogWarning($"Happen {this}: did not receive conditions! Possible missing 'WHEN:' clause");
    }
    //private readonly List<Delegate> broken = new();
    #region lifecycle cbs
    internal void AbstUpdate (AbstractRoom absroom, int time) {
        if (On_AbstUpdate is null) return;
        foreach (API.lc_AbstractUpdate cb in On_AbstUpdate.GetInvocationList())
        {
            try
            {
                cb?.Invoke(absroom, time);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.abstup, cb, ex));
                On_AbstUpdate -= cb;
            }
        }
    }
    /// <summary>
    /// Attach to this to receive a call once per abstract update, for every affected room
    /// </summary>
    public event API.lc_AbstractUpdate? On_AbstUpdate;
    internal void RealUpdate (Room room) {
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
                inst.Plog.LogError(ErrorMessage(LCE.realup, cb, ex));
                On_RealUpdate -= cb;
            }
        }
        LogFrameTime(realup_times, sw.Elapsed, realup_readings, STORE_CYCLES);
        sw.Reset();
    }

    /// <summary>
    /// Attach to this to receive a call once per realized update, for every affected room
    /// </summary>
    public event API.lc_RealizedUpdate? On_RealUpdate;
    internal void Init(World world) {
        initRan = true;
        if (On_Init is null) return;
        foreach (API.lc_Init cb in On_Init.GetInvocationList())
        {
            try
            {
                cb?.Invoke(world);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.init, cb, ex, false));
                //On_Init -= cb;
            }
        }
    }
    /// <summary>
    /// Subscribe to this to receive one call when abstract update is first ran.
    /// </summary>
    public event API.lc_Init? On_Init;
    internal void CoreUpdate(RainWorldGame rwg)
    {
        sw.Start();
        foreach (var tr in triggers) tr.Update();
        active = conditions?.Eval() ?? true;
        foreach (var t in triggers) t.EvalResults(active);
        if (On_CoreUpdate is null) return;
        foreach (API.lc_CoreUpdate cb in On_CoreUpdate.GetInvocationList())
        {
            try
            {
                cb(rwg);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(ErrorMessage(LCE.coreup, cb, ex));
                On_CoreUpdate -= cb;
            }
        }
        LogFrameTime(haeval_times, sw.Elapsed, haeval_readings, STORE_CYCLES);
        sw.Reset();
    }

    /// <summary>
    /// Subscribe to this to receive an update once per frame
    /// </summary>
    public event API.lc_CoreUpdate? On_CoreUpdate;
    #endregion

    /// <summary>
    /// Checks if happen should be running
    /// </summary>
    /// <param name="rwg">RainWorldGame instance to check</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool IsOn(RainWorldGame rwg)
        => active;
    /// <summary>
    /// Returns a performance report.
    /// </summary>
    /// <returns></returns>
    public Perf PerfRecord()
    {
        var perf = new Perf();
        perf.name = name;
        perf.samples_eval = haeval_readings.Count;
        perf.samples_realup = realup_readings.Count;
        
        double 
            realuptotal = 0d,
            evaltotal = 0d;
        if (perf.samples_realup is not 0)
        {
            foreach (double rec in realup_readings) realuptotal += rec;
            perf.avg_realup = realuptotal / (double)realup_readings.Count;
        }
        else
        {
            perf.avg_realup = float.NaN;
        }
        if (perf.samples_eval is not 0)
        {
            foreach (double rec in haeval_readings) evaltotal += rec;
            perf.avg_eval = evaltotal / (double)haeval_readings.Count;
        }
        else
        {
            perf.avg_eval= float.NaN;
        }
        return perf;
    }
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
        return $"{name}-{guid}[{(actions.Count == 0 ? string.Empty : actions.Select(x => $"{x.Key}").Aggregate(Utils.JoinWithComma))}]({triggers.Length} triggers)";
    }

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
    private enum LCE
    {
        abstup,
        realup,
        coreup,
        init,
    }
    #endregion

    private string ErrorMessage(LCE where, Delegate cb, Exception ex, bool removing = true)
        => $"Happen {this}: {where}: " +
        $"Error on callback {cb}//{cb.Method}:" +
        $"\n{ex}" + (removing ? "\nRemoving problematic callback." : string.Empty);
}
