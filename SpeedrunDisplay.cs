global using static SpeedrunDisplay.Utils.SpeedrunUtil;

using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using ReLogic.Content;
using SpeedrunDisplay.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SpeedrunDisplay;

public class SpeedrunDisplay : Mod
{
    /// <summary>
    /// The collection of all registered splits from all mods.
    /// </summary>
    public static readonly BidirectionalDictionary<string, Split> AllSplits = new();

    /// <summary>
    /// The collection of all registered run categories from all mods.
    /// </summary>
    public static readonly BidirectionalDictionary<string, Category> AllCategories = new();

    public override void Load()
    {
        static Asset<Texture2D> BossHead(int bossId) => ModContent.Request<Texture2D>($"Terraria/Images/NPC_Head_Boss_{bossId}");
        static Asset<Texture2D> SpeedrunAsset(string name) => ModContent.Request<Texture2D>($"SpeedrunDisplay/Assets/Textures/{name}");

        AllSplits.AddRange(new Dictionary<string, Split>()
        {
            ["KingSlime"] = new(GetLocalizationKey("Splits.KingSlime"), BossHead(7), () => NPC.downedSlimeKing),
            ["EyeOfCthulhu"] = new(GetLocalizationKey("Splits.EyeOfCthulhu"), BossHead(0), () => NPC.downedBoss1),
            ["EvilBoss"] = new(GetLocalizationKey("Splits.EvilBoss"), SpeedrunAsset("EvilBossIcon"), () => NPC.downedBoss2),
            ["QueenBee"] = new(GetLocalizationKey("Splits.QueenBee"), BossHead(14), () => NPC.downedQueenBee),
            ["Deerclops"] = new(GetLocalizationKey("Splits.Deerclops"), SpeedrunAsset("DeerclopsIcon"), () => NPC.downedDeerclops),
            ["Skeletron"] = new(GetLocalizationKey("Splits.Skeletron"), BossHead(19), () => NPC.downedBoss3),
            ["WallOfFlesh"] = new(GetLocalizationKey("Splits.WallOfFlesh"), BossHead(22), () => Main.hardMode),
            ["QueenSlime"] = new(GetLocalizationKey("Splits.QueenSlime"), BossHead(38), () => NPC.downedQueenSlime),
            ["TheDestroyer"] = new(GetLocalizationKey("Splits.TheDestroyer"), BossHead(25), () => NPC.downedMechBoss1),
            ["TheTwins"] = new(GetLocalizationKey("Splits.TheTwins"), BossHead(16), () => NPC.downedMechBoss2),
            ["SkeletronPrime"] = new(GetLocalizationKey("Splits.SkeletronPrime"), BossHead(18), () => NPC.downedMechBoss3),
            ["Plantera"] = new(GetLocalizationKey("Splits.Plantera"), BossHead(11), () => NPC.downedPlantBoss),
            ["Golem"] = new(GetLocalizationKey("Splits.Golem"), BossHead(5), () => NPC.downedGolemBoss),
            ["DukeFishron"] = new(GetLocalizationKey("Splits.DukeFishron"), BossHead(4), () => NPC.downedFishron),
            ["EmpressOfLight"] = new(GetLocalizationKey("Splits.EmpressOfLight"), BossHead(37), () => NPC.downedEmpressOfLight),
            ["LunaticCultist"] = new(GetLocalizationKey("Splits.LunaticCultist"), BossHead(31), () => NPC.downedAncientCultist),
            ["SolarPillar"] = new(GetLocalizationKey("Splits.SolarPillar"), BossHead(27), () => NPC.downedTowerSolar),
            ["NebulaPillar"] = new(GetLocalizationKey("Splits.NebulaPillar"), BossHead(29), () => NPC.downedTowerNebula),
            ["VortexPillar"] = new(GetLocalizationKey("Splits.VortexPillar"), BossHead(28), () => NPC.downedTowerVortex),
            ["StardustPillar"] = new(GetLocalizationKey("Splits.StardustPillar"), BossHead(30), () => NPC.downedTowerStardust),
            ["MoonLord"] = new(GetLocalizationKey("Splits.MoonLord"), BossHead(8), () => NPC.downedMoonlord),

            ["AllBosses"] = new(GetLocalizationKey("Splits.AllBosses"), SpeedrunAsset("AllBossesIcon"), () =>
                NPC.downedMoonlord &&
                NPC.downedAncientCultist &&
                NPC.downedEmpressOfLight &&
                NPC.downedFishron &&
                NPC.downedGolemBoss &&
                NPC.downedPlantBoss &&
                NPC.downedMechBoss3 &&
                NPC.downedMechBoss2 &&
                NPC.downedMechBoss1 &&
                NPC.downedQueenSlime &&
                Main.hardMode &&
                NPC.downedBoss3 &&
                NPC.downedDeerclops &&
                NPC.downedQueenBee &&
                NPC.downedBoss2 &&
                NPC.downedBoss1 &&
                NPC.downedSlimeKing)
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
    public static Split AddSplit(string splitKey, string splitNameLocalization, Asset<Texture2D> splitIcon, Func<bool> completionCheck)
    {
        Split split = new(splitNameLocalization, splitIcon, completionCheck);
        AllSplits.Add(splitKey, split);
        return split;
    }

    /// <summary>
    /// Registers a custom category for new runs to utilize.
    /// </summary>
    /// <param name="categoryKey">The unique key to register this category with.</param>
    /// <param name="categoryLocalization">The localization key for this split's name in the timer UI.</param>
    /// <param name="completionSplit">The split that, when triggered, marks run completion.</param>
    public static Category AddCategory(string categoryKey, string categoryLocalization, Split completionSplit)
    {
        Category category = new(categoryLocalization, completionSplit);
        AllCategories.Add(categoryKey, category);
        return category;
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
