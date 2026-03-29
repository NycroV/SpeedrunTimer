using System;
using Terraria.ModLoader;

namespace SpeedrunTimer.Systems;

public class RunCategorySafetyCheck : ModPlayer
{
    // If the player has unloaded the mod from which the current active run was registered,
    // we need to account for that and cancel the current run.
    public override void OnEnterWorld()
    {
        if (RunTracker.RunCategory is not null && !SpeedrunTimer.AllCategories.ContainsKey(RunTracker.RunCategory))
            RunTracker.CancelRun();
    }
}

public class RunUpdater : ModSystem
{
    public override void PostUpdatePlayers()
    {
        // Obvious...
        if (!RunTracker.RunActive)
            return;

        // We don't want to start the timer until the player enters the world,
        // so we do this here instead of in the mod system.
        if (RunTracker.IGT_FrameCounter == 0)
            RunTracker.RTA_RunStart = DateTime.UtcNow;

        // Retrieve the current run category and the run end criteria.
        var runCategory = SpeedrunTimer.AllCategories[RunTracker.RunCategory!];
        var completionSplit = runCategory.CompletionSplit;

        // Complete the run if the run end criteria has been met.
        if (completionSplit.CompletionCheck())
        {
            completionSplit.CreateSplit().Register();
            RunTracker.CompleteRun();
            return;
        }

        // Clone the collection as it may be modified during enumeration
        var splits = RunTracker.AvailableSplits.ToArray();

        // Update splits
        foreach (var split in splits)
        {
            if (!split.CompletionCheck())
                continue;

            split.CreateSplit().Register();
        }

        // Update IGT
        RunTracker.IGT_FrameCounter++;

        // We do not actually keep track of RTA, instead subtracting current
        // UTC DateTime from the cached start-of-run UTC DateTime when we need the run length.
    }
}
