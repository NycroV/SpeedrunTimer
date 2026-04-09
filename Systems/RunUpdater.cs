using System;
using Terraria.ModLoader;

namespace SpeedrunDisplay.Systems;

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
        var runCategory = SpeedrunDisplay.AllCategories[RunTracker.RunCategory!];
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
