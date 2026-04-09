using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpeedrunDisplay.Systems;
using System;

namespace SpeedrunDisplay.DataStructures;

public record class Split(string LocalizationKey, Asset<Texture2D> Icon, Func<bool> CompletionCheck)
{
    public RunSplit CreateSplit()
    {
        uint runTime = RunTracker.IGT_FrameCounter;
        uint splitTime = runTime - (RunTracker.CurrentSplits.Count > 0 ? RunTracker.CurrentSplits[^1].RunTime : 0);
        return new(this, runTime, splitTime);
    }
}
