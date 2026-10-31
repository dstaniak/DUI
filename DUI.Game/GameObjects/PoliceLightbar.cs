using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Pixel roof lights drawn in car-local coordinates; owns its drawing texture.</summary>
internal sealed class PoliceLightbar : IDisposable
{
    private static readonly Vector2 s_roofCenter = new(-6f, 0f);
    private readonly Texture2D _pixel;

    public PoliceLightbar(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 center, float rotation,
        float scale, TimeSpan renderTime)
    {
        // RenderTime belongs to the movement model: pause and game over freeze
        // the lights, while retry resets them. There are exactly two frames.
        const long frameTicks = 250 * TimeSpan.TicksPerMillisecond;
        bool blueFrame = renderTime.Ticks / frameTicks % 2 == 0;
        // Saturated, opaque blocks keep the two frames distinct at gameplay size.
        // Pale palette colors and large white cores made the old lamps look static.
        Color lightColor = blueFrame ? new(32, 104, 255) : new(255, 48, 48);
        Vector2 forward = new(MathF.Cos(rotation), MathF.Sin(rotation));
        Vector2 side = new(-forward.Y, forward.X);

        DrawBlock(s_roofCenter, new(10, 28), C64Palette.Black);
        DrawBlock(s_roofCenter, new(8, 26), new(68, 68, 68));
        DrawLamp(-7f);
        DrawLamp(7f);

        void DrawLamp(float offsetY)
        {
            Vector2 position = s_roofCenter + new Vector2(0, offsetY);
            DrawBlock(position, new(8, 12), lightColor);
            DrawBlock(position + new Vector2(-2, -3), new(2, 2), C64Palette.White);
        }

        void DrawBlock(Vector2 localPosition, Vector2 size, Color color)
        {
            Vector2 position = center + (forward * localPosition.X + side * localPosition.Y) * scale;
            spriteBatch.Draw(_pixel, position, null, color, rotation,
                new Vector2(0.5f), size * scale, SpriteEffects.None, 0);
        }
    }

    public void Dispose()
    {
        _pixel.Dispose();
    }
}
