using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Renders each snake segment as a car facing along its current grid step.</summary>
public sealed class CarConvoy
{
    public const float Size = 80.0f;

    private readonly Sprite _sprite;
    private SnakeMovement _movement;

    public event EventHandler BodyCollision;

    public CarConvoy(Sprite sprite)
    {
        _sprite = sprite;
        _sprite.CenterOrigin();
        _sprite.Scale = new Vector2(Size / sprite.Region.Width, Size / sprite.Region.Height);
    }

    public void Initialize(Vector2 startingPosition, float stride)
    {
        _movement = new SnakeMovement(startingPosition, stride);
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
        _movement.Update(gameTime.ElapsedGameTime);
    }

    public void Draw()
    {
        float rotation = 0.0f;
        foreach (var segment in _movement.Segments)
        {
            Vector2 position = Vector2.Lerp(segment.At, segment.To, _movement.MovementProgress);
            Vector2 travel = segment.To - segment.At;
            if (travel != Vector2.Zero)
            {
                rotation = MathF.Atan2(travel.Y, travel.X);
            }

            // A newly grown tail overlaps its predecessor until the next step.
            // Reuse that car's heading while the new segment has no travel yet.
            _sprite.Rotation = rotation;
            _sprite.Draw(Core.SpriteBatch, position + new Vector2(Size * 0.5f));
        }
    }

    public Circle GetBounds()
    {
        Vector2 position = _movement.GetHeadPosition();
        return new Circle(
            (int)(position.X + Size * 0.5f),
            (int)(position.Y + Size * 0.5f),
            (int)(Size * 0.5f));
    }

    private void OnBodyCollision(object sender, EventArgs args)
    {
        BodyCollision?.Invoke(this, args);
    }
}
