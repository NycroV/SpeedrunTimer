global using CastRunSplit = (string splitKey, uint splitTime, uint runTime);

using SpeedrunDisplay.Systems;

namespace SpeedrunDisplay.DataStructures;

public readonly record struct RunSplit(Split Split, uint RunTime, uint SplitTime)
{
    public void Register()
    {
        RunTracker.AvailableSplits.Remove(Split);
        RunTracker.CurrentSplits.Add(this);
    }

    public static implicit operator CastRunSplit(RunSplit split) => (SpeedrunDisplay.AllSplits.Inverse[split.Split], split.SplitTime, split.RunTime);
}