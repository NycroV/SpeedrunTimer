using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SpeedrunDisplay.Systems
{
    public class DownedSystem : ModSystem
    {
        public static bool downedSlimeRain = false;
        public static bool downedBloodMoon = false;
        public static bool downedFrostMoon = false;
        public static bool downedPumpkinMoon = false;
        public static bool downedSolarEclipse = false;

        public static bool downedDungeonGuardian = false;
        public static bool downedEaterOfWorlds = false;
        public static bool downedBrainOfCthulhu = false;

        private static bool cachedSlimeRain = false;
        private static bool cachedBloodMoon = false;
        private static bool cachedFrostMoon = false;
        private static bool cachedPumpkinMoon = false;
        private static bool cachedSolarEclipse = false;

        #region State Saving

        private static IEnumerable<FieldInfo> DownedFields { get; } = typeof(DownedSystem).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(bool));
        private static IEnumerable<FieldInfo> CachedFields { get; } = typeof(DownedSystem).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(f => f.FieldType == typeof(bool));

        public override void ClearWorld()
        {
            foreach (var field in DownedFields.Concat(CachedFields))
                field.SetValue(null, false);
        }

        public override void SaveWorldData(TagCompound tag)
        {
            foreach (var field in DownedFields)
                tag[field.Name] = field.GetValue(null);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            foreach (var field in DownedFields)
            {
                if (tag.TryGet(field.Name, out bool downed))
                    field.SetValue(null, downed);
            }
        }

        #endregion

        #region Updating

        private static void UpdateEvent(ref bool downed, ref bool cached, bool state)
        {
            if (cached && !state)
                downed = true;

            cached = state;
        }

        public override void PostUpdateTime()
        {
            UpdateEvent(ref downedSlimeRain, ref cachedSlimeRain, Main.slimeRain);
            UpdateEvent(ref downedBloodMoon, ref cachedBloodMoon, Main.bloodMoon);
            UpdateEvent(ref downedFrostMoon, ref cachedFrostMoon, Main.snowMoon);
            UpdateEvent(ref downedPumpkinMoon, ref cachedPumpkinMoon, Main.pumpkinMoon);
            UpdateEvent(ref downedSolarEclipse, ref cachedSolarEclipse, Main.eclipse);
        }

        #endregion
    }

    public class DownedNPC : GlobalNPC
    {
        private static void UpdateNPC(ref bool downed, int npcType, NPC npc)
        {
            if (npc.type == npcType)
                downed = true;
        }

        public override void OnKill(NPC npc)
        {
            UpdateNPC(ref DownedSystem.downedDungeonGuardian, NPCID.DungeonGuardian, npc);
            UpdateNPC(ref DownedSystem.downedBrainOfCthulhu, NPCID.BrainofCthulhu, npc);

            // EoW nonsense
            if (npc.boss)
            {
                UpdateNPC(ref DownedSystem.downedEaterOfWorlds, NPCID.EaterofWorldsHead, npc);
                UpdateNPC(ref DownedSystem.downedEaterOfWorlds, NPCID.EaterofWorldsBody, npc);
                UpdateNPC(ref DownedSystem.downedEaterOfWorlds, NPCID.EaterofWorldsTail, npc);
            }
        }
    }
}
