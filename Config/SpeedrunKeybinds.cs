using SpeedrunTimer.Systems;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace SpeedrunTimer.Config;

public class SpeedrunKeybinds : ModSystem
{
    public static ModKeybind ToggleSpeedrunUI { get; private set; }

    public override void Load() => ToggleSpeedrunUI = KeybindLoader.RegisterKeybind(Mod, "ToggleSpeedrunUI", Microsoft.Xna.Framework.Input.Keys.None);

    public override void Unload() => ToggleSpeedrunUI = null;
}

public class SpeedrunKeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!SpeedrunKeybinds.ToggleSpeedrunUI.JustPressed)
            return;

        RunDisplay.DisplayTimer = !RunDisplay.DisplayTimer;
    }
}
