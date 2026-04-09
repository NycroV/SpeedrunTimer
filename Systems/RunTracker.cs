using SpeedrunDisplay.Config;
using SpeedrunDisplay.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SpeedrunDisplay.Systems;

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

    internal static readonly string ActiveRunFilePath = Path.Combine(Main.SavePath, "SpeedrunDisplay", "ActiveRun.txt");

    // Re-loads the last active run, if there was one.
    public override void PostSetupContent()
    {
        TryLoadActiveRun();
        SpeedrunConfig.Instance.GetType(); // Force a config cache by fetching the property, we don't actually need it
    }

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
        AvailableSplits.AddRange(SpeedrunDisplay.AllSplits.Values);
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
        var category = SpeedrunDisplay.AllCategories[RunCategory!];
        var splits = CurrentSplits.ToArray().AsReadOnly();
        TimeSpan rta = DateTime.UtcNow - RTA_RunStart;
        TimeSpan igt = TimeSpan.FromSeconds(IGT_FrameCounter / 60f);

        RunCategory = null;
        RTA_RunStart = DateTime.UnixEpoch;
        IGT_FrameCounter = 0;
        LastCompletedRun = new(category, splits, rta, igt);

        CurrentSplits.Clear();
        AvailableSplits.Clear();
    }

    internal static void ExportLastRun()
    {
        if (nativefiledialog.NFD_SaveDialog("txt", null, out string filePath) != nativefiledialog.nfdresult_t.NFD_OKAY)
            return;

        if (Path.GetExtension(filePath) != ".txt")
            filePath += ".txt";

        var run = LastCompletedRun.Value;
        string text = $"{run.Category.LocalizationKey.Fetch()}\n------\n";

        int splitCount = run.Splits.Count;
        int longestNameLength = 0;
        int longestSplitLength = 0;
        int longestRunLength = 0;

        string[] splits = [.. run.Splits.Select(s => s.Split.LocalizationKey.Fetch())];
        string[] splitTimes = [.. run.Splits.Select(s => TimeSpan.FromSeconds(s.SplitTime / 60f).Format(fractionalSeconds: true))];
        string[] runTimes = [.. run.Splits.Select(s => TimeSpan.FromSeconds(s.RunTime / 60f).Format(fractionalSeconds: true))];

        for (int i = 0; i < splitCount; i++)
        {
            if (splits[i].Length > longestNameLength)
            {
                longestNameLength = splits[i].Length;

                for (int j = 0; j < i; j++)
                    splits[j] = splits[j] + new string(' ', longestNameLength - splits[j].Length);
            }

            else if (splits[i].Length < longestNameLength)
                splits[i] = splits[i] + new string(' ', longestNameLength - splits[i].Length);


            if (splitTimes[i].Length > longestSplitLength)
            {
                longestSplitLength = splitTimes[i].Length;

                for (int j = 0; j < i; j++)
                    splitTimes[j] = new string(' ', longestSplitLength - splitTimes[i].Length) + splitTimes[j];
            }

            else if (splitTimes[i].Length < longestSplitLength)
                splitTimes[i] = splitTimes[i] + new string(' ', longestSplitLength - splitTimes[i].Length);

            if (runTimes[i].Length > longestRunLength)
            {
                longestRunLength = runTimes[i].Length;

                for (int j = 0; j < i; j++)
                    runTimes[j] = new string(' ', longestRunLength - runTimes[i].Length) + runTimes[j];
            }

            else if (runTimes[i].Length < longestRunLength)
                runTimes[i] = runTimes[i] + new string(' ', longestRunLength - runTimes[i].Length);
        }

        for (int i = 0; i < splitCount; i++)
            text += $"{splits[i]}  -  {splitTimes[i]}  |  {runTimes[i]}\n";

        text += $"------\n{run.IGT.Format(fractionalSeconds: true)} IGT  --  {run.RTA.Format(fractionalSeconds: true)} RTA";
        File.WriteAllText(filePath, text);
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

            if (splitParts.Length != 3 || !SpeedrunDisplay.AllSplits.TryGetValue(splitParts[0], out var split))
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

        AvailableSplits.AddRange(SpeedrunDisplay.AllSplits.Values);

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
            SpeedrunDisplay.AllSplits.Inverse[s.Split],
            s.RunTime.ToString(),
            s.SplitTime.ToString()));

        string splitsLine = string.Join('/', stringSplits);
        string activeRun = string.Join('\n', RunCategory, RTA_RunStart.ToString(), IGT_FrameCounter.ToString(), splitsLine);
        string directory = Path.GetDirectoryName(ActiveRunFilePath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(ActiveRunFilePath, activeRun);
    }

    // This ensures that the "default run category" config and the saved
    // category from the last active run are valid. If the user unloads
    // the mod which added their specific category, or changes the saved category
    // from their last active run, it can cause the game to crash by trying
    // to access a category that does not exist.
    internal static void ValidateCategoryTypes(Action orig)
    {
        // ConfigManager.FinishSetup()
        orig();

        if (RunCategory is not null && !SpeedrunDisplay.AllCategories.ContainsKey(RunCategory))
            RunCategory = null;

        if (!SpeedrunDisplay.AllCategories.ContainsKey(SpeedrunConfig.Instance.DefaultRunCategory))
        {
            SpeedrunConfig.Instance.DefaultRunCategory = SpeedrunDisplay.AllCategories.First().Key;
            SpeedrunConfig.Instance.SaveChanges();
        }

        CategoriesValidated = true;
    }
}