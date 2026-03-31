using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using ReLogic.Content;
using SpeedrunTimer.Config;
using SpeedrunTimer.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace SpeedrunTimer.Systems;

public class RunDisplay : ModSystem
{
    public static bool DisplayTimer { get; set; } = true;

    public static SpriteFont JetbrainsMono { get; set; } = ModContent.Request<SpriteFont>("SpeedrunTimer/Assets/Fonts/JetbrainsMono", AssetRequestMode.ImmediateLoad).Value;
    public static Texture2D Arrow { get; set; } = ModContent.Request<Texture2D>("SpeedrunTimer/Assets/Textures/Arrow", AssetRequestMode.ImmediateLoad).Value;

    private static bool movingTimer = false;
    private static Point cachedMousePos = Point.Zero;
    private static bool cachedClick = false;

    private static bool cancellingRun = false;
    private static bool startingRun = false;
    private static int startingRunType = 0;

    private static string UIText(string localization) => Language.GetTextValue("Mods.SpeedrunTimer.UI." + localization);

    public override void OnModLoad() => IL_Main.DrawMenu += IL_Main_DrawMenu;

    private void IL_Main_DrawMenu(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            var drawCursorMethod = typeof(Main).GetMethod(nameof(Main.DrawCursor), BindingFlags.Static | BindingFlags.Public, [typeof(Vector2), typeof(bool)]);
            c.GotoNext(i => i.MatchCall(drawCursorMethod));

            var spriteBatchEndMethod = typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.End), BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
            c.GotoPrev(i => i.MatchCallvirt(spriteBatchEndMethod));

