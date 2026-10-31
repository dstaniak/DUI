using System;
using DUI.GameObjects;
using DUI.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;

namespace DUI.Scenes;

public class GameScene : Scene
{
    private enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

    private CarConvoy _convoy;
    private BeerBottle _bottle;
    private BeerBurst _beerBurst;
    private ParkingLot _parkingLot;
    private Rectangle _drivingBounds;
    private SoundEffect _collectSoundEffect;
    // Game points expressed in per mille; decimal keeps each 0.2 step exact.
    private decimal _scorePermille;
    private GameSceneUI _ui;
    private GameState _state;
    private Effect _grayscaleEffect;
    private float _saturation = 1.0f;
    private const float FadeSpeed = 0.01f;
    private const decimal BeerScorePermille = 0.2m;

    public override void Initialize()
    {
        base.Initialize();

        Core.ExitOnEscape = false;

        _drivingBounds = _parkingLot.DrivingBounds;

        _convoy.BodyCollision += OnConvoyBodyCollision;

        Gum.GumService.Default.Root.Children.Clear();

        InitializeUI();
        InitializeNewGame();
    }

    public override void LoadContent()
    {
        _parkingLot = new ParkingLot(Content);
        _convoy = new CarConvoy(LoadSprite("Images/car"));
        var bounceEffect = Content.Load<SoundEffect>("Audio/bounce");
        _bottle = new BeerBottle(LoadSprite("Images/beer-bottle"), bounceEffect);
        _beerBurst = new BeerBurst(Core.GraphicsDevice);
        _collectSoundEffect = Content.Load<SoundEffect>("Audio/collect");
        _grayscaleEffect = Content.Load<Effect>("Effects/grayscaleEffect");
    }

    public override void Update(GameTime gameTime)
    {
        GameController.Update();
        GameState stateBeforeUi = _state;
        _ui.Update(gameTime);

        // A click that resumes or restarts belongs to the UI. It must not also
        // steer the first frame of the resumed game.
        if (_state != stateBeforeUi)
        {
            GameController.ResetPointer();
            return;
        }

        if (_state != GameState.Playing)
        {
            _saturation = Math.Max(0.0f, _saturation - FadeSpeed);

            if (_state == GameState.GameOver)
            {
                return;
            }
        }

        if (GameController.Pause())
        {
            TogglePause();
        }

        if (_state == GameState.Paused)
        {
            GameController.ResetPointer();
            return;
        }

        _convoy.QueueDirection(GameController.ReadDirection());
        _convoy.Update(gameTime);
        _bottle.Update(gameTime);
        _beerBurst.Update(gameTime);

        CollisionChecks();
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        Effect sceneEffect = null;
        if (_state != GameState.Playing)
        {
            _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
            sceneEffect = _grayscaleEffect;
        }

        // Sprite textures use straight alpha; preserve their transparent edges.
        Core.SpriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.PointClamp,
            effect: sceneEffect,
            transformMatrix: Core.Display.Transform);

