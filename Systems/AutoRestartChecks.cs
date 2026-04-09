using SpeedrunTimer.Config;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace SpeedrunTimer.Systems;

public class AutoRestartCheck : ModPlayer
{
    public static string LastLoadedPlayerPath { get; private set; } = null;

    public static string LastLoadedWorldPath { get; private set; } = null;

    internal static readonly string AutoRestartInfoPath = Path.Combine(Main.SavePath, "SpeedrunTimer", "AutoRestartInfo.txt");

    public override void Load()
    {
        Main.instance.Exiting += (_, _) => TrySaveAutoRestartInfo();

        if (!File.Exists(AutoRestartInfoPath))
            return;

        string text = File.ReadAllText(AutoRestartInfoPath);
        string[] lastLoaded = text.Split('\n');

        if (lastLoaded.Length != 2)
            return;

        LastLoadedPlayerPath = lastLoaded[0];
        LastLoadedWorldPath = lastLoaded[1];
    }

    public override void OnEnterWorld()
    {
        // We read file paths instead of names or other arbitrary data from something like TagCompounds
        string playerPath = Main.ActivePlayerFileData.Path;
        string worldPath = Main.ActiveWorldFileData.Path;

        // This should only happen on first world entry
        LastLoadedPlayerPath ??= playerPath;
        LastLoadedWorldPath ??= worldPath;

        // Reset timer on a new player AND new world
        if (SpeedrunConfig.Instance.AutoRestart && LastLoadedPlayerPath != playerPath && LastLoadedWorldPath != worldPath)
        {
            RunTracker.CancelRun();
            RunTracker.StartRun(SpeedrunConfig.Instance.DefaultRunCategory);
        }

        LastLoadedPlayerPath = playerPath;
        LastLoadedWorldPath = worldPath;
    }

    internal static void TrySaveAutoRestartInfo()
    {
        if (!SpeedrunConfig.Instance.AutoRestart)
        {
            try { File.Delete(AutoRestartInfoPath); } catch { }
            return;
        }

        string text = string.Join('\n', LastLoadedPlayerPath, LastLoadedWorldPath);
        File.WriteAllText(AutoRestartInfoPath, text);
    }
}