global using CastCompletedRun = (SpeedrunDisplay.DataStructures.Category category, System.Collections.ObjectModel.ReadOnlyCollection<SpeedrunDisplay.DataStructures.RunSplit> splits, System.TimeSpan IGT, System.TimeSpan RTA);

using System;
using System.Collections.ObjectModel;

namespace SpeedrunDisplay.DataStructures;

public readonly record struct CompletedRun(Category Category, ReadOnlyCollection<RunSplit> Splits, TimeSpan IGT, TimeSpan RTA)
{
    public static implicit operator CastCompletedRun(CompletedRun completedRun) => (completedRun.Category, completedRun.Splits, completedRun.IGT, completedRun.RTA);
}