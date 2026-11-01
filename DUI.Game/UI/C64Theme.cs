using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace DUI.UI;

/// <summary>Shared C64 font, color roles and square borders for all UI.</summary>
internal static class C64Theme
{
    public const string FontFile = "Fonts/c64.fnt";

    // Change this set to recolor the entire UI when it is next created.
    public static Color BackgroundColor => C64Palette.Orange;
    public static Color TextColor => C64Palette.Yellow;
    public static Color AccentColor => C64Palette.Yellow;
    public static Color PressedColor => C64Palette.White;
    public static Color MutedColor => C64Palette.LightGray;
    public static Color SymbolColor => C64Palette.White;

    public static void ApplyFont(TextRuntime text, float scale = 1f)
    {
        text.UseCustomFont = true;
        text.CustomFontFile = FontFile;
        text.FontScale = scale;
        text.Color = TextColor;
    }

    public static TextRuntime CreateText(string value, float scale = 1f)
    {
        var text = new TextRuntime
        {
            Text = value,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren
        };
        ApplyFont(text, scale);
        return text;
    }

    public static RectangleRuntime CreateSurface(Color color)
    {
        return new RectangleRuntime
        {
            IsFilled = true,
            FillColor = color,
            StrokeWidth = 0,
            IsAntialiased = false
        };
    }

    public static RectangleRuntime AddFrame(GraphicalUiElement parent, float borderWidth)
    {
        RectangleRuntime border = CreateSurface(AccentColor);
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.Width = 0;
        border.Height = 0;
        border.Anchor(Anchor.TopLeft);
        parent.AddChild(border);

        RectangleRuntime background = CreateSurface(BackgroundColor);
        background.WidthUnits = DimensionUnitType.RelativeToParent;
        background.HeightUnits = DimensionUnitType.RelativeToParent;
        background.Anchor(Anchor.TopLeft);
        background.X = borderWidth;
        background.Y = borderWidth;
        background.Width = -2 * borderWidth;
        background.Height = -2 * borderWidth;
        parent.AddChild(background);
        return background;
    }
}
