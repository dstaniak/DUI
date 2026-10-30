using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class TextureAtlas
{
    public Texture2D Texture { get; set; }

    private Dictionary<string, TextureRegion> _regions;
    private Dictionary<string, Animation> _animations;

    public TextureAtlas()
    {
        _regions = [];
        _animations = [];
    }

    public TextureAtlas(Texture2D texture) : this()
    {
        Texture = texture;
    }

    public void AddRegion(string name, int x, int y, int width, int height)
    {
        var region = new TextureRegion(Texture, x, y, width, height);
        _regions.Add(name, region);
    }

    public TextureRegion GetRegion(string name) => _regions[name];
    public bool RemoveRegion(string name) => _regions.Remove(name);

    public void AddAnimation(string animationName, Animation animation) => _animations.Add(animationName, animation);
    public Animation GetAnimation(string animationName) => _animations[animationName];
    public bool RemoveAnimation(string animationName) => _animations.Remove(animationName);

    public void Clear()
    {
        _regions.Clear();
        _animations.Clear();
    }

    public Sprite CreateSprite(string regionName) => new(GetRegion(regionName));

    public AnimatedSprite CreateAnimatedSprite(string animationName) => new(GetAnimation(animationName));

    public static TextureAtlas FromFile(ContentManager content, string fileName)
    {
        TextureAtlas atlas = new();

        string filePath = Path.Combine(content.RootDirectory, fileName);

        using var stream = TitleContainer.OpenStream(filePath);
        using var reader = XmlReader.Create(stream);

        XDocument doc = XDocument.Load(reader);
        XElement root = doc.Root;

        string texturePath = root.Element("Texture").Value;
        atlas.Texture = content.Load<Texture2D>(texturePath);

        var regionNodes = root.Element("Regions")?.Elements("Region");
        if (regionNodes != null)
        {
            foreach (var regionNode in regionNodes)
            {
                string name = regionNode.Attribute("name")?.Value;
                int x = int.Parse(regionNode.Attribute("x")?.Value ?? "0");
                int y = int.Parse(regionNode.Attribute("y")?.Value ?? "0");
                int width = int.Parse(regionNode.Attribute("width")?.Value ?? "0");
                int height = int.Parse(regionNode.Attribute("height")?.Value ?? "0");

                if (!string.IsNullOrEmpty(name))
                {
                    atlas.AddRegion(name, x, y, width, height);
                }
            }
        }

        var animationNodes = root.Element("Animations")?.Elements("Animation");
        if (animationNodes != null)
        {
            foreach (var animationNode in animationNodes)
            {
                string name = animationNode.Attribute("name")?.Value;
                float delayMs = float.Parse(animationNode.Attribute("delay")?.Value ?? "0");
                TimeSpan delay = TimeSpan.FromMilliseconds(delayMs);

                List<TextureRegion> frames = [];

                var frameNodes = animationNode.Elements("Frame");
                if (frameNodes != null)
                {
                    foreach (var frameNode in frameNodes)
                    {
                        string regionName = frameNode.Attribute("region")?.Value;
                        var region = atlas.GetRegion(regionName);
                        frames.Add(region);
                    }
                }

                Animation animation = new(frames, delay);
                atlas.AddAnimation(name, animation);
            }
        }

        return atlas;
    }
}
