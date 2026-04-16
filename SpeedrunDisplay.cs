global using static SpeedrunDisplay.Utils.SpeedrunUtil;

using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using ReLogic.Content;
using SpeedrunDisplay.Config;
using SpeedrunDisplay.DataStructures;
using SpeedrunDisplay.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
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

    /// <summary>
    /// SpeedrunDisplay mod instance.
    /// </summary>
    public static SpeedrunDisplay Instance { get; private set; } = null;

    // Allows usage in static contexts
    internal static string GetKey(string suffix) => Instance.GetLocalizationKey(suffix);
    internal static Asset<Texture2D> ItemSprite(int itemId) => ModContent.Request<Texture2D>($"Terraria/Images/Item_{itemId}");
    internal static Asset<Texture2D> BossHead(int bossId) => ModContent.Request<Texture2D>($"Terraria/Images/NPC_Head_Boss_{bossId}");
    internal static Asset<Texture2D> SpeedrunAsset(string name) => ModContent.Request<Texture2D>($"SpeedrunDisplay/Assets/Textures/{name}");
    internal static Asset<Texture2D> VanillaAsset(string name) => ModContent.Request<Texture2D>($"Terraria/Images/{name}");

    // Used for config-based splits
    internal static bool Extended() { return SpeedrunConfig.Instance.ExtendedVariants; }
    internal static bool BundlePillars() { return SpeedrunConfig.Instance.BundlePillars; }
    internal static bool BundleEvils() { return SpeedrunConfig.Instance.BundleEvils; }

    public override void Load()
    {
        Instance = this;

        AllSplits.AddRange(new Dictionary<string, Split>()
        {
            ["KingSlime"] = new(GetKey("Splits.KingSlime"), BossHead(7), () => NPC.downedSlimeKing),
            ["EyeOfCthulhu"] = new(GetKey("Splits.EyeOfCthulhu"), BossHead(0), () => NPC.downedBoss1),
            ["EaterOfWorlds"] = new(GetKey("Splits.EaterOfWorlds"), BossHead(2), () => NPC.downedBoss2 && !WorldGen.crimson && !BundleEvils()),
            ["BrainOfCthulhu"] = new(GetKey("Splits.BrainOfCthulhu"), BossHead(23), () => NPC.downedBoss2 && WorldGen.crimson && !BundleEvils()),
            ["EvilBoss"] = new(GetKey("Splits.EvilBoss"), SpeedrunAsset("EvilBossIcon"), () => NPC.downedBoss2 && BundleEvils()),
            ["QueenBee"] = new(GetKey("Splits.QueenBee"), BossHead(14), () => NPC.downedQueenBee),
            ["Deerclops"] = new(GetKey("Splits.Deerclops"), BossHead(39), () => NPC.downedDeerclops),
            ["Skeletron"] = new(GetKey("Splits.Skeletron"), BossHead(19), () => NPC.downedBoss3),
            ["WallOfFlesh"] = new(GetKey("Splits.WallOfFlesh"), BossHead(22), () => Main.hardMode),
            ["QueenSlime"] = new(GetKey("Splits.QueenSlime"), BossHead(38), () => NPC.downedQueenSlime),
            ["TheDestroyer"] = new(GetKey("Splits.TheDestroyer"), BossHead(25), () => NPC.downedMechBoss1),
            ["TheTwins"] = new(GetKey("Splits.TheTwins"), BossHead(16), () => NPC.downedMechBoss2),
            ["SkeletronPrime"] = new(GetKey("Splits.SkeletronPrime"), BossHead(18), () => NPC.downedMechBoss3),
            ["Plantera"] = new(GetKey("Splits.Plantera"), BossHead(11), () => NPC.downedPlantBoss),
            ["Golem"] = new(GetKey("Splits.Golem"), BossHead(5), () => NPC.downedGolemBoss),
            ["DukeFishron"] = new(GetKey("Splits.DukeFishron"), BossHead(4), () => NPC.downedFishron),
            ["EmpressOfLight"] = new(GetKey("Splits.EmpressOfLight"), BossHead(37), () => NPC.downedEmpressOfLight),
            ["LunaticCultist"] = new(GetKey("Splits.LunaticCultist"), BossHead(31), () => NPC.downedAncientCultist),
            ["SolarPillar"] = new(GetKey("Splits.SolarPillar"), BossHead(27), () => NPC.downedTowerSolar && !BundlePillars()),
            ["NebulaPillar"] = new(GetKey("Splits.NebulaPillar"), BossHead(29), () => NPC.downedTowerNebula && !BundlePillars()),
            ["VortexPillar"] = new(GetKey("Splits.VortexPillar"), BossHead(28), () => NPC.downedTowerVortex && !BundlePillars()),
            ["StardustPillar"] = new(GetKey("Splits.StardustPillar"), BossHead(30), () => NPC.downedTowerStardust && !BundlePillars()),
            ["Pillars"] = new(GetKey("Splits.Pillars"), SpeedrunAsset("PillarsIcon"), () => NPC.downedTowers && BundlePillars()),
            ["MoonLord"] = new(GetKey("Splits.MoonLord"), BossHead(8), () => NPC.downedMoonlord),

            ["AllBosses"] = new(GetKey("Splits.AllBosses"), SpeedrunAsset("AllBossesIcon"), () =>
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
                NPC.downedSlimeKing),

            // Split only available in "All Pre-HM Bosses" Category
            ["AllPreHardmodeBosses"] = new(GetKey("Splits.AllPreHardmodeBosses"), SpeedrunAsset("AllBossesIcon"), () =>
                RunTracker.RunCategory == "AllPreHardmodeBosses" &&
                Main.hardMode &&
                NPC.downedBoss3 &&
                NPC.downedDeerclops &&
                NPC.downedQueenBee &&
                NPC.downedBoss2 &&
                NPC.downedBoss1 &&
                NPC.downedSlimeKing),

            // Split only available in "Nights Edge" category
            ["CraftNightsEdge"] = new(GetKey("Splits.CraftNightsEdge"), ItemSprite(ItemID.NightsEdge), () => RunTracker.RunCategory == "NightsEdge" && Main.LocalPlayer.inventory.Any(i => i.stack > 0 && i.type == ItemID.NightsEdge)),

            // Extended splits
            ["SlimeRain"] = new(GetKey("Splits.SlimeRain"), SpeedrunAsset("SlimeRain"), () => Extended() && DownedSystem.downedSlimeRain),
            ["BloodMoon"] = new(GetKey("Splits.BloodMoon"), SpeedrunAsset("BloodMoon"), () => Extended() && DownedSystem.downedBloodMoon),
            ["GoblinArmy"] = new(GetKey("Splits.GoblinArmy"), VanillaAsset("Extra_9"), () => Extended() && NPC.downedGoblins),

            ["FrostLegion"] = new(GetKey("Splits.FrostLegion"), VanillaAsset("Extra_7"), () => Extended() && NPC.downedFrost),
            ["SolarEclipse"] = new(GetKey("Splits.SolarEclipse"), SpeedrunAsset("SolarEclipse"), () => Extended() && DownedSystem.downedSolarEclipse),
            ["PirateInvasion"] = new(GetKey("Splits.PirateInvasion"), VanillaAsset("Extra_11"), () => Extended() && NPC.downedPirates),
            ["PumpkinMoon"] = new(GetKey("Splits.PumpkinMoon"), VanillaAsset("Extra_12"), () => Extended() && DownedSystem.downedPumpkinMoon),
            ["FrostMoon"] = new(GetKey("Splits.FrostMoon"), VanillaAsset("Extra_8"), () => Extended() && DownedSystem.downedFrostMoon),
            ["MartianMadness"] = new(GetKey("Splits.MartianMadness"), VanillaAsset("Extra_10"), () => Extended() && NPC.downedMartians),

            ["OldOnesArmyT1"] = new(GetKey("Splits.OldOnesArmyT1"), SpeedrunAsset("OldOnesArmy"), () => Extended() && DD2Event.DownedInvasionT1),
            ["OldOnesArmyT2"] = new(GetKey("Splits.OldOnesArmyT2"), SpeedrunAsset("OldOnesArmy"), () => Extended() && DD2Event.DownedInvasionT2),
            ["OldOnesArmyT3"] = new(GetKey("Splits.OldOnesArmyT3"), SpeedrunAsset("OldOnesArmy"), () => Extended() && DD2Event.DownedInvasionT3),

            ["CraftZenith"] = new(GetKey("Splits.CraftZenith"), ItemSprite(ItemID.Zenith), () => Extended() && RunTracker.RunCategory == "CraftZenith" && Main.LocalPlayer.inventory.Any(i => i.stack > 0 && i.type == ItemID.Zenith)),
            ["CraftTerraBlade"] = new(GetKey("Splits.CraftTerraBlade"), ItemSprite(ItemID.TerraBlade), () => Extended() && RunTracker.RunCategory == "CraftTerraBlade" && Main.LocalPlayer.inventory.Any(i => i.stack > 0 && i.type == ItemID.TerraBlade)),
            ["AllEventsAndBosses"] = new(GetKey("Splits.AllEventsAndBosses"), SpeedrunAsset("AllBossesIcon"), () => Extended() &&
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
                NPC.downedSlimeKing &&
                // Events
                NPC.downedMartians &&
                DownedSystem.downedFrostMoon &&
                DownedSystem.downedBloodMoon &&
                DownedSystem.downedPumpkinMoon &&
                NPC.downedPirates &&
                DownedSystem.downedSolarEclipse &&
                NPC.downedFrost &&
                NPC.downedGoblins &&
                DownedSystem.downedSlimeRain &&
                DD2Event.DownedInvasionAnyDifficulty),

            ["TorchGod"] = new(GetKey("Splits.TorchGod"), ItemSprite(ItemID.TorchGodsFavor), () => Extended() && Main.LocalPlayer.unlockedBiomeTorches),
            ["OldOnesArmy"] = new(GetKey("Splits.OldOnesArmy"), SpeedrunAsset("OldOnesArmy"), () => Extended() && RunTracker.RunCategory == "OldOnesArmy" && DD2Event.DownedInvasionAnyDifficulty),
            ["AllAchievements"] = new(GetKey("Splits.AllAchievements"), SpeedrunAsset("AllBossesIcon"), () => Extended() && false), // TODO

            ["PlatinumCoin"] = new(GetKey("Splits.PlatinumCoin"), ItemSprite(ItemID.PlatinumCoin), () => Extended() && RunTracker.RunCategory == "PlatinumCoin" && Main.LocalPlayer.inventory.Any(i => i.stack > 0 && i.type == ItemID.PlatinumCoin)),
            ["DungeonGuardian"] = new(GetKey("Splits.DungeonGuardian"), BossHead(19), () => Extended() && DownedSystem.downedDungeonGuardian)
        });

        AllCategories.AddRange(new Dictionary<string, Category>()
        {
            ["MoonLord"] = new(GetKey("Categories.MoonLord"), GetSplit("MoonLord")),
            ["AllBosses"] = new(GetKey("Categories.AllBosses"), GetSplit("AllBosses")),
            ["AllPreHardmodeBosses"] = new(GetKey("Categories.AllPreHardmodeBosses"), GetSplit("AllPreHardmodeBosses")),
            ["NightsEdge"] = new(GetKey("Categories.NightsEdge"), GetSplit("CraftNightsEdge"))

            // Extended categories are not included in the dictionary by default
            // to prevent cluttering. They are added/removed in the config class.
        });

        SpeedrunConfig.Instance.OnChanged();
    }

    public static Dictionary<string, Category> ExtendedCategories => new()
    {
        ["CraftZenith"] = new(GetKey("Categories.CraftZenith"), GetSplit("CraftZenith")),
        ["CraftTerraBlade"] = new(GetKey("Categories.CraftTerraBlade"), GetSplit("CraftTerraBlade")),
        ["AllEventsAndBosses"] = new(GetKey("Categories.AllEventsAndBosses"), GetSplit("AllEventsAndBosses")),
        ["TorchGod"] = new(GetKey("Categories.TorchGod"), GetSplit("TorchGod")),
        ["OldOnesArmy"] = new(GetKey("Categories.OldOnesArmy"), GetSplit("OldOnesArmy")),
        ["AllAchievements"] = new(GetKey("Categories.AllAchievements"), GetSplit("AllAchievements")),
        ["PlatinumCoin"] = new(GetKey("Categories.PlatinumCoin"), GetSplit("PlatinumCoin")),
        ["DungeonGuardian"] = new(GetKey("Categories.DungeonGuardian"), GetSplit("DungeonGuardian")),
        ["GoblinArmy"] = new(GetKey("Categories.GoblinArmy"), GetSplit("GoblinArmy"))
    };

    /// <summary>
    /// Retrieves a split via the key it was registered with.
    /// </summary>
    public static Split GetSplit(string splitKey) => AllSplits[splitKey];

    /// <summary>
    /// Retrieves a category via the key it was registered with.
    /// </summary>
    public static Category GetCategory(string categoryKey) => AllCategories[categoryKey];

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
            // API
            "getsplit" => GetSplit((args[1] as string)!),
            "getcategory" => GetCategory((args[1] as string)!),
            "addsplit" => AddSplit((args[1] as string)!, (args[2] as string)!, (args[3] as Asset<Texture2D>)!, (args[4] as Func<bool>)!),
            "addcategory" => AddCategory((args[1] as string)!, (args[2] as string)!, (args[3] as Split)!),

            // Info
            "runactive" => RunTracker.RunActive,
            "runcategory" => RunTracker.RunCategory,
            "currentsplits" => RunTracker.CurrentSplits.Select(s => (CastRunSplit)s).ToArray(), // using .Cast<T>() is being weird here for some reason
            "lastcompletedrun" => RunTracker.LastCompletedRun is null ? null : (CastCompletedRun)RunTracker.LastCompletedRun.Value,

            _ => throw new NotImplementedException($"Did not recognize {args[0]} as a valid mod call!")
        };
    }
}
