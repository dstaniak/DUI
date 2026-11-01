using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.GameObjects;

/// <summary>Renders the player's car and police followers with roof lights and curb hops.</summary>
public sealed class CarConvoy : IDisposable
{
    public const float Size = 80.0f;

    private readonly Sprite _playerSprite;
    private readonly Sprite _policeSprite;
    private readonly Vector2 _playerScale;
    private readonly Vector2 _policeScale;
    private readonly PoliceLightbar _lightbar;
    private readonly ParkingLot _parkingLot;
    private readonly List<CarHop> _hops = [];
    private readonly List<CarPose> _previousPoses = [];
    private ConvoyMovement _movement;

    public event EventHandler BodyCollision;
    public event EventHandler BoundaryCollision;

    // The player's car is segment zero and contributes one point.
    public int SegmentCount => _movement.Cars.Count;

    public CarConvoy(Sprite playerSprite, Sprite policeSprite, ParkingLot parkingLot)
    {
        _parkingLot = parkingLot;
        _playerSprite = playerSprite;
        _policeSprite = policeSprite;
        _playerSprite.CenterOrigin();
        _policeSprite.CenterOrigin();
        _playerScale = new Vector2(Size / playerSprite.Region.Width, Size / playerSprite.Region.Height);
        _policeScale = new Vector2(Size / policeSprite.Region.Width, Size / policeSprite.Region.Height);
        _lightbar = new PoliceLightbar(policeSprite.Region.Texture.GraphicsDevice);
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

    public void Update(TimeSpan movementElapsed)
    {
        _movement.Update(movementElapsed);
    }

    public void Draw()
    {
        for (int i = 0; i < _movement.Cars.Count; i++)
        {
            CarPose logicalPose = _movement.GetRenderPose(i);
            CarPose pose = CarSway.GetPose(logicalPose);
            var hop = _hops[i].GetTransform(_movement.RenderTime);
            float terrainScale = _parkingLot.GetCarScale(logicalPose.Center);
            float visualScale = terrainScale * hop.Scale;
            Sprite sprite = i == 0 ? _playerSprite : _policeSprite;
            Vector2 baseScale = i == 0 ? _playerScale : _policeScale;

            // Apply lift in screen-up board space, independently of the car's heading.
            Vector2 center = pose.Center - Vector2.UnitY * hop.Lift;
            sprite.Rotation = pose.Rotation;
            sprite.Scale = baseScale * visualScale;
            sprite.Draw(Core.SpriteBatch, center);
            if (i > 0)
            {
                _lightbar.Draw(Core.SpriteBatch, center, pose.Rotation,
                    visualScale, _movement.RenderTime);
            }
        }
    }

    public void Dispose()
    {
        // Sprite textures belong to the scene's ContentManager.
        _lightbar.Dispose();
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

        if (_parkingLot.Geometry.IntersectsBoundary(_movement.Cars[0]))
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
