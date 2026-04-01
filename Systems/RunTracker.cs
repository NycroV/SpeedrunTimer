using SpeedrunTimer.Config;
using SpeedrunTimer.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SpeedrunTimer.Systems;

public class RunTracker : ModSystem
{
    #region Run Type

    /// <summary>
    /// Indicates whether post-load validation checks have been run for categories.
    /// </summary>
    public static bool CategoriesValidated { get; private set; } = false;

    /// <summary>
    /// Indicates whether a run is currently active.
    /// </summary>
    public static bool RunActive => RunCategory is not null;

    /// <summary>
    /// Indicates the currently active run category, if a run is active. Otherwise <see langword="null"/>.
    /// </summary>
    public static string RunCategory { get; internal set; } = null;

    #endregion

    #region Timing

    /// <summary>
    /// Indicates the exact start time (in UTC) of the current run, if a run is active. Otherwise <see cref="DateTime.UnixEpoch"/>.
    /// </summary>
    public static DateTime RTA_RunStart { get; internal set; } = DateTime.UnixEpoch;

    /// <summary>
    /// Indicates the number of elapsed game ticks of the current run, if a run is active. Otherwise <see langword="0"/>.
    /// </summary>
    public static uint IGT_FrameCounter { get; internal set; } = 0u;

    #endregion

    #region Splits

    /// <summary>
    /// The list of registered splits that have not yet been triggered in the current run.
    /// </summary>
    public static readonly List<Split> AvailableSplits = [];

    /// <summary>
    /// The current run splits.
    /// </summary>
    public static readonly List<RunSplit> CurrentSplits = [];

    #endregion

    /// <summary>
    /// Contains info from the last completed run, if one was just completed. Otherwise <see langword="null"/>.
    /// </summary>
    public static CompletedRun? LastCompletedRun { get; private set; } = null;

    internal static readonly string ActiveRunFilePath = Path.Combine(Main.SavePath, "SpeedrunTimer", "ActiveRun.txt");

    // Re-loads the last active run, if there was one.
    public override void PostSetupContent() => TryLoadActiveRun();

    // Verifies that the last active run type is available, and that the configured default run type is valid

    public override void Load()
    {
        MonoModHooks.Add(typeof(ConfigManager).GetMethod("FinishSetup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, Type.EmptyTypes), ValidateCategoryTypes);
        Main.instance.Exiting += (_, _) => TrySaveActiveRun();
    }

    // Saves the currently active run, if there is one.
    public override void Unload() => TrySaveActiveRun();

    internal static void StartRun(string runCategory)
    {
        RunCategory = runCategory;
        RTA_RunStart = default;
        IGT_FrameCounter = 0;
        LastCompletedRun = null;

        CurrentSplits.Clear();
        AvailableSplits.Clear();
        AvailableSplits.AddRange(SpeedrunTimer.AllSplits.Values);
    }

    internal static void CancelRun()
    {
        RunCategory = null;
        RTA_RunStart = DateTime.UnixEpoch;
        IGT_FrameCounter = 0;
        LastCompletedRun = null;

        CurrentSplits.Clear();
        AvailableSplits.Clear();
    }

    internal static void CompleteRun()
    {
        var category = SpeedrunTimer.AllCategories[RunCategory!];
        var splits = CurrentSplits.AsReadOnly();
        TimeSpan rta = DateTime.UtcNow - RTA_RunStart;
        TimeSpan igt = TimeSpan.FromSeconds(IGT_FrameCounter / 60f);

        RunCategory = null;
        RTA_RunStart = DateTime.UnixEpoch;
        IGT_FrameCounter = 0;
        LastCompletedRun = new(category, splits, rta, igt);

        CurrentSplits.Clear();
        AvailableSplits.Clear();
    }

    internal static void TryLoadActiveRun()
    {
        if (!File.Exists(ActiveRunFilePath))
            return;

        string activeRun = File.ReadAllText(ActiveRunFilePath);
        string[] runParts = activeRun.Split('\n');

        if (runParts.Length != 4)
            return;

        if (!DateTime.TryParse(runParts[1], out var runStart))
            return;

        if (!uint.TryParse(runParts[2], out var frameCounter))
            return;

        List<RunSplit> runSplits = [];

        foreach (string runSplit in runParts[3].Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var splitParts = runSplit.Split('|');

            if (splitParts.Length != 3 || !SpeedrunTimer.AllSplits.TryGetValue(splitParts[0], out var split))
                return;

            if (!uint.TryParse(splitParts[1], out var runTime))
                return;

            if (!uint.TryParse(splitParts[2], out var splitTime))
                return;

            runSplits.Add(new(split, runTime, splitTime));
        }

        RunCategory = runParts[0];
        RTA_RunStart = runStart;
        IGT_FrameCounter = frameCounter;

        AvailableSplits.AddRange(SpeedrunTimer.AllSplits.Values);

        foreach (var runSplit in runSplits)
            runSplit.Register();
    }

    internal static void TrySaveActiveRun()
    {
        if (!RunActive)
        {
            if (File.Exists(ActiveRunFilePath))
                File.Delete(ActiveRunFilePath);

            return;
        }

        var stringSplits = CurrentSplits.Select(s => string.Join('|',
            SpeedrunTimer.AllSplits.Inverse[s.Split],
            s.RunTime.ToString(),
            s.SplitTime.ToString()));

        string splitsLine = string.Join('/', stringSplits);
        string activeRun = string.Join('\n', RunCategory, RTA_RunStart.ToString(), IGT_FrameCounter.ToString(), splitsLine);
        string directory = Path.GetDirectoryName(ActiveRunFilePath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(ActiveRunFilePath, activeRun);
    }

    internal static void ValidateCategoryTypes(Action orig)
    {
        orig();

        if (RunCategory is not null && !SpeedrunTimer.AllCategories.ContainsKey(RunCategory))
            RunCategory = null;

        if (!SpeedrunTimer.AllCategories.ContainsKey(SpeedrunConfig.Instance.DefaultRunCategory))
        {
            SpeedrunConfig.Instance.DefaultRunCategory = SpeedrunTimer.AllCategories.First().Key;
            SpeedrunConfig.Instance.SaveChanges();
        }

        CategoriesValidated = true;
    }
}