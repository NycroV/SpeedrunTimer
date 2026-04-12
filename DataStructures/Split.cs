global using CastSplit = (string localizationKey, ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D> icon, System.Func<bool> completionCheck);

using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpeedrunDisplay.Systems;
using System;
using System.Linq;

namespace SpeedrunDisplay.DataStructures;

public record class Split(string LocalizationKey, Asset<Texture2D> Icon, Func<bool> CompletionCheck)
{
    public RunSplit CreateSplit()
    {
        uint runTime = RunTracker.IGT_FrameCounter;
        uint splitTime = runTime - (RunTracker.CurrentSplits.Count > 0 ? RunTracker.CurrentSplits[^1].RunTime : 0);
        return new(this, runTime, splitTime);
    }

    public static implicit operator CastSplit(Split split) => (split.LocalizationKey, split.Icon, split.CompletionCheck);

    public static implicit operator Split(CastSplit casted)
    {
        var validSplits = SpeedrunDisplay.AllSplits.Values.Where(s => s.LocalizationKey == casted.localizationKey);
        var matchedSplits = validSplits.Where(c => c.Icon == casted.icon && c.CompletionCheck == casted.completionCheck);
        return matchedSplits.FirstOrDefault(defaultValue: null);
    }
}
