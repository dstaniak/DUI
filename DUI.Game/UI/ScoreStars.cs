using System;
using DUI.Graphics;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DUI.UI;

/// <summary>Five stars filled from left to right by convoy length, including the head.</summary>
public sealed class ScoreStars : ContainerRuntime
{
    private const int StarCount = 5;
    private const int FullScore = 84;
    private const float StarGap = 2f;
    private readonly SpriteRuntime[] _filledStars = new SpriteRuntime[StarCount];
    private readonly int _starPixelWidth;
    private int _lastScore = -1;

    public ScoreStars(Texture2D texture)
    {
        // ContentManager owns the shared texture. One source pixel is one
        // pixel in the 720p UI buffer, even though Gum uses a 4x camera zoom.
        _starPixelWidth = texture.Width;
        float starWidth = texture.Width / (float)GameDisplay.UiScale;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;
        Width = StarCount * starWidth + (StarCount - 1) * StarGap;
        Height = texture.Height / (float)GameDisplay.UiScale;
        Anchor(Gum.Wireframe.Anchor.TopLeft);

        for (int index = 0; index < StarCount; index++)
        {
            float x = index * (starWidth + StarGap);
            AddChild(CreateStar(texture, x, C64Theme.MutedColor));
            SpriteRuntime filledStar = CreateStar(texture, x, C64Theme.SymbolColor);
            AddChild(filledStar);
            _filledStars[index] = filledStar;
        }

        UpdateScore(0);
    }

    public void UpdateScore(int segmentCount)
    {
        int score = Math.Clamp(segmentCount, 0, FullScore);
        if (score == _lastScore)
        {
            return;
        }

        _lastScore = score;
        // Exclude gaps from progress and quantize only once across all stars.
        // Crop both source and destination so a partial star is never squeezed.
        int filledPixels = score * StarCount * _starPixelWidth / FullScore;
        for (int index = 0; index < StarCount; index++)
        {
            int width = Math.Clamp(filledPixels - index * _starPixelWidth, 0, _starPixelWidth);
            SpriteRuntime star = _filledStars[index];
            star.Visible = width > 0;
            star.TextureWidth = width;
            star.Width = width / (float)GameDisplay.UiScale;
        }
    }

    private static SpriteRuntime CreateStar(Texture2D texture, float x, Color color)
    {
        var star = new SpriteRuntime
        {
            Texture = texture,
            TextureAddress = TextureAddress.Custom,
            TextureLeft = 0,
            TextureTop = 0,
            TextureWidth = texture.Width,
            TextureHeight = texture.Height,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = texture.Width / (float)GameDisplay.UiScale,
            Height = texture.Height / (float)GameDisplay.UiScale,
            Color = color
        };
        star.Anchor(Gum.Wireframe.Anchor.TopLeft);
        star.X = x;
        return star;
    }
}