        _parkingLot.Draw(Core.SpriteBatch);
        _convoy.Draw(_parkingLot);
        _bottle.Draw();
        _beerBurst.Draw(Core.SpriteBatch);
        Core.SpriteBatch.End();
        _ui.Draw();

    }

    public void Pause()
    {
        if (_ui == null || _state != GameState.Playing)
        {
            return;
        }

        _ui.ShowPausePanel();
        _state = GameState.Paused;
        _saturation = 1.0f;
        GameController.ResetPointer();
    }

    public override void UnloadContent()
    {
        _beerBurst?.Dispose();
        base.UnloadContent();
    }

    private Sprite LoadSprite(string assetName)
    {
        Texture2D texture = Content.Load<Texture2D>(assetName);
        return new Sprite(new TextureRegion(texture, 0, 0, texture.Width, texture.Height));
    }

    private void TogglePause()
    {
        if (_state == GameState.Paused)
        {
            _ui.HidePausePanel();
            _state = GameState.Playing;
        }
        else if (_state == GameState.Playing)
        {
            Pause();
        }
    }

    private void CollisionChecks()
    {
        var convoyBounds = _convoy.GetBounds();
        var bottleBounds = _bottle.GetBounds();

        if (convoyBounds.Intersects(bottleBounds))
        {
            // Capture the pickup location before the next bottle is positioned.
            _beerBurst.Emit(_bottle.Center);
            PositionBottleAwayFromConvoy();
            _bottle.RandomizeVelocity();
            _convoy.Grow();
            _scorePermille += BeerScorePermille;
            _ui.UpdateScoreText(_scorePermille);
            Core.Audio.PlaySoundEffect(_collectSoundEffect);
        }

        if (convoyBounds.Top < _drivingBounds.Top ||
            convoyBounds.Bottom > _drivingBounds.Bottom ||
            convoyBounds.Left < _drivingBounds.Left ||
            convoyBounds.Right > _drivingBounds.Right)
        {
            GameOver();
            return;
        }

        if (bottleBounds.Top <= _drivingBounds.Top)
        {
            _bottle.Bounce(Vector2.UnitY);
        }

        if (bottleBounds.Bottom >= _drivingBounds.Bottom)
        {
            _bottle.Bounce(-Vector2.UnitY);
        }

        if (bottleBounds.Left <= _drivingBounds.Left)
        {
            _bottle.Bounce(Vector2.UnitX);
        }

        if (bottleBounds.Right >= _drivingBounds.Right)
        {
            _bottle.Bounce(-Vector2.UnitX);
        }
    }

    private void InitializeUI()
    {
        Gum.GumService.Default.Root.Children.Clear();
        _ui = new GameSceneUI();
        _ui.ResumeButtonClick += OnResumeButtonClicked;
        _ui.RetryButtonClick += OnRetryButtonClicked;
        _ui.PauseButtonClick += OnPauseButtonClicked;
    }

    private void InitializeNewGame()
    {
        _convoy.Initialize(_parkingLot.StartingPosition, ParkingLot.TileSize);
        _beerBurst.Clear();

        _bottle.RandomizeVelocity();
        PositionBottleAwayFromConvoy();

        _scorePermille = 0m;
        _ui.UpdateScoreText(_scorePermille);
        _ui.HidePausePanel();
        _ui.HideGameOverPanel();
        GameController.ResetPointer();

        _state = GameState.Playing;

        if (!Core.Instance.IsActive)
        {
            Pause();
        }
    }

    private void PositionBottleAwayFromConvoy()
    {
        float parkingCenterX = _drivingBounds.X + _drivingBounds.Width * 0.5f;
        float parkingCenterY = _drivingBounds.Y + _drivingBounds.Height * 0.5f;
        Vector2 parkingCenter = new(parkingCenterX, parkingCenterY);

        Circle convoyBounds = _convoy.GetBounds();
        Vector2 convoyCenter = new(convoyBounds.X, convoyBounds.Y);

        Vector2 centerToConvoy = convoyCenter - parkingCenter;

        Circle bottleBounds = _bottle.GetBounds();
        int padding = bottleBounds.Radius * 2;

        // Position is the texture's top-left, so the far-side random limit
        // must also reserve its center offset, not just the collision radius.
        var newBottlePosition = Vector2.Zero;
        if (Math.Abs(centerToConvoy.X) > Math.Abs(centerToConvoy.Y))
        {
            newBottlePosition.Y = Random.Shared.Next(
                _drivingBounds.Top + padding, _drivingBounds.Bottom - padding * 2
            );

            if (centerToConvoy.X > 0)
            {
                newBottlePosition.X = _drivingBounds.Left + padding;
            }
            else
            {
                newBottlePosition.X = _drivingBounds.Right - padding * 2;
            }
        }
        else
        {
            newBottlePosition.X = Random.Shared.Next(
                _drivingBounds.Left + padding, _drivingBounds.Right - padding * 2
            );

            if (centerToConvoy.Y > 0)
            {
                newBottlePosition.Y = _drivingBounds.Top + padding;
            }
            else
            {
                newBottlePosition.Y = _drivingBounds.Bottom - padding * 2;
            }
        }

        _bottle.Position = newBottlePosition;
        _bottle.ResetPulse();
    }

    private void OnResumeButtonClicked(object sender, EventArgs e)
    {
        _state = GameState.Playing;
    }

    private void OnRetryButtonClicked(object sender, EventArgs e)
    {
        InitializeNewGame();
    }

    private void OnPauseButtonClicked(object sender, EventArgs e)
    {
        Pause();
    }

    private void OnConvoyBodyCollision(object sender, EventArgs e)
    {
        GameOver();
    }

    private void GameOver()
    {
        _ui.ShowGameOverPanel();
        _state = GameState.GameOver;
        _saturation = 1.0f;
    }

}
