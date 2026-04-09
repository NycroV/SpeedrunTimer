using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SpeedrunTimer.Utils;

public enum TextAlignment
{
    Left,
    Middle,
    Right
}

public static partial class SpeedrunUtil
{
    public static Texture2D MagicPixel { get; set; } = ModContent.Request<Texture2D>("Terraria/Images/MagicPixel", AssetRequestMode.ImmediateLoad).Value;

    private static readonly FieldInfo customEffectField = typeof(SpriteBatch).GetField("customEffect", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo transformMatrixField = typeof(SpriteBatch).GetField("transformMatrix", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void GetDrawParameters(this SpriteBatch spriteBatch,
        out BlendState blendState,
        out SamplerState samplerState,
        out DepthStencilState depthStencilState,
        out RasterizerState rasterizerState,
        out Effect effect,
        out Matrix matrix)
    {
        blendState = spriteBatch.GraphicsDevice.BlendState;
        samplerState = spriteBatch.GraphicsDevice.SamplerStates[0];
        depthStencilState = spriteBatch.GraphicsDevice.DepthStencilState;
        rasterizerState = spriteBatch.GraphicsDevice.RasterizerState;
        effect = customEffectField.GetValue(spriteBatch) as Effect;
        matrix = (Matrix)transformMatrixField.GetValue(spriteBatch);
    }

    /// <summary>
    /// Centers a rectangle on a give point.
    /// </summary>
    public static Rectangle CenteredRectangle(Vector2 center, Vector2 size)
    {
        size = new(Math.Abs(size.X), Math.Abs(size.Y));
        return new((int)center.X - ((int)size.X / 2), (int)center.Y - ((int)size.Y / 2), (int)size.X, (int)size.Y);
    }

    /// <summary>
    /// Draws a simple rectangle to the spritebatch.
    /// </summary>
    public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, float stroke = 2f, bool fill = false)
    {
        Texture2D pixel = MagicPixel;

        if (fill)
        {
            spriteBatch.Draw(pixel, rectangle, color);
            return;
        }

        int halfStroke = (int)Math.Ceiling(stroke * 0.5f);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Left - halfStroke, rectangle.Top - halfStroke, rectangle.Width + (int)stroke, (int)stroke), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Left - halfStroke, rectangle.Top - halfStroke, (int)stroke, rectangle.Height + (int)stroke), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Left - halfStroke, rectangle.Bottom - halfStroke, rectangle.Width + (int)stroke, (int)stroke), color);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - halfStroke, rectangle.Top - halfStroke, (int)stroke, rectangle.Height + (int)stroke), color);
    }

    /// <summary>
    /// Creates a new rectangle from a "cookie cutter" slice of another rectangle.<br/>
    /// <br/>
    /// <paramref name="center"/> is the position within the given rectangle that you want to center your new rectangle. <c>-1f</c> for the top/left, <c>1f</c> for the bottom/right.<br/>
    /// <paramref name="size"/> is the scale of your new rectangle, based on the size of the given rectangle. <c>1f</c> is the same size as the given rectangle, <c>0.5f</c> is half the size.<br/>
    /// <br/>
    /// Both <paramref name="center"/> and <paramref name="size"/> can be beyond the bounds of the given rectangle (ie. <paramref name="center"/> can be greater/less than <c>1f</c>/<c>-1f</c>, <paramref name="size"/> can be greater/less than <c>1f</c>/<c>0f</c>.
    /// </summary>
    public static Rectangle CookieCutter(this Rectangle rectangle, Vector2 center, Vector2 size)
    {
        Vector2 cookieCenter = rectangle.Center.ToVector2() + (center * rectangle.Size() * 0.5f);
        return CenteredRectangle(cookieCenter, size * rectangle.Size());
    }

    public static List<(string line, Vector2 drawPos, Vector2 origin, float scale)> GetRectangleStringParameters(
        Rectangle rectangle,
        SpriteFont font,
        string text,
        float? minimumScale = null,
        float? maxScale = null,
        float? extraScale = null,
        TextAlignment alignment = TextAlignment.Middle,
        Vector2? offset = null,
        float verticalSpacing = 0)
    {
        string[] strings = text.Split('\n');
        List<(string line, Vector2 drawPos, Vector2 origin, float scale)> results = [];

        if (strings.Length == 0)
            return results;

        float availableSpace = rectangle.Height - ((strings.Length - 1) * verticalSpacing);
        float lineHeight = availableSpace / strings.Length;

        for (int i = 0; i < strings.Length; i++)
        {
            string line = strings[i];
            Rectangle designatedArea = new(rectangle.X, (int)(rectangle.Y + (i * (verticalSpacing + lineHeight))), rectangle.Width, (int)lineHeight);

            Vector2 textSize = font.MeasureString(line);
            Vector2 ratio = designatedArea.Size() / textSize;

            float scale = Math.Min(ratio.X, ratio.Y);
            if (minimumScale.HasValue)
                scale = Math.Max(scale, minimumScale.Value);

            if (maxScale.HasValue)
                scale = Math.Min(scale, maxScale.Value);

            Vector2 origin = alignment switch
            {
                TextAlignment.Left => new Vector2(0, textSize.Y * 0.5f),
                TextAlignment.Middle => textSize * 0.5f,
                TextAlignment.Right => new Vector2(textSize.X, textSize.Y * 0.5f),
                _ => textSize * 0.5f
            };

            Vector2 drawPos = alignment switch
            {
                TextAlignment.Left => new(designatedArea.Left, designatedArea.Center().Y),
                TextAlignment.Middle => designatedArea.Center(),
                TextAlignment.Right => new(designatedArea.Right, designatedArea.Center().Y),
                _ => designatedArea.Center()

            };

            if (offset.HasValue)
                drawPos += offset.Value;

            if (extraScale.HasValue)
                scale *= extraScale.Value;

            results.Add(new(line, drawPos, origin, scale));
        }

        return results;
    }

    public static void DrawOutlinedStringInRectangle(this SpriteBatch spriteBatch,
        Rectangle rectangle,
        SpriteFont font,
        Color color,
        Color outlineColor,
        string text,
        float stroke = 2f,
        float? minimumScale = null,
        float? maxScale = null,
        float extraScale = 1f,
        TextAlignment alignment = TextAlignment.Middle,
        Vector2? offset = null,
        float verticalSpacing = 0,
        bool clipBounds = true)
    {
        var results = GetRectangleStringParameters(rectangle.CreateMargin((int)(stroke * 0.5f)), font, text, minimumScale, maxScale, extraScale, alignment, offset, verticalSpacing);

        bool cachedClip = spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable;
        Rectangle cachedClipArea = spriteBatch.GraphicsDevice.ScissorRectangle;

        if (clipBounds)
        {
            spriteBatch.End();
            spriteBatch.GetDrawParameters(out var blend, out var sampler, out var depth, out var raster, out var effect, out var matrix);

            raster.ScissorTestEnable = true;
            matrix.Decompose(out var scale, out _, out var translation);

            rectangle = new(
                (int)(rectangle.X * scale.X + translation.X),
                (int)(rectangle.Y * scale.Y + translation.Y),
                (int)(rectangle.Width * scale.X),
                (int)(rectangle.Height * scale.Y));

            spriteBatch.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(cachedClipArea, rectangle);

            spriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, raster, effect, matrix);
        }

        if (spriteBatch.GraphicsDevice.ScissorRectangle != Rectangle.Empty)
            foreach (var (line, drawPos, origin, scale) in results)
                spriteBatch.DrawOutlinedString(font, line, drawPos, origin, scale, stroke, outlineColor, color);

        if (clipBounds)
        {
            spriteBatch.End();
            spriteBatch.GetDrawParameters(out var blend, out var sampler, out var depth, out var raster, out var effect, out var matrix);

            raster.ScissorTestEnable = cachedClip;
            spriteBatch.GraphicsDevice.ScissorRectangle = cachedClipArea;

            spriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, raster, effect, matrix);
        }
    }

    public static void DrawStringInRectangle(this SpriteBatch spriteBatch,
        Rectangle rectangle,
        SpriteFont font,
        Color color,
        string text,
        float? minimumScale = null,
        float? maxScale = null,
        float extraScale = 1f,
        TextAlignment alignment = TextAlignment.Middle,
        Vector2? offset = null,
        float verticalSpacing = 0,
        bool clipBounds = true)
    {
        var results = GetRectangleStringParameters(rectangle, font, text, minimumScale, maxScale, extraScale, alignment, offset, verticalSpacing);

        bool cachedClip = spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable;
        Rectangle cachedClipArea = spriteBatch.GraphicsDevice.ScissorRectangle;

        if (clipBounds)
        {
            spriteBatch.End();
            spriteBatch.GetDrawParameters(out var blend, out var sampler, out var depth, out var raster, out var effect, out var matrix);

            raster.ScissorTestEnable = true;
            matrix.Decompose(out var scale, out _, out var translation);

            rectangle = new(
                (int)(rectangle.X * scale.X + translation.X),
                (int)(rectangle.Y * scale.Y + translation.Y),
                (int)(rectangle.Width * scale.X),
                (int)(rectangle.Height * scale.Y));

            spriteBatch.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(cachedClipArea, rectangle);

            spriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, raster, effect, matrix);
        }

        if (spriteBatch.GraphicsDevice.ScissorRectangle != Rectangle.Empty)
            foreach (var (line, drawPos, origin, scale) in results)
                spriteBatch.DrawString(font, line, drawPos, color, 0f, origin, scale, SpriteEffects.None, 0f);

        if (clipBounds)
        {
            spriteBatch.End();
            spriteBatch.GetDrawParameters(out var blend, out var sampler, out var depth, out var raster, out var effect, out var matrix);

            raster.ScissorTestEnable = cachedClip;
            spriteBatch.GraphicsDevice.ScissorRectangle = cachedClipArea;

            spriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, depth, raster, effect, matrix);
        }
    }

    public static void DrawOutlinedString(this SpriteBatch spriteBatch,
       SpriteFont font,
       string line,
       Vector2 drawPos,
       Vector2 origin,
       float scale,
       float stroke = 2f,
       Color? outlineColor = null,
       Color? textColor = null)
    {
        Color strokeColor = outlineColor ?? Color.Black;
        Color color = textColor ?? Color.White;

        spriteBatch.DrawString(font, line, drawPos + new Vector2(-stroke, 0f), strokeColor, 0f, origin, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, line, drawPos + new Vector2(stroke, 0f), strokeColor, 0f, origin, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, line, drawPos + new Vector2(0f, stroke), strokeColor, 0f, origin, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, line, drawPos + new Vector2(0f, -stroke), strokeColor, 0f, origin, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, line, drawPos, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Scales a rectangle around its center.
    /// </summary>
    public static Rectangle Scale(this Rectangle rectangle, float scale)
    {
        return CenteredRectangle(rectangle.Center(), rectangle.Size() * scale);
    }

    /// <summary>
    /// Scales a rectangle around its center.
    /// </summary>
    public static Rectangle Scale(this Rectangle rectangle, Vector2 scale)
    {
        return CenteredRectangle(rectangle.Center(), rectangle.Size() * scale);
    }

    /// <summary>
    /// Creates non-scaled margins at the edges of a rectangle.<br/>
    /// Positive margins make the rectangle smaller, negative margins make it bigger.<br/>
    /// This margin will be subtracted from each side of the rectangle, not just the width/height.
    /// </summary>
    public static Rectangle CreateMargin(this Rectangle rectangle, int margin)
    {
        return CenteredRectangle(rectangle.Center(), new(rectangle.Width - (margin * 2), rectangle.Height - (margin * 2)));
    }

    /// <summary>
    /// Creates non-scaled margins at the edges of a rectangle.<br/>
    /// Positive margins make the rectangle smaller, negative margins make it bigger.
    /// </summary>
    public static Rectangle CreateMargins(this Rectangle rectangle, int left = 0, int right = 0, int top = 0, int bottom = 0)
    {
        return new Rectangle(rectangle.Left + left, rectangle.Top + top, rectangle.Width - left - right, rectangle.Height - top - bottom);
    }

    /// <summary>
    /// Creates scaled margins at the edge of a rectangle.<br/>
    /// Positive margins make the rectangle smaller, negative margins make it bigger.<br/>
    /// This margin will be subtracted from each side of the rectangle, not just the width/height.
    /// </summary>
    public static Rectangle CreateScaledMargin(this Rectangle rectangle, float margin)
    {
        return rectangle.Scale(1f - (margin * 2f));
    }

    /// <summary>
    /// Creates scaled margins at the edges of a rectangle.<br/>
    /// Positive margins make the rectangle smaller, negative margins make it bigger.
    /// </summary>
    public static Rectangle CreateScaledMargins(this Rectangle rectangle, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        return rectangle.CreateMargins((int)(left * rectangle.Width), (int)(right * rectangle.Width), (int)(top * rectangle.Height), (int)(bottom * rectangle.Height));
    }

    /// <summary>
    /// Nice TimeSpan formatting.
    /// </summary>
    public static string Format(this TimeSpan timeSpan, bool fractionalSeconds) => $"{(timeSpan.Hours > 0 ? $"{(int)timeSpan.TotalHours}:" : "")}{(timeSpan.Hours > 0 ? timeSpan.ToString("mm") : timeSpan.ToString("%m"))}:{timeSpan.ToString($"ss{(fractionalSeconds ? "\\.fff" : "")}")}";

    /// <summary>
    /// Shorthand <see cref="Language.GetTextValue(string)"/>
    /// </summary>
    public static string Fetch(this string localizationKey) => Language.GetTextValue(localizationKey);
}
