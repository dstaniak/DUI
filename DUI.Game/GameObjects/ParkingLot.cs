using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Owns the parking layout and the visual height of its traversable lawns.</summary>
public sealed class ParkingLot
{
    public const int TileSize = 80;

    private readonly Tilemap _tilemap;
    private readonly Texture2D _storefront;
    private readonly Texture2D _fenceCorner;
    private readonly Rectangle[] _grassIslands;

    public Rectangle DrivingBounds { get; }
    public Vector2 StartingPosition { get; }

    public ParkingLot(ContentManager content)
    {
        _tilemap = Tilemap.FromFile(content, "Images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(TileSize / _tilemap.TileWidth, TileSize / _tilemap.TileHeight);
        _storefront = content.Load<Texture2D>("Images/storefront");
        _fenceCorner = content.Load<Texture2D>("Images/fence-corner");

        // The shop occupies two rows. The closed gate stays outside the same
        // driving bounds as the rest of the bottom fence.
        DrivingBounds = new Rectangle(
            TileSize, TileSize * 2,
            (_tilemap.Columns - 2) * TileSize, (_tilemap.Rows - 3) * TileSize);
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
        var shopBounds = new Rectangle(0, 0, _tilemap.Columns * TileSize, TileSize * 2);
        spriteBatch.Draw(_storefront, shopBounds, Color.White);
    }

    private void DrawFenceCorners(SpriteBatch spriteBatch)
    {
        int bottom = (_tilemap.Rows - 1) * TileSize;
        var leftCorner = new Rectangle(0, bottom, TileSize, TileSize);
        var rightCorner = new Rectangle((_tilemap.Columns - 1) * TileSize, bottom, TileSize, TileSize);

        // One L-shaped tile joins the side fence to the bottom rail. Mirroring
        // the same artwork keeps the two corner posts exactly symmetrical.
        spriteBatch.Draw(_fenceCorner, leftCorner, Color.White);
        spriteBatch.Draw(_fenceCorner, rightCorner, null, Color.White,
            0, Vector2.Zero, SpriteEffects.FlipHorizontally, 0);
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
