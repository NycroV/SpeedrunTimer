using System;
using System.Collections.ObjectModel;

namespace SpeedrunDisplay.DataStructures;

public readonly record struct CompletedRun(Category Category, ReadOnlyCollection<RunSplit> Splits, TimeSpan RTA, TimeSpan IGT);