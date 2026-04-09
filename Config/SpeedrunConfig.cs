using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SpeedrunDisplay.Config;

public class SpeedrunConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [JsonIgnore]
    private static SpeedrunConfig _configCache = null;

    [JsonIgnore]
    public static SpeedrunConfig Instance
    {
        get
        {
            var conf = ModContent.GetInstance<SpeedrunConfig>();
            
            if (conf is not null)
                _configCache = conf;

            return _configCache;
        }
    }

    [JsonIgnore]
    public Vector2 SpeedrunUIPos => new(SpeedrunUIPosX, SpeedrunUIPosY);

    [Header("MainConfig")]
    [DefaultValue(6)]
    public int SplitsToShow { get; set; }

    [DefaultValue("Any%")]
    [CustomModConfigItem(typeof(RunCategoryStringSelectionElement))]
    public string DefaultRunCategory { get; set; }

    [DefaultValue(false)]
    public bool AutoRestart { get; set; }

    [DefaultValue(false)]
    public bool ShowOnTop { get; set; }

    [Header("DisplayConfig")]
    [DefaultValue(true)]
    public bool LockSpeedrunUIPos { get; set; }

    [DefaultValue(1f)]
    public float SpeedrunUIScale { get; set; }

    [DefaultValue(0.9f)]
    public float SpeedrunUIPosX { get; set; }

    [DefaultValue(0.1f)]
    public float SpeedrunUIPosY { get; set; }
}
