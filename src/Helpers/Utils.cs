﻿using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.Mathf;
namespace Atmo.Helpers;
/// <summary>
/// contains general purpose utility methods
/// </summary>
public static class Utils
{
    #region fields
    /// <summary>
    /// Strings that evaluate to bool.true
    /// </summary>
    public static readonly string[] trueStrings = new[] { "true", "1", "yes", };
    /// <summary>
    /// Strings that evaluate to bool.false
    /// </summary>
    public static readonly string[] falseStrings = new[] { "false", "0", "no", };
    #endregion
    #region collections
    /// <summary>
    /// Attempts getting an item at specified index; if none found, uses default value.
    /// </summary>
    /// <typeparam name="T">List item type</typeparam>
    /// <param name="arr">List in question</param>
    /// <param name="index">Target index</param>
    /// <param name="def">Default value</param>
    /// <returns></returns>
    public static T AtOr<T>(this IList<T> arr!!, int index, T def)
    {
        if (index >= arr.Count || index < 0) return def;
        return arr[index];
    }
    public static char? Get(this StringBuilder sb!!, int index)
    {
        if (index >= sb.Length || index < 0) return null;
        return sb[index];
    }
    /// <summary>
    /// Tries getting a dictionary item by specified key; if none found, adds one using specified callback and returns the new item.
    /// </summary>
    /// <typeparam name="Tkey">Dictionary keys</typeparam>
    /// <typeparam name="Tval">Dictionary values</typeparam>
    /// <param name="dict">Dictionary to get items from</param>
    /// <param name="key">Key to look up</param>
    /// <param name="defval">Default value callback. Executed if item is not found; its return is added to the dictionary, then returned from the extension method.</param>
    /// <returns>Resulting item.</returns>
    public static Tval AddIfNone_Get<Tkey, Tval>(
        this IDictionary<Tkey, Tval> dict!!,
        Tkey key!!,
        Func<Tval> defval)
    {
        if (dict.TryGetValue(key, out Tval oldVal)) { return oldVal; }
        else
        {
            Tval def = defval();
            dict.Add(key, def);
            return def;
        }
    }
    /// <summary>
    /// Shifts contents of a BitArray one position to the right.
    /// </summary>
    /// <param name="arr">Array in question</param>
    public static void RightShift(this System.Collections.BitArray arr)
    {
        for (var i = arr.Count - 2; i >= 0; i--)
        {
            arr[i + 1] = arr[i];//arr.Set(i + 1, arr.Get(i));//[i + 1] = arr[i];
        }
        arr[0] = false;
    }
    /// <summary>
    /// For a specified key, checks if a value is present. If yes, updates the value, otherwise adds the value.
    /// </summary>
    /// <typeparam name="Tk">Keys type</typeparam>
    /// <typeparam name="Tv">Values type</typeparam>
    /// <param name="dict">Dictionary in question</param>
    /// <param name="key">Key to look up</param>
    /// <param name="val">Value to set</param>
    public static void Set<Tk, Tv>(this Dictionary<Tk, Tv> dict, Tk key, Tv val)
    {
        if (dict.ContainsKey(key)) dict[key] = val;
        else dict.Add(key, val);
    }
    #endregion collections
    #region refl flag templates
    /// <summary>
    /// Binding flags for all normal contexts.
    /// </summary>
    public const BindingFlags AllContexts =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static;
    /// <summary>
    /// Binding flags for all instance members regardless of visibility.
    /// </summary>
    public const BindingFlags AllContextsInstance =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance;
    /// <summary>
    /// Binding flags for all static members regardless of visibility.
    /// </summary>
    public const BindingFlags AllContextsStatic =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Static;
    /// <summary>
    /// Binding flags for all constructors.
    /// </summary>
    public const BindingFlags AllContextsCtor =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.CreateInstance;
    #endregion
    #region refl helpers
    /// <summary>
    /// Gets a method regardless of visibility.
    /// </summary>
    /// <param name="self">Type to get methods from</param>
    /// <param name="name">Name of the method</param>
    /// <returns></returns>
    public static MethodInfo? GetMethodAllContexts(
        this Type self,
        string name)
        => self.GetMethod(name, AllContexts);
    /// <summary>
    /// Gets
    /// </summary>
    /// <param name="self"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static PropertyInfo? GetPropertyAllContexts(
        this Type self,
        string name)
        => self.GetProperty(name, AllContexts);
    /// <summary>
    /// Returns prop backing field name
    /// </summary>
    public static string Pbfiname(string propname)
        => $"<{propname}>k__BackingField";
    /// <summary>
    /// Looks up methodinfo from T, defaults to <see cref="AllContextsInstance"/>
    /// </summary>
    /// <typeparam name="T">target type</typeparam>
    /// <param name="mname">methodname</param>
    /// <param name="context">binding flags, default private+public+instance</param>
    /// <returns></returns>
    public static MethodInfo? methodof<T>(
        string mname,
        BindingFlags context = AllContextsInstance)
        => typeof(T).GetMethod(mname, context);
    /// <summary>
    /// Looks up methodinfo from t, defaults to <see cref="AllContextsStatic"/>
    /// </summary>
    /// <param name="t">target type</param>
    /// <param name="mname">method name</param>
    /// <param name="context">binding flags, default private+public+static</param>
    /// <returns></returns>
    public static MethodInfo methodof(
        Type t,
        string mname,
        BindingFlags context = AllContextsStatic)
    {
        return t.GetMethod(mname, context);
    }
    /// <summary>
    /// gets constructorinfo from T. no cctors by default.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="pms"></param>
    /// <returns></returns>
    public static ConstructorInfo ctorof<T>(BindingFlags context = AllContextsCtor, params Type[] pms)
        => typeof(T).GetConstructor(context, null, pms, null);
    /// <summary>
    /// Gets constructorinfo from T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pms"></param>
    /// <returns></returns>
    public static ConstructorInfo ctorof<T>(params Type[] pms)
        => typeof(T).GetConstructor(pms);
    /// <summary>
    /// takes fieldinfo from T, defaults to <see cref="AllContextsInstance"/>
    /// </summary>
    /// <typeparam name="T">target type</typeparam>
    /// <param name="name">field name</param>
    /// <param name="context">context, default private+public+instance</param>
    /// <returns></returns>
    public static FieldInfo fieldof<T>(string name, BindingFlags context = AllContextsInstance)
    {
        return typeof(T).GetField(name, context);
    }
    /// <summary>
    /// Yields all loaded assemblies that match the name
    /// </summary>
    /// <param name="n">String that the assemblies' name has to contain</param>
    /// <returns>A yield ienumerable with results</returns>
    public static IEnumerable<Assembly> FindAssembliesByName(string n)
    {
        Assembly[] lasms = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = lasms.Length - 1; i > -1; i--)
            if (lasms[i].FullName.Contains(n)) yield return lasms[i];
    }
    /// <summary>
    /// Force clones an object instance through reflection
    /// </summary>
    /// <typeparam name="T">tar type</typeparam>
    /// <param name="from">source object</param>
    /// <param name="to">target object</param>
    /// <param name="context">specifies context of fields to be cloned</param>
    public static void CloneInstance<T>(T from, T to, BindingFlags context = AllContextsInstance)
    {
        Type tt = typeof(T);
        foreach (FieldInfo field in tt.GetFields(context))
        {
            if (field.IsStatic) continue;
            field.SetValue(to, field.GetValue(from), context, null, System.Globalization.CultureInfo.CurrentCulture);
        }
    }
    /// <summary>
    /// Cleans up static reference members in a type.
    /// </summary>
    /// <param name="t">Target type.</param>
    public static VT<List<string>, List<string>> CleanupStatic(this Type t)
    {
        List<string> success = new();
        List<string> failure = new();

        foreach (FieldInfo field in t.GetFields(AllContextsStatic))
            if (!field.FieldType.IsValueType)
            {
                string fullname = $"{t.FullName}.{field.Name}";
                try
                {
                    field.SetValue(null, null, AllContextsStatic, null, System.Globalization.CultureInfo.CurrentCulture);
                    success.Add(fullname);
                }
                catch (Exception ex)
                {
                    failure.Add(fullname + $" (exception: {ex.Message})");
                }
            }
        foreach (Type nested in t.GetNestedTypes(AllContextsStatic))
        {
            VT<List<string>, List<string>> res = nested.CleanupStatic();
            success.AddRange(res.a);
            failure.AddRange(res.b);
        }
        return new(success, failure, "CleanupResults", "Done", "Faliled");
    }
    #endregion
    #region randomization extensions
    /// <summary>
    /// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
    /// </summary>
    /// <param name="start">Center of the spread.</param>
    /// <param name="mDev">Maximum deviation.</param>
    /// <param name="minRes">Result lower bound.</param>
    /// <param name="maxRes">Result upper bound.</param>
    /// <returns>The resulting value.</returns>
    public static int ClampedIntDeviation(
        int start,
        int mDev,
        int minRes = int.MinValue,
        int maxRes = int.MaxValue)
        => IntClamp(RND.Range(start - mDev, start + mDev), minRes, maxRes);
    /// <summary>
    /// Returns a random deviation from start position, up to mDev in both directions. Clamps to given bounds if provided.
    /// </summary>
    /// <param name="start">Center of the spread.</param>
    /// <param name="mDev">Maximum deviation.</param>
    /// <param name="minRes">Result lower bound.</param>
    /// <param name="maxRes">Result upper bound.</param>
    /// <returns>The resulting value.</returns>
    public static float ClampedFloatDeviation(
        float start,
        float mDev,
        float minRes = float.NegativeInfinity,
        float maxRes = float.PositiveInfinity)
        => Clamp(Lerp(start - mDev, start + mDev, RND.value), minRes, maxRes);
    /// <summary>
    /// Gives you a random sign.
    /// </summary>
    /// <returns>1f or -1f on a coinflip.</returns>
    public static float RandSign()
        => RND.value > 0.5f ? -1f : 1f;
    /// <summary>
    /// Performs a random lerp between two 2d points.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Resulting vector.</returns>
    public static Vector2 V2RandLerp(Vector2 a, Vector2 b)
        => Vector2.Lerp(a, b, RND.value);
    /// <summary>
    /// Clamps a color to acceptable values.
    /// </summary>
    /// <param name="bcol"></param>
    /// <returns></returns>
    public static Color Clamped(this Color bcol)
    {
        return new(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
    }
    /// <summary>
    /// Performs a channelwise random deviation on a color.
    /// </summary>
    /// <param name="bcol">base</param>
    /// <param name="dbound">deviations</param>
    /// <param name="clamped">whether to clamp the result to reasonable values</param>
    /// <returns>resulting colour</returns>
    public static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
    {
        Color res = default;
        for (var i = 0; i < 3; i++) res[i] = bcol[i] + dbound[i] * RND.Range(-1f, 1f);
        return clamped ? res.Clamped() : res;
    }
    #endregion
    #region misc bs
    /// <summary>
    /// Attempts to parse enum value from a string, in a non-throwing fashion.
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="str">Source string</param>
    /// <param name="result">out-result.</param>
    /// <returns>Whether parsing was successful.</returns>
    public static bool TryParseEnum<T>(string str, out T? result)
        where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        foreach (T val in values)
        {
            if (str == val.ToString())
            {
                result = val;
                return true;
            }
        }
        result = default;
        return false;
    }
    /// <summary>
    /// Joins two strings with a comma and a space.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static string JoinWithComma(string x, string y)
        => $"{x}, {y}";
    /// <summary>
    /// Stitches a given collection with, returns an empty string if empty.
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="aggregator">Aggregator function. <see cref="JoinWithComma"/> by default.</param>
    /// <returns>Resulting string.</returns>
    public static string Stitch(
        this IEnumerable<string> coll!!,
        Func<string, string, string>? aggregator = null)
        => coll is null || coll.Count() is 0 ? string.Empty : coll.Aggregate(aggregator ?? JoinWithComma);
    /// <summary>
    /// Creates an <see cref="IntRect"/> from two corner points.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public static IntRect ConstructIR(IntVector2 p1, IntVector2 p2)
        => new(Min(p1.x, p2.x), Min(p1.y, p2.y), Max(p1.x, p2.x), Max(p1.y, p2.y));
    /// <summary>
    /// <see cref="IO.Path.Combine"/> but params.
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string CombinePath(params string[] parts)
        => parts.Aggregate(IO.Path.Combine);
    /// <summary>
    /// Current RainWorld instance. Uses Unity lookup, may be slow.
    /// </summary>
    public static RainWorld CRW
        => UnityEngine.Object.FindObjectOfType<RainWorld>();
    /// <summary>
    /// Gets a <see cref="StaticWorld"/> template object by type.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t)
        => StaticWorld.creatureTemplates[(int)t];
    /// <summary>
    /// Finds specified subprocess in ProcessManager (looks at both mainloop and side processes).
    /// </summary>
    /// <typeparam name="T">Type of subprocess.</typeparam>
    /// <param name="manager">must not be null.</param>
    /// <returns>Found subprocess; null if none.</returns>
    public static T? FindSubProcess<T>(this ProcessManager manager!!)
        where T : MainLoopProcess
    {
        if (manager.currentMainLoop is T tmain) return tmain;
        foreach (MainLoopProcess sideprocess in manager.sideProcesses) if (sideprocess is T tside) return tside;
        return null;
    }
    /// <summary>
    /// Gets bytes from ER of an assembly.
    /// </summary>
    /// <param name="resname">name of the resource</param>
    /// <param name="casm">target assembly. If unspecified, RK asm</param>
    /// <returns>resulting byte array</returns>
    public static byte[]? ResourceBytes(string resname, Assembly? casm = null)
    {
        if (resname is null) throw new ArgumentNullException("can not get with a null name");
        casm ??= Assembly.GetExecutingAssembly();
        IO.Stream? str = casm.GetManifestResourceStream(resname);
        byte[]? bf = str is null ? null : new byte[str.Length];
        str?.Read(bf, 0, (int)str.Length);
        return bf;
    }
    /// <summary>
    /// Gets an ER of an assembly and returns it as string. Default encoding is UTF-8
    /// </summary>
    /// <param name="resname">Name of ER</param>
    /// <param name="enc">Encoding. If none is specified, UTF-8</param>
    /// <param name="casm">assembly to get resource from. If unspecified, RK asm.</param> 
    /// <returns>Resulting string. If none is found, <c>null</c> </returns>
    public static string? ResourceAsString(string resname, Encoding? enc = null, Assembly? casm = null)
    {
        enc ??= Encoding.UTF8;
        casm ??= Assembly.GetExecutingAssembly();
        try
        {
            var bf = ResourceBytes(resname, casm);
            return bf is null ? null : enc.GetString(bf);
        }
        catch (Exception ee) { plog.LogError($"Error getting ER: {ee}"); return null; }
    }
    /// <summary>
    /// Replaces ValueTuple with some semblance of named field functionality. Names are used when comparing!
    /// </summary>
    /// <typeparam name="T1">Type of left item</typeparam>
    /// <typeparam name="T2">Type of right item</typeparam>
    /// <param name="a">Left item</param>
    /// <param name="b">Right item</param>
    /// <param name="name">Name of the instance</param>
    /// <param name="nameA">Name of the left item</param>
    /// <param name="nameB">Name of the right item</param>
    public record struct VT<T1, T2>(
        T1 a,
        T2 b,
        string name,
        string nameA,
        string nameB)
    {
        /// <summary>
        /// Creates a new instance, using default names.
        /// </summary>
        /// <param name="_a">Left item</param>
        /// <param name="_b">Right item</param>
        public VT(T1 _a, T2 _b) : this(_a, _b, defName ?? "VT", defAName ?? "a", defBName ?? "b")
        {

        }
        /// <inheritdoc/>
        public override string ToString()
            => $"{name} {{ {nameA} = {a}, {nameB} = {b} }}";
        /// <summary>
        /// Default name for instances. "VT" if null
        /// </summary>
        public static string? defName { get; private set; }
        /// <summary>
        /// Default name for left items. "a" if null
        /// </summary>
        public static string? defAName { get; private set; }
        /// <summary>
        /// Default name for right items. "b" if null
        /// </summary>
        public static string? defBName { get; private set; }
        /// <summary>
        /// Use this as a shorthand for creating several instances with similar names. Not thread safe (but that's okay because basically nothing in RW is)
        /// </summary>
        public struct Names : IDisposable
        {
            /// <summary>
            /// Sets name defaults to specified values.
            /// </summary>
            /// <param name="defname">default instance name</param>
            /// <param name="defaname">default left item name</param>
            /// <param name="defbname">default right item name</param>
            public Names(string defname, string defaname, string defbname)
            {
                defName = defname;
                defAName = defaname;
                defBName = defbname;
            }
            /// <summary>
            /// Resets the static default names.
            /// </summary>
            public void Dispose()
            {
                defName = null;
                defAName = null;
                defBName = null;
            }
        }
    }
    /// <summary>
    /// Deconstructs a KeyValuePair.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    /// <param name="kvp"></param>
    /// <param name="k"></param>
    /// <param name="v"></param>
    public static void Deconstruct<TKey, TVal>(this KeyValuePair<TKey, TVal> kvp, out TKey k, out TVal v)
    {
        k = kvp.Key;
        v = kvp.Value;
    }

