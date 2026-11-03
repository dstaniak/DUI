using System;

namespace MonoGameLibrary.Graphics;

public class Tileset
{
    public int TileWidth { get; }
    public int TileHeight { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int Count { get; }
    public int Spacing { get; }

    private readonly TextureRegion[] _tiles;

    public Tileset(TextureRegion textureRegion, int tileWidth, int tileHeight, int spacing = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(spacing);

        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Spacing = spacing;

        int columnStride = tileWidth + spacing;
        int rowStride = tileHeight + spacing;
        // The region ends at the last tile's edge, without a trailing gap.
        Columns = (textureRegion.Width + spacing) / columnStride;
        Rows = (textureRegion.Height + spacing) / rowStride;
        Count = Columns * Rows;
        _tiles = new TextureRegion[Count];

        for (int i = 0; i < Count; ++i)
        {
            int x = i % Columns * columnStride;
            int y = i / Columns * rowStride;

            _tiles[i] = new TextureRegion(
                textureRegion.Texture,
                textureRegion.SourceRectangle.X + x,
                textureRegion.SourceRectangle.Y + y,
                tileWidth,
                tileHeight);
        }
    }

    public TextureRegion GetTile(int index) => _tiles[index];
    public TextureRegion GetTile(int column, int row) => GetTile(column + row * Columns);
}
