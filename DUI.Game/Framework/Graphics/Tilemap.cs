using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class Tilemap
{
    public int Columns { get; }
    public int Rows { get; }
    public int Count { get; }
    public Vector2 Scale { get; set; }
    public float TileWidth => _tileset.TileWidth * Scale.X;
    public float TileHeight => _tileset.TileHeight * Scale.Y;

    private readonly Tileset _tileset;
    private readonly int[] _tiles;

    public Tilemap(Tileset tileset, int columns, int rows)
    {
        _tileset = tileset;
        Columns = columns;
        Rows = rows;
        Count = Columns * Rows;
        Scale = Vector2.One;
        _tiles = new int[Count];
    }

    public void SetTile(int index, int tilesetId)
    {
        _tiles[index] = tilesetId;
    }

    public void SetTile(int column, int row, int tilesetId)
    {
        _tiles[column + row * Columns] = tilesetId;
    }

    public TextureRegion GetTile(int index) => _tileset.GetTile(index);
    public TextureRegion GetTile(int column, int row) => _tileset.GetTile(column + row * Columns);

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < Count; ++i)
        {
            TextureRegion tile = GetTile(_tiles[i]);
            int x = i % Columns;
            int y = i / Columns;
            Vector2 position = new(x * TileWidth, y * TileHeight);
            tile.Draw(
                spriteBatch,
                position,
                Color.White,
                0.0f,
                Vector2.Zero,
                Scale,
                SpriteEffects.None,
                1.0f);
        }
    }

    public static Tilemap FromFile(ContentManager content, string filename)
    {
        string filePath = Path.Combine(content.RootDirectory, filename);

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);

        var doc = XDocument.Load(reader);
        var root = doc.Root;

        var tilesetNode = root.Element("Tileset");

        string regionName = tilesetNode.Attribute("region").Value;
        string[] split = regionName.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        int x = int.Parse(split[0]);
        int y = int.Parse(split[1]);
        int width = int.Parse(split[2]);
        int height = int.Parse(split[3]);
        int tileWidth = int.Parse(tilesetNode.Attribute("tileWidth").Value);
        int tileHeight = int.Parse(tilesetNode.Attribute("tileHeight").Value);
        int spacing = int.Parse(tilesetNode.Attribute("spacing")?.Value ?? "0");
        string contentPath = tilesetNode.Value;

        var texture = content.Load<Texture2D>(contentPath);
        TextureRegion textureRegion = new(texture, x, y, width, height);
        Tileset tileset = new(textureRegion, tileWidth, tileHeight, spacing);

        var tilesNode = root.Element("Tiles");
        string[] rows = tilesNode.Value.Trim().Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int columnCount = rows[0].Split(" ", StringSplitOptions.RemoveEmptyEntries).Length;

        Tilemap tilemap = new(tileset, columnCount, rows.Length);

        for (int row = 0; row < rows.Length; ++row)
        {
            string[] columns = rows[row].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            for (int column = 0; column < columnCount; ++column)
            {
                int tilesetId = int.Parse(columns[column]);
                tilemap.SetTile(column, row, tilesetId);
            }
        }

        return tilemap;
    }

}
