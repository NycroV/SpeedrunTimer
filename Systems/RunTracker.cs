using SpeedrunTimer.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace SpeedrunTimer.Systems;

public class RunTracker : ModSystem
{
    #region Run Type

    /// <summary>
    /// Indicates whether a run is currently active.
    /// </summary>
    public static bool RunActive => RunCategory is not null;

    /// <summary>
    /// Indicates the currently active run category, if a run is active. Otherwise <see langword="null"/>.
    /// </summary>
    public static string? RunCategory { get; internal set; } = null;

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

    internal static string ActiveRunFilePath => Path.Combine(Main.SavePath, "SpeedrunTimer", "ActiveRun.txt");

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

    // Re-loads the last active run, if there was one.
    public override void Load() => TryLoadActiveRun();

    internal static void TryLoadActiveRun()
    {
        if (!File.Exists(ActiveRunFilePath))
            return;

        string activeRun = File.ReadAllText(ActiveRunFilePath);
        string[] runParts = activeRun.Split('\n');

        if (runParts.Length != 3)
            return;

        if (!DateTime.TryParse(runParts[1], out var runStart))
            return;

        if (!uint.TryParse(runParts[2], out var frameCounter))
            return;

        RunCategory = runParts[0];
        RTA_RunStart = runStart;
        IGT_FrameCounter = frameCounter;
    }

    // Saves the currently active run, if there is one.
    public override void Unload() => TrySaveActiveRun();

    internal static void TrySaveActiveRun()
    {
        if (!RunActive)
        {
            if (File.Exists(ActiveRunFilePath))
                File.Delete(ActiveRunFilePath);

            return;
        }

        string activeRun = string.Join('\n', RunCategory, RTA_RunStart.ToString(), IGT_FrameCounter.ToString());
        string directory = Path.GetDirectoryName(ActiveRunFilePath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(ActiveRunFilePath, activeRun);
    }
}