#if ATMO //atmo-specific things
    internal static void DbgVerbose(
        this LOG.ManualLogSource logger!!,
        object data,
        LOG.LogLevel sev = BepInEx.Logging.LogLevel.Debug)
    {
        if (log_verbose?.Value ?? false) logger.Log(sev, data);
    }
    internal static void LogFrameTime(
        List<TimeSpan> realup_times,
        TimeSpan frame,
        LinkedList<double> realup_readings,
        int storeCap)
    {
        realup_times.Add(frame);
        if (realup_times.Count == realup_times.Capacity)
        {
            TimeSpan total = new(0);
            for (var i = 0; i < realup_times.Count; i++)
            {
                total += realup_times[i];
            }
            realup_readings.AddLast(total.TotalMilliseconds / realup_times.Capacity);
            if (realup_readings.Count > storeCap) realup_readings.RemoveFirst();
            realup_times.Clear();
        }
    }
    /// <summary>
    /// Save name for when character is not found
    /// </summary>
    public const string SlugNotFound = "ATMO_SER_NOCHAR";
    /// <summary>
    /// Cache to speed up <see cref="SlugName(int)"/>.
    /// </summary>
    internal readonly static Dictionary<int, string> SlugNameCache = new();
    /// <summary>
    /// Returns slugcat name for a given index.
    /// </summary>
    /// <param name="slugNumber"></param>
    /// <returns>Resulting name for the slugcat; <see cref="SlugNotFound"/> if the index is below zero, </returns>
    internal static string SlugName(int slugNumber)
    {
        return SlugNameCache.AddIfNone_Get(slugNumber, () =>
        {
            string? res = slugNumber switch
            {
                < 0 => SlugNotFound,
                0 => "survivor",
                1 => "monk",
                2 => "hunter",
                _ => null,
            };
            try
            {
                res ??= SlugName_WrapSB(slugNumber);
            }
            catch (TypeLoadException)
            {
                plog.LogMessage($"SlugBase not present: character #{slugNumber} has no valid name.");
            }
            res ??= ((SlugcatStats.Name)slugNumber).ToString();
            return res ?? SlugNotFound;
        });
    }
    private static string SlugName_WrapSB(int slugNumber)
        => SlugBase.PlayerManager.GetCustomPlayer(slugNumber)?.Name ?? SlugNotFound;
    internal static string ApplyEscapes(this string str!!)
    {
        StringBuilder sb = new(str);
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            //check if this escaping is sufficient
            sb = sb[i] switch
            {
                'q' => TryEscape(sb, i, '\''),
                't' => TryEscape(sb, i, '\t'),
                'n' => TryEscape(sb, i, '\n'),
                _ => sb,
            };
		}
        return sb.ToString();
        static StringBuilder TryEscape(StringBuilder sb, int ind, char repl)
		{
            if (sb.Get(ind - 1) is '\\' && sb.Get(ind - 2) is not '\\')
            {
                sb.Remove(ind - 1, 2);
                sb.Insert(ind - 1, repl);
            }
            return sb;
        }
    }
#endif
    #endregion
}
