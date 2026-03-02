using SpeedrunTimer.Systems;

namespace SpeedrunTimer.DataStructures;

public readonly record struct RunSplit(Split Split, uint RunTime, uint SplitTime)
{
    public void Register()
    {
        RunTracker.AvailableSplits.Remove(Split);
        RunTracker.CurrentSplits.Add(this);
    }
}