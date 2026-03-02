using System;
using System.Collections.ObjectModel;

namespace SpeedrunTimer.DataStructures;

public readonly record struct CompletedRun(Category Category, ReadOnlyCollection<RunSplit> Splits, TimeSpan RTA, TimeSpan IGT);