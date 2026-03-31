global using static SpeedrunTimer.Utils.SpeedrunUtil;

using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using ReLogic.Content;
using SpeedrunTimer.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SpeedrunTimer;

public class SpeedrunTimer : Mod
{
    /// <summary>
    /// The collection of all registered splits from all mods.
    /// </summary>
    public static readonly BidirectionalDictionary<string, Split> AllSplits = new();

    /// <summary>
    /// The collection of all registered run categories from all mods.
    /// </summary>
    public static readonly BidirectionalDictionary<string, Category> AllCategories = new();

    // Load vanilla splits and categories
    public override void PostSetupContent()
    {
        AllSplits.AddRange(new Dictionary<string, Split>()
        {
            ["KingSlime"] = new(GetLocalizationKey("Splits.KingSlime"), null!, () => NPC.downedSlimeKing),
            ["EyeOfCthulhu"] = new(GetLocalizationKey("Splits.EyeOfCthulhu"), null!, () => NPC.downedBoss1),
            ["EvilBoss"] = new(GetLocalizationKey("Splits.EvilBoss"), null!, () => NPC.downedBoss2),
            ["QueenBee"] = new(GetLocalizationKey("Splits.QueenBee"), null!, () => NPC.downedQueenBee),
            ["Deerclops"] = new(GetLocalizationKey("Splits.Deerclops"), null!, () => NPC.downedDeerclops),
            ["Skeletron"] = new(GetLocalizationKey("Splits.Skeletron"), null!, () => NPC.downedBoss3),
            ["WallOfFlesh"] = new(GetLocalizationKey("Splits.WallOfFlesh"), null!, () => Main.hardMode),
            ["QueenSlime"] = new(GetLocalizationKey("Splits.QueenSlime"), null!, () => NPC.downedQueenSlime),
            ["TheDestroyer"] = new(GetLocalizationKey("Splits.TheDestroyer"), null!, () => NPC.downedMechBoss1),
            ["TheTwins"] = new(GetLocalizationKey("Splits.TheTwins"), null!, () => NPC.downedMechBoss2),
            ["SkeletronPrime"] = new(GetLocalizationKey("Splits.SkeletronPrime"), null!, () => NPC.downedMechBoss3),
            ["Plantera"] = new(GetLocalizationKey("Splits.Plantera"), null!, () => NPC.downedPlantBoss),
            ["Golem"] = new(GetLocalizationKey("Splits.Golem"), null!, () => NPC.downedGolemBoss),
            ["DukeFishron"] = new(GetLocalizationKey("Splits.DukeFishron"), null!, () => NPC.downedFishron),
            ["EmpressOfLight"] = new(GetLocalizationKey("Splits.EmpressOfLight"), null!, () => NPC.downedEmpressOfLight),
            ["LunaticCultist"] = new(GetLocalizationKey("Splits.LunaticCultist"), null!, () => NPC.downedAncientCultist),
            ["SolarPillar"] = new(GetLocalizationKey("Splits.SolarPillar"), null!, () => NPC.downedTowerSolar),
            ["NebulaPillar"] = new(GetLocalizationKey("Splits.NebulaPillar"), null!, () => NPC.downedTowerNebula),
            ["VortexPillar"] = new(GetLocalizationKey("Splits.VortexPillar"), null!, () => NPC.downedTowerVortex),
            ["StardustPillar"] = new(GetLocalizationKey("Splits.StardustPillar"), null!, () => NPC.downedTowerStardust),
            ["MoonLord"] = new(GetLocalizationKey("Splits.MoonLord"), null!, () => NPC.downedMoonlord),
            ["AllBosses"] = new(GetLocalizationKey("Splits.AllBosses"), null!, () => NPC.downedMoonlord) // TODO: All bosses
        });

        AllCategories.AddRange(new Dictionary<string, Category>()
        {
            ["Any%"] = new(GetLocalizationKey("Categories.AnyPercent"), GetSplit("MoonLord")),
            ["AllBosses%"] = new(GetLocalizationKey("Categories.AllBosses"), GetSplit("AllBosses"))
        });
    }

    /// <summary>
    /// Retrieves a split via the key it was registered with.
    /// </summary>
    public static Split GetSplit(string splitKey) => AllSplits[splitKey];

    /// <summary>
    /// Registers a custom split to be used in new speedrun categories.
    /// </summary>
    /// <param name="splitKey">The unique key to register this split with.</param>
    /// <param name="splitNameLocalization">The localization key for this split's name in the timer UI.</param>
    /// <param name="splitIcon">A small icon to display by the split name in the timer UI.</param>
    /// <param name="completionCheck">A function that returns <see langword="true"/> when the split should be triggered.</param>
    public static object AddSplit(string splitKey, string splitNameLocalization, Asset<Texture2D> splitIcon, Func<bool> completionCheck)
    {
        Split split = new(splitNameLocalization, splitIcon, completionCheck);
        AllSplits.Add(splitKey, split);
        return null!;
    }

    /// <summary>
    /// Registers a custom category for new runs to utilize.
    /// </summary>
    /// <param name="categoryKey">The unique key to register this category with.</param>
    /// <param name="categoryLocalization">The localization key for this split's name in the timer UI.</param>
    /// <param name="completionSplit">The split that, when triggered, marks run completion.</param>
    public static object AddCategory(string categoryKey, string categoryLocalization, Split completionSplit)
    {
        Category category = new(categoryLocalization, completionSplit);
        AllCategories.Add(categoryKey, category);
        return null!;
    }

    public override object Call(params object[] args)
    {
        return args[0].ToString()!.ToLower() switch
        {
            "getsplit" => GetSplit((args[1] as string)!),
            "addsplit" => AddSplit((args[1] as string)!, (args[2] as string)!, (args[3] as Asset<Texture2D>)!, (args[4] as Func<bool>)!),
            "addcategory" => AddCategory((args[1] as string)!, (args[2] as string)!, (args[3] as Split)!),
            _ => throw new NotImplementedException($"Did not recognize {args[0]} as a valid mod call!")
        };
    }
}
