using SpeedrunDisplay.Systems;
using System.Reflection;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace SpeedrunDisplay.Config;

public class SpeedrunKeybinds : ModSystem
{
    public static ModKeybind ToggleSpeedrunUI { get; private set; } = null;

    public static ModKeybind ToggleSplitDisplay { get; private set; } = null;

    private static bool _loaded = false;
    public static bool Loaded => _loaded || (_loaded = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus.ContainsKey(ToggleSpeedrunUI.FullName()));

    public override void Load()
    {
        ToggleSpeedrunUI = KeybindLoader.RegisterKeybind(Mod, "ToggleSpeedrunUI", Microsoft.Xna.Framework.Input.Keys.None);
        ToggleSplitDisplay = KeybindLoader.RegisterKeybind(Mod, "ToggleSplitDisplay", Microsoft.Xna.Framework.Input.Keys.None);
    }
}

public class SpeedrunKeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        
    }
}
