using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

public class Bat(AnimatedSprite sprite, SoundEffect bounceEffect)
{
    public Vector2 Position { get; set; }

    private const float MovementSpeed = 0.5f;
    private Vector2 _velocity;
    private AnimatedSprite _sprite = sprite;
    private SoundEffect _bounceEffect = bounceEffect;

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
            newPosition.X += normal.X * (_sprite.Width * 0.1f);
        }

        if (normal.Y != 0)
        {
            newPosition.Y += normal.Y * (_sprite.Height * 0.1f);
        }

        Position = newPosition;

        normal.Normalize();

        _velocity = Vector2.Reflect(_velocity, normal);

        Core.Audio.PlaySoundEffect(_bounceEffect);
    }

    public Circle GetBounds()
    {
        int x = (int)(Position.X + _sprite.Width * 0.5f);
        int y = (int)(Position.Y + _sprite.Height * 0.5f);
        int radius = (int)(_sprite.Width * 0.25f);

        return new(x, y, radius);
    }

    public void Update(GameTime gameTime)
    {
        _sprite.Update(gameTime);
        Position += _velocity;
    }

    public void Draw()
    {
        _sprite.Draw(Core.SpriteBatch, Position);
    }
}
