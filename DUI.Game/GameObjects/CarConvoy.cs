using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Renders the convoy with delayed sideways sway and smooth car headings.</summary>
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
            Vector2 position = Vector2.Lerp(segment.At, segment.To, _movement.MovementProgress);
            Vector2 gridCenter = position + new Vector2(Size * 0.5f);

            // Lawn height and collision geometry continue to use the logical grid path.
            _sprite.Rotation = pose.Rotation;
            _sprite.Scale = _baseScale * parkingLot.GetCarScale(gridCenter);
            _sprite.Draw(Core.SpriteBatch, pose.Position + new Vector2(Size * 0.5f));
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
