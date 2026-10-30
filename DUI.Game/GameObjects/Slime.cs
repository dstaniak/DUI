using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

public sealed class Slime
{
    private readonly AnimatedSprite _sprite;
    private SlimeMovement _movement;

    public event EventHandler BodyCollision;

    public Slime(AnimatedSprite sprite)
    {
        _sprite = sprite;
    }

    public void Initialize(Vector2 startingPosition, float stride)
    {
        _movement = new SlimeMovement(startingPosition, stride);
        _movement.BodyCollision += OnBodyCollision;
    }

    public void QueueDirection(Vector2 direction)
    {
        _movement.QueueDirection(direction);
    }

    public void Grow()
    {
        _movement.Grow();
    }

    public void Update(GameTime gameTime)
    {
        _sprite.Update(gameTime);
        _movement.Update(gameTime.ElapsedGameTime);
    }

    public void Draw()
    {
        foreach (var segment in _movement.Segments)
        {
            Vector2 position = Vector2.Lerp(segment.At, segment.To, _movement.MovementProgress);
            _sprite.Draw(Core.SpriteBatch, position);
        }
    }

    public Circle GetBounds()
    {
        Vector2 position = _movement.GetHeadPosition();
        return new Circle(
            (int)(position.X + _sprite.Width * 0.5f),
            (int)(position.Y + _sprite.Height * 0.5f),
            (int)(_sprite.Width * 0.5f));
    }

    private void OnBodyCollision(object sender, EventArgs args)
    {
        BodyCollision?.Invoke(this, args);
    }
}
