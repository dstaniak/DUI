using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>A moving collectible whose visual pulse does not alter its collision bounds.</summary>
public sealed class BeerBottle
{
    public const float Size = 80.0f;
    private const float MovementSpeed = 0.5f;
    private const double PulsePeriodSeconds = 0.8;

    private readonly Sprite _sprite;
    private readonly SoundEffect _bounceEffect;
    private readonly Vector2 _baseScale;
    private Vector2 _velocity;
    private double _pulseTime;

    public BeerBottle(Sprite sprite, SoundEffect bounceEffect)
    {
        _sprite = sprite;
        _sprite.CenterOrigin();
        _baseScale = new Vector2(Size / sprite.Region.Width, Size / sprite.Region.Height);
        _bounceEffect = bounceEffect;
    }

    public Vector2 Position { get; set; }
    public Vector2 Center => Position + new Vector2(Size * 0.5f);

    public void ResetPulse()
    {
        _pulseTime = 0;
    }

    public void RandomizeVelocity()
    {
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new(x, y);
        _velocity = direction * MovementSpeed;
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
}
