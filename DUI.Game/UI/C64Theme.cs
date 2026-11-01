using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace DUI.UI;

/// <summary>Shared C64 font, flat blue surfaces and square borders for all UI.</summary>
internal static class C64Theme
{
    public const string FontFile = "Fonts/c64.fnt";

    public static void ApplyFont(TextRuntime text, float scale = 1f)
    {
        text.UseCustomFont = true;
        text.CustomFontFile = FontFile;
        text.FontScale = scale;
        text.Color = C64Palette.White;
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
        RectangleRuntime border = CreateSurface(C64Palette.LightBlue);
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.Width = 0;
        border.Height = 0;
        border.Anchor(Anchor.TopLeft);
        parent.AddChild(border);

        RectangleRuntime background = CreateSurface(C64Palette.Blue);
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
