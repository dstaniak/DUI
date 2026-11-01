using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>A moving collectible whose visual pulse does not alter its collision bounds.</summary>
public sealed class Pickup
{
    public const float Size = 80.0f;
    private const float MovementSpeed = 0.5f;
    private const double PulsePeriodSeconds = 0.8;

    private readonly Dictionary<PickupType, Sprite> _sprites;
    private readonly SoundEffect _bounceEffect;
    private Sprite _sprite;
    private Vector2 _baseScale;
    private Vector2 _velocity;
    private double _pulseTime;

    public Pickup(Sprite beerSprite, Sprite vodkaSprite, Sprite powderSprite,
        Sprite mushroomSprite, Sprite sausageSprite, SoundEffect bounceEffect)
    {
        _sprites = new Dictionary<PickupType, Sprite>
        {
            [PickupType.Beer] = beerSprite,
            [PickupType.Vodka] = vodkaSprite,
            [PickupType.Powder] = powderSprite,
            [PickupType.Mushroom] = mushroomSprite,
            [PickupType.Sausage] = sausageSprite
        };
        foreach (Sprite sprite in _sprites.Values)
        {
            sprite.CenterOrigin();
        }

        _bounceEffect = bounceEffect;
        SetType(PickupType.Beer);
    }

    public PickupType Type { get; private set; }
    public Vector2 Position { get; set; }
    public Vector2 Center => Position + new Vector2(Size * 0.5f);

    public void Spawn(PickupType type, Vector2 position)
    {
        SetType(type);
        Position = position;
        _pulseTime = 0;
        RandomizeVelocity();
    }

    public void Bounce(Vector2 normal)
    {
        Vector2 newPosition = Position;

        if (normal.X != 0)
        {
            newPosition.X += normal.X * (Size * 0.1f);
        }

        if (normal.Y != 0)
        {
            newPosition.Y += normal.Y * (Size * 0.1f);
        }

        Position = newPosition;

        normal.Normalize();

        _velocity = Vector2.Reflect(_velocity, normal);

        Core.Audio.PlaySoundEffect(_bounceEffect);
    }

    public Circle GetBounds()
    {
        int x = (int)Center.X;
        int y = (int)Center.Y;
        int radius = (int)(Size * 0.25f);

        return new(x, y, radius);
    }

    public void Update(GameTime gameTime)
    {
        _pulseTime = (_pulseTime + gameTime.ElapsedGameTime.TotalSeconds) % PulsePeriodSeconds;
        Position += _velocity;
    }

    public void Draw()
    {
        float phase = (float)(_pulseTime / PulsePeriodSeconds * Math.Tau);
        _sprite.Scale = _baseScale * (1.0f + 0.13f * MathF.Sin(phase));
        _sprite.Draw(Core.SpriteBatch, Center);
    }

    private void SetType(PickupType type)
    {
        _sprite = _sprites[type];
        _baseScale = new Vector2(Size / _sprite.Region.Width, Size / _sprite.Region.Height);
        Type = type;
    }

    private void RandomizeVelocity()
    {
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new(x, y);
        _velocity = direction * MovementSpeed;
    }
}
