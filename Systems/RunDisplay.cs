using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace SpeedrunTimer.Systems
{
    public class RunDisplay : ModSystem
    {
        public override void OnModLoad()
        {
            On_Main.DrawMenu += DrawMenuRunButtons;
        }

        private void DrawMenuRunButtons(On_Main.orig_DrawMenu orig, Main self, Microsoft.Xna.Framework.GameTime gameTime)
        {
            orig(self, gameTime);
            // TODO: Draw run start/cancel buttons
        }
    }
}
