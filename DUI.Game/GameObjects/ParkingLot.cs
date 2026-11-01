using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Owns the parking layout, lawn height and car hops over its curbs.</summary>
public sealed class ParkingLot
{
    public const int TileSize = ParkingGeometry.TileSize;

    private readonly Tilemap _tilemap;
    private readonly Texture2D _storefront;
    private readonly TextureRegion _cornerUpright;
    private readonly TextureRegion _cornerRail;
    private readonly TextureRegion _cornerBracket;
    private readonly Rectangle[] _grassIslands;

    public Rectangle DrivingBounds => Geometry.DrivingBounds;
    public Rectangle BottleBounds => Geometry.BottleBounds;
    public Vector2 StartingPosition { get; }
    internal ParkingGeometry Geometry { get; }
    internal ReadOnlySpan<Rectangle> GrassIslands => _grassIslands;

    public ParkingLot(ContentManager content)
    {
        _tilemap = Tilemap.FromFile(content, "Images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(TileSize / _tilemap.TileWidth, TileSize / _tilemap.TileHeight);
        _storefront = content.Load<Texture2D>("Images/c64-storefront");
        TextureRegion upright = _tilemap.GetTile(10);
        TextureRegion rail = _tilemap.GetTile(12);

        // Stop the upright at the horizontal rail's top edge (atlas row 100).
        // Both legs keep the atlas scale, including the diamond mesh pattern.
        _cornerUpright = new TextureRegion(upright.Texture,
            upright.SourceRectangle.X, upright.SourceRectangle.Y, upright.Width, 100);
        _cornerRail = new TextureRegion(rail.Texture,
            rail.SourceRectangle.X + rail.Width / 2, rail.SourceRectangle.Y,
            rail.Width / 2, rail.Height);

        // Only the bolted bracket is needed from the separate corner artwork;
        // its generated fence legs have a different rail spacing.
        Texture2D corner = content.Load<Texture2D>("Images/fence-corner");
        _cornerBracket = new TextureRegion(corner, 544, 510, 205, 245);

        Geometry = new ParkingGeometry(_tilemap.Columns, _tilemap.Rows);
        StartingPosition = new Vector2(
            _tilemap.Columns / 2 * TileSize, _tilemap.Rows / 2 * TileSize);

        _grassIslands = new[]
        {
            new Rectangle(2 * TileSize, 3 * TileSize, 2 * TileSize, 4 * TileSize),
            new Rectangle((_tilemap.Columns - 4) * TileSize, 3 * TileSize, 2 * TileSize, 4 * TileSize)
        };

        foreach (Rectangle island in _grassIslands)
        {
            PlaceGrassTiles(island);
        }
    }

    public float GetCarScale(Vector2 center)
    {
        foreach (Rectangle island in _grassIslands)
        {
            float distanceToEdge = MathF.Min(
                MathF.Min(center.X - island.Left, island.Right - center.X),
                MathF.Min(center.Y - island.Top, island.Bottom - center.Y));

            if (distanceToEdge > 0)
            {
                // Ease up over half a tile at the curb, independently for each
                // car. Sampling its interpolated center also freezes on pause.
                float height = MathHelper.Clamp(distanceToEdge / (TileSize * 0.5f), 0, 1);
                return MathHelper.SmoothStep(1.0f, 1.06f, height);
            }
        }

        return 1.0f;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _tilemap.Draw(spriteBatch);
        DrawFenceCorners(spriteBatch);
        DrawStorefront(spriteBatch);
    }

    private void DrawStorefront(SpriteBatch spriteBatch)
    {
        float width = _tilemap.Columns * TileSize;
        float scale = TileSize * 2f / _storefront.Height;

        // Repeat a complete left display bay, cut through its gray pillars.
        // It has no sign or doorway, so the central facade remains unique.
        var displayBay = new Rectangle(26, 0, 432, _storefront.Height);
        float bayWidth = displayBay.Width * scale;
        float sideWidth = (width - _storefront.Width * scale) * 0.5f;
        for (float offset = 0; offset < sideWidth; offset += bayWidth)
        {
            // Any partial bay is clipped at the outer screen edge, not beside
            // the entrance. Work outwards from the complete central facade.
            DrawBay(sideWidth - offset - bayWidth);
            DrawBay(width - sideWidth + offset);
        }

        // One uniform scale keeps doors, lettering and bottles in proportion.
        // The bays fill the remaining width without stretching the 3:1 artwork.
        var center = new Vector2(sideWidth, 0);
        spriteBatch.Draw(_storefront, center, null,
            Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

        void DrawBay(float left)
        {
            spriteBatch.Draw(_storefront, new Vector2(left, 0), displayBay,
                Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }

    private void DrawFenceCorners(SpriteBatch spriteBatch)
    {
        int bottom = (_tilemap.Rows - 1) * TileSize;
        DrawFenceCorner(spriteBatch, new Vector2(0, bottom), false);
        DrawFenceCorner(spriteBatch,
            new Vector2((_tilemap.Columns - 1) * TileSize, bottom), true);
    }

    private void DrawFenceCorner(SpriteBatch spriteBatch, Vector2 origin, bool mirrored)
    {
        SpriteEffects effects = mirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        // Draw the rail first: its opaque asphalt must not cover the upright.
        // The source half and its destination share the exact regular-tile scale.
        float railOffset = mirrored ? 0 : TileSize * 0.5f;
        _cornerRail.Draw(spriteBatch, origin + new Vector2(railOffset, 0),
            Color.White, 0, Vector2.Zero, _tilemap.Scale, effects, 0);
        _cornerUpright.Draw(spriteBatch, origin,
            Color.White, 0, Vector2.Zero, _tilemap.Scale, effects, 0);

        const float bracketWidth = 14;
        float bracketLeft = mirrored ? TileSize - 34 - bracketWidth : 34;
        var bracketScale = new Vector2(bracketWidth / _cornerBracket.Width, 23.0f / _cornerBracket.Height);
        _cornerBracket.Draw(spriteBatch, origin + new Vector2(bracketLeft, 25),
            Color.White, 0, Vector2.Zero, bracketScale, effects, 0);
    }

    private void PlaceGrassTiles(Rectangle island)
    {
        int left = island.Left / TileSize;
        int top = island.Top / TileSize;
        int rows = island.Height / TileSize;
        for (int row = 0; row < rows; row++)
        {
            // Atlas pairs: top 4/5, middle 6/7, bottom 8/9. The same
            // rectangles drive both these visible tiles and the height query.
            int leftTile = row == 0 ? 4 : row == rows - 1 ? 8 : 6;
            _tilemap.SetTile(left, top + row, leftTile);
            _tilemap.SetTile(left + 1, top + row, leftTile + 1);
        }
    }
}