            c.EmitDelegate(DrawMenuRunButtons);
        }
        catch
        {
            MonoModHooks.DumpIL(ModContent.GetInstance<SpeedrunTimer>(), il);
        }
    }

    private static void DrawMenuRunButtons()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin();

        var mouseState = Mouse.GetState();
        Vector2 screenSize = new(Main.graphics.PreferredBackBufferWidth, Main.graphics.PreferredBackBufferHeight);
        Point mousePos = new(mouseState.X, mouseState.Y);

        bool mouseClicked = cachedClick && mouseState.LeftButton == ButtonState.Released && Main.hasFocus;
        cachedClick = mouseState.LeftButton == ButtonState.Pressed;

        Rectangle timerUI = DrawSpeedrunTimer(Main.spriteBatch, screenSize, mousePos);
        bool buttonsAbove = timerUI.Center().Y > screenSize.Y * 0.5f;

        if (cancellingRun)
        {
            Rectangle warning = timerUI.CookieCutter(new(0f, buttonsAbove ? -1.65f : 1.25f), new(1.2f, 0.2f));
            Main.spriteBatch.DrawOutlinedStringInRectangle(warning, JetbrainsMono, Color.White, Color.Black, UIText("ConfirmCancelRun"));

            Rectangle confirmCancel = timerUI.CookieCutter(new(-0.55f, buttonsAbove ? -1.25f : 1.65f), new(0.35f, 0.12f));
            Rectangle cancelCancel = timerUI.CookieCutter(new(0.55f, buttonsAbove ? -1.25f : 1.65f), new(0.35f, 0.12f));

            bool confirmHovered = confirmCancel.Contains(mousePos);
            bool cancelHovered = cancelCancel.Contains(mousePos);

            Main.spriteBatch.DrawRectangle(confirmCancel, confirmHovered ? Color.Yellow : Color.LightGray);
            Main.spriteBatch.DrawRectangle(cancelCancel, cancelHovered ? Color.Yellow : Color.LightGray);

            Main.spriteBatch.DrawOutlinedStringInRectangle(confirmCancel, JetbrainsMono, confirmHovered ? Color.Yellow : Color.White, Color.Black, UIText("Yes"));
            Main.spriteBatch.DrawOutlinedStringInRectangle(cancelCancel, JetbrainsMono, cancelHovered ? Color.Yellow : Color.White, Color.Black, UIText("No"));

            if (!mouseClicked)
                return;

            if (confirmHovered)
            {
                RunTracker.CancelRun();
                cancellingRun = false;
            }

            else if (cancelHovered)
                cancellingRun = false;

            return;
        }

        if (RunTracker.RunActive)
        {
            Rectangle cancelRun = timerUI.CookieCutter(new(0f, buttonsAbove ? -1.25f : 1.25f), new(1f, 0.125f));
            bool cancelHovered = cancelRun.Contains(mousePos);

            Main.spriteBatch.DrawRectangle(cancelRun, cancelHovered ? Color.Yellow : Color.LightGray);
            Main.spriteBatch.DrawOutlinedStringInRectangle(cancelRun, JetbrainsMono, cancelHovered ? Color.Yellow : Color.White, Color.Black, UIText("CancelRun"));

            if (cancelHovered && mouseClicked)
                cancellingRun = true;

            return;
        }

        if (startingRun)
        {
            Rectangle runType = timerUI.CookieCutter(new(0f, buttonsAbove ? -1.8f : 1.2f), new(1f, 0.125f));
            Rectangle startSelectedRun = runType.CookieCutter(new(0f, 2.5f), new(0.6f, 1f));
            Rectangle cancelStartRun = startSelectedRun.CookieCutter(new(0f, 2.6f), new(1f, 0.8f));

            Rectangle previousType = runType.CookieCutter(new(-0.8f, 2.5f), new(0.1f, 0.5f));
            Rectangle nextType = runType.CookieCutter(new(0.8f, 2.5f), new(0.1f, 0.5f));

            bool startSelectedHovered = startSelectedRun.Contains(mousePos);
            bool cancelStartHovered = cancelStartRun.Contains(mousePos);
            bool previousHovered = previousType.Contains(mousePos);
            bool nextHovered = nextType.Contains(mousePos);

            string runCategory = SpeedrunTimer.AllCategories.ElementAt(startingRunType).Key;
            Main.spriteBatch.DrawOutlinedStringInRectangle(runType, JetbrainsMono, Color.White, Color.Black, Language.GetTextValue(SpeedrunTimer.AllCategories[runCategory].LocalizationKey));

            Main.spriteBatch.DrawRectangle(startSelectedRun, startSelectedHovered ? Color.Yellow : Color.LightGray);
            Main.spriteBatch.DrawOutlinedStringInRectangle(startSelectedRun, JetbrainsMono, startSelectedHovered ? Color.Yellow : Color.White, Color.Black, UIText("Start"));

            Main.spriteBatch.DrawRectangle(cancelStartRun, cancelStartHovered ? Color.Yellow : Color.LightGray);
            Main.spriteBatch.DrawOutlinedStringInRectangle(cancelStartRun, JetbrainsMono, cancelStartHovered ? Color.Yellow : Color.White, Color.Black, UIText("Cancel"));

            Vector2 origin = Arrow.Size() * 0.5f;
            Main.spriteBatch.Draw(Arrow, previousType.Center(), null, previousHovered ? Color.Yellow : Color.White, 0f, origin, SpeedrunConfig.Instance.SpeedrunUIScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Arrow, nextType.Center(), null, nextHovered ? Color.Yellow : Color.White, 0f, origin, SpeedrunConfig.Instance.SpeedrunUIScale, SpriteEffects.FlipHorizontally, 0f);

            if (!mouseClicked)
                return;

            if (startSelectedHovered)
            {
                RunTracker.StartRun(runCategory);
                startingRun = false;
            }

            else if (previousHovered)
            {
                startingRunType++;
                if (startingRunType >= SpeedrunTimer.AllCategories.Count)
                    startingRunType = 0;
            }

            else if (nextHovered)
            {
                startingRunType--;
                if (startingRunType < 0)
                    startingRunType = SpeedrunTimer.AllCategories.Count - 1;
            }

            else if (cancelStartHovered)
                startingRun = false;

            return;
        }

        Rectangle startNewRun = timerUI.CookieCutter(new(0f, buttonsAbove ? -1.25f : 1.25f), new(1f, 0.125f));
        bool startNewHovered = startNewRun.Contains(mousePos);

        Main.spriteBatch.DrawRectangle(startNewRun, startNewHovered ? Color.Yellow : Color.LightGray);
        Main.spriteBatch.DrawOutlinedStringInRectangle(startNewRun, JetbrainsMono, startNewHovered ? Color.Yellow : Color.White, Color.Black, UIText("NewRun"));

        if (mouseClicked && startNewHovered)
            startingRun = true;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!DisplayTimer || !RunTracker.RunActive)
            return;

        int layerIndex = SpeedrunConfig.Instance.ShowOnTop ? layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory")) : 0;

        if (layerIndex == -1)
            return;

        layers.Insert(layerIndex, new LegacyGameInterfaceLayer(
            "SpeedrunTimer: Timer Display", () => {
                DrawSpeedrunTimer(Main.spriteBatch, Main.ScreenSize.ToVector2(), new Point(Main.mouseX, Main.mouseY));
                return true;
            }, InterfaceScaleType.None));
    }

    public static Rectangle DrawSpeedrunTimer(SpriteBatch spriteBatch, Vector2 screenSize, Point mousePos)
    {
        Vector2 drawTopCenter = screenSize * SpeedrunConfig.Instance.SpeedrunUIPos;
        Vector2 drawSize = new Vector2(200f, 100f) * SpeedrunConfig.Instance.SpeedrunUIScale;
        int splits = SpeedrunConfig.Instance.SplitsToShow;

        Rectangle drawArea = CenteredRectangle(drawTopCenter + new Vector2(0f, drawSize.Y * 0.5f), drawSize);
        Vector2 splitSize = new(drawArea.Width, drawArea.Height * 0.375f);
        int splitsOffset = (int)float.Ceiling(splitSize.Y * splits);

        Rectangle titleBox = drawArea.CookieCutter(new(0f, -0.6f), new(0.95f, 0.3f));
        Rectangle timerBox = drawArea.CookieCutter(new(0f, 0.375f), new Vector2(1f, 0.595f));
        Rectangle igtBox = drawArea.CookieCutter(new(0f, 0.15f), new(0.96f, 0.4f));
        Rectangle rtaBox = drawArea.CookieCutter(new(0f, 0.7f), new(0.96f, 0.25f));

        drawArea.Height += splitsOffset;
        timerBox.Y += splitsOffset;
        igtBox.Y += splitsOffset;
        rtaBox.Y += splitsOffset;

        spriteBatch.DrawRectangle(drawArea, Color.Black * 0.45f, fill: true);
        spriteBatch.DrawRectangle(titleBox.Scale(new Vector2(1.05f, 1.25f)), Color.Black * 0.35f, fill: true);
        spriteBatch.DrawRectangle(timerBox, Color.Black * 0.35f, fill: true);

        string runTitle = RunTracker.RunActive ?
            Language.GetTextValue(SpeedrunTimer.AllCategories[RunTracker.RunCategory].LocalizationKey) :
            RunTracker.LastCompletedRun is not null ? Language.GetTextValue(RunTracker.LastCompletedRun.Value.Category.LocalizationKey) : "---";

        TimeSpan igtTime = RunTracker.RunActive ?
            TimeSpan.FromSeconds(RunTracker.IGT_FrameCounter / 60f) :
            RunTracker.LastCompletedRun?.IGT ?? TimeSpan.Zero;

        TimeSpan rtaTime = RunTracker.RunActive ?
            (RunTracker.IGT_FrameCounter > 0 ? DateTime.UtcNow - RunTracker.RTA_RunStart : TimeSpan.Zero) :
            RunTracker.LastCompletedRun?.RTA ?? TimeSpan.Zero;

        spriteBatch.DrawOutlinedStringInRectangle(titleBox, JetbrainsMono, Color.White, Color.Black, runTitle);

        string igt = $"{(igtTime.Hours > 0 ? $"{(int)igtTime.TotalHours}:" : "")}{(igtTime.Hours > 0 ? igtTime.ToString("mm") : igtTime.ToString("%m"))}:{igtTime:ss\\.fff}";
        spriteBatch.DrawOutlinedStringInRectangle(igtBox, JetbrainsMono, Color.White, Color.Black, igt, alignment: Utils.TextAlignment.Right);

        string rta = $"{(rtaTime.Hours > 0 ? $"{(int)rtaTime.TotalHours}:" : "")}{(rtaTime.Hours > 0 ? rtaTime.ToString("mm") : rtaTime.ToString("%m"))}:{rtaTime:ss}";
        spriteBatch.DrawOutlinedStringInRectangle(rtaBox, JetbrainsMono, Color.DarkGray, Color.Black, rta, alignment: Utils.TextAlignment.Right);

        if (splits == 0)
            goto MoveUI;

        void DrawSplit(Rectangle box, RunSplit? runSplit)
        {
            Rectangle iconArea = box.CookieCutter(new(-0.85f, 0f), new(0.17f, 1f));
            Rectangle textArea = box.CookieCutter(new(-0.14f, 0f), new(0.5f, 1f));
            Rectangle timeArea = box.CookieCutter(new(0.7f, 0f), new(0.3f, 1f));

            //spriteBatch.DrawRectangle(iconArea, Color.Red);
            //spriteBatch.DrawRectangle(textArea, Color.Yellow);
            //spriteBatch.DrawRectangle(timeArea, Color.Lime);

            if (!runSplit.HasValue)
            {
                spriteBatch.DrawOutlinedStringInRectangle(iconArea, JetbrainsMono, Color.DarkGray, Color.Black, "-", 1f);
                spriteBatch.DrawOutlinedStringInRectangle(timeArea, JetbrainsMono, Color.DarkGray, Color.Black, "-:-:-", 1f, alignment: Utils.TextAlignment.Right);
                return;
            }

            Split split = runSplit.Value.Split;
            Texture2D splitIcon = split.Icon.Value;
            float scale = float.Min((float)splitIcon.Width / iconArea.Width, (float)splitIcon.Height / iconArea.Height);

            string splitText = Language.GetTextValue(split.LocalizationKey);
            TimeSpan splitRunTime = TimeSpan.FromSeconds(runSplit.Value.RunTime / 60f);
            string splitTime = $"{(splitRunTime.Hours > 0 ? $"{(int)splitRunTime.TotalHours}:" : "")}{(splitRunTime.Hours > 0 ? splitRunTime.ToString("mm") : splitRunTime.ToString("%m"))}:{splitRunTime:ss}";

            spriteBatch.Draw(splitIcon, iconArea.Center.ToVector2(), null, Color.White, 0f, splitIcon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawOutlinedStringInRectangle(textArea, JetbrainsMono, Color.White, Color.Black, splitText, alignment: Utils.TextAlignment.Left);
            spriteBatch.DrawOutlinedStringInRectangle(timeArea, JetbrainsMono, Color.White, Color.Black, splitTime, 1f, alignment: Utils.TextAlignment.Right);
        }

        int splitCount = RunTracker.LastCompletedRun?.Splits.Count ?? RunTracker.CurrentSplits?.Count ?? 0;
        Rectangle splitBox = titleBox.CookieCutter(new(0f, 2.5f), Vector2.One);
        RunSplit? split = splitCount > 0 ? (RunTracker.LastCompletedRun?.Splits[^1] ?? RunTracker.CurrentSplits[^1]) : null;
        DrawSplit(splitBox, split);

        for (int i = 1; i < splits; i++)
        {
            splitBox = splitBox.CookieCutter(new(0f, 2.5f), Vector2.One);
            split = splitCount > i ? (RunTracker.LastCompletedRun?.Splits[^(i + 1)] ?? RunTracker.CurrentSplits[^(i + 1)]) : null;
            DrawSplit(splitBox, split);
        }

    MoveUI:

        if (SpeedrunConfig.Instance.LockSpeedrunUIPos)
            return drawArea;

        bool mousePressed = Mouse.GetState().LeftButton == ButtonState.Pressed && Main.hasFocus;

        if (!movingTimer && !drawArea.Contains(mousePos))
            return drawArea;

        if (Main.myPlayer >= 0 && Main.LocalPlayer is not null)
            Main.LocalPlayer.mouseInterface = true;

        if (!mousePressed)
        {
            movingTimer = false;
            SpeedrunConfig.Instance.SaveChanges();
            return drawArea;
        }

        if (!movingTimer)
        {
            movingTimer = true;
            cachedMousePos = mousePos;
            return drawArea;
        }

        Vector2 oldUiPos = cachedMousePos.ToVector2() / screenSize;
        Vector2 newUiPos = mousePos.ToVector2() / screenSize;

        Vector2 uiPosChange = newUiPos - oldUiPos;
        cachedMousePos = mousePos;

        if (uiPosChange == Vector2.Zero)
            return drawArea;

        SpeedrunConfig.Instance.SpeedrunUIPosX += uiPosChange.X;
        SpeedrunConfig.Instance.SpeedrunUIPosY += uiPosChange.Y;
        return drawArea;
    }

}