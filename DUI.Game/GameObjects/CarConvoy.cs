using System;
using System.Collections.Generic;
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
    private readonly ParkingLot _parkingLot;
    private readonly List<CarHop> _hops = [];
    private readonly List<CarPose> _previousPoses = [];
    private ConvoyMovement _movement;

    public event EventHandler BodyCollision;
    public event EventHandler BoundaryCollision;

    public CarConvoy(Sprite sprite, ParkingLot parkingLot)
    {
        _parkingLot = parkingLot;
        _sprite = sprite;
        _sprite.CenterOrigin();
        _baseScale = new Vector2(Size / sprite.Region.Width, Size / sprite.Region.Height);
        _sprite.Scale = _baseScale;
    }

    public void Initialize(Vector2 startingPosition)
    {
        // Parking placement uses tile origins; the movement model always uses car centers.
        _movement = new ConvoyMovement(startingPosition + new Vector2(Size * 0.5f));
        _movement.BodyCollision += OnBodyCollision;
        _movement.Stepped += OnMovementStepped;
        _hops.Clear();
        _previousPoses.Clear();
        AddCarAnimation();
    }

    public void QueueDirection(Vector2 direction)
    {
        _movement.QueueDirection(direction);
    }

    public void Grow()
    {
        _movement.Grow();
        AddCarAnimation();
    }

    public void Update(GameTime gameTime)
    {
        _movement.Update(gameTime.ElapsedGameTime);
    }

    public void Draw()
    {
        for (int i = 0; i < _movement.Cars.Count; i++)
        {
            CarPose logicalPose = _movement.GetRenderPose(i);
            CarPose pose = CarSway.GetPose(logicalPose);
            var hop = _hops[i].GetTransform(_movement.RenderTime);
            float terrainScale = _parkingLot.GetCarScale(logicalPose.Center);

            // Apply lift in screen-up board space, independently of the car's heading.
            _sprite.Rotation = pose.Rotation;
            _sprite.Scale = _baseScale * terrainScale * hop.Scale;
            _sprite.Draw(Core.SpriteBatch, pose.Center - Vector2.UnitY * hop.Lift);
        }
    }

    public Circle GetBounds()
    {
        Vector2 center = _movement.Cars[0].Center;
        return new Circle(
            (int)center.X,
            (int)center.Y,
            (int)(Size * 0.5f));
    }

    private void AddCarAnimation()
    {
        _hops.Add(new CarHop());
        _previousPoses.Add(_movement.Cars[_movement.Cars.Count - 1]);
    }

    private void OnMovementStepped(object sender, EventArgs args)
    {
        for (int i = 0; i < _movement.Cars.Count; i++)
        {
            CarPose pose = _movement.Cars[i];
            _hops[i].Advance(_previousPoses[i], pose, _movement.ElapsedTime, _parkingLot.GrassIslands);
            _previousPoses[i] = pose;
        }

        if (ConvoyCollision.IsOutsideParking(_movement.Cars[0], _parkingLot.DrivingBounds))
        {
            _movement.Stop();
            BoundaryCollision?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnBodyCollision(object sender, EventArgs args)
    {
        BodyCollision?.Invoke(this, args);
    }
}
