using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Renders the convoy with sideways sway, smooth headings and curb hops.</summary>
public sealed class CarConvoy
{
    public const float Size = 80.0f;

    private readonly Sprite _sprite;
    private readonly Vector2 _baseScale;
    private SnakeMovement _movement;

    public event EventHandler BodyCollision;

    public CarConvoy(Sprite sprite)
    {
        _sprite = sprite;
        _sprite.CenterOrigin();
        _baseScale = new Vector2(Size / sprite.Region.Width, Size / sprite.Region.Height);
        _sprite.Scale = _baseScale;
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

    public void Draw(ParkingLot parkingLot)
    {
        for (int i = 0; i < _movement.Segments.Count; i++)
        {
            var segment = _movement.Segments[i];
            var pose = CarSway.GetPose(_movement, i);
            var terrain = parkingLot.GetCarTransform(segment, _movement.MovementProgress);
            Vector2 center = pose.Position + new Vector2(Size * 0.5f);

            // Apply lift in screen-up board space, independently of the car's heading.
            _sprite.Rotation = pose.Rotation;
            _sprite.Scale = _baseScale * terrain.Scale;
            _sprite.Draw(Core.SpriteBatch, center - Vector2.UnitY * terrain.Lift);
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
