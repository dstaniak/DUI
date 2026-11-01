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

    private readonly ScreenShake _screenShake = new();
    private readonly PowerupEffects _powerups = new();
    private CarConvoy _convoy;
    private Pickup _pickup;
    private BeerBurst _beerBurst;
    private ParkingLot _parkingLot;
    private Rectangle _pickupArea;
    private SoundEffect _collectSoundEffect;
    // Game points expressed in per mille; decimal keeps pickup changes exact.
    private decimal _scorePermille;
    private GameSceneUI _ui;
    private GameState _state;
    private Effect _grayscaleEffect;
    private float _saturation = 1.0f;
    private const float FadeSpeed = 0.01f;

    public decimal ScorePermille => _scorePermille;
    public bool VisualEffectsSuppressed => _powerups.VisualEffectsSuppressed;
    public bool RainbowActive => _powerups.RainbowActive;

    // Drives world effects only while playing; a new game resets it.
    public TimeSpan PlayTime { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        Core.ExitOnEscape = false;

        _pickupArea = _parkingLot.PickupBounds;

        _convoy.BodyCollision += OnConvoyCollision;
        _convoy.BoundaryCollision += OnConvoyCollision;

        Gum.GumService.Default.Root.Children.Clear();

        InitializeUI();
        InitializeNewGame();
    }

    public override void LoadContent()
    {
        _parkingLot = new ParkingLot(Content);
        _convoy = new CarConvoy(LoadSprite("Images/car"), LoadSprite("Images/police-car"), _parkingLot);
        var bounceEffect = Content.Load<SoundEffect>("Audio/bounce");
        _pickup = new Pickup(
            LoadSprite("Images/beer-bottle"),
            LoadSprite("Images/vodka-shot"),
            LoadSprite("Images/powder-bag"),
            LoadSprite("Images/mushroom"),
            LoadSprite("Images/sausage"),
            bounceEffect);
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

        PlayTime += gameTime.ElapsedGameTime;
        _screenShake.Update(gameTime.ElapsedGameTime);
        TimeSpan movementElapsed = _powerups.Update(gameTime.ElapsedGameTime);
        _convoy.QueueDirection(_powerups.MapDirection(GameController.ReadDirection()));
        _convoy.Update(movementElapsed);
        if (_state != GameState.Playing)
        {
            return;
        }

        _pickup.Update(gameTime);
        _beerBurst.Update(gameTime);

        CollisionChecks();
    }

    public override void Draw(GameTime gameTime)
    {
        Effect sceneEffect = null;
        if (_state != GameState.Playing)
        {
            _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);
            sceneEffect = _grayscaleEffect;
        }

        // This batch contains only the world. DuiGame captures UI separately.
        // Sprite textures use straight alpha; preserve their transparent edges.
        Core.SpriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.PointClamp,
            effect: sceneEffect,
            transformMatrix: Matrix.CreateTranslation(
                VisualEffectsSuppressed ? 0f : _screenShake.HorizontalOffset, 0, 0));

        _parkingLot.Draw(Core.SpriteBatch);
        _convoy.Draw();
        _pickup.Draw();
        _beerBurst.Draw(Core.SpriteBatch);
        Core.SpriteBatch.End();
    }

    public void DrawUI()
    {
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
        _convoy.Dispose();
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
        var pickupBounds = _pickup.GetBounds();

        if (convoyBounds.Intersects(pickupBounds))
        {
            CollectPickup();
            return;
        }

        if (pickupBounds.Top <= _pickupArea.Top)
        {
            _pickup.Bounce(Vector2.UnitY);
        }

        if (pickupBounds.Bottom >= _pickupArea.Bottom)
        {
            _pickup.Bounce(-Vector2.UnitY);
        }

        if (pickupBounds.Left <= _pickupArea.Left)
        {
            _pickup.Bounce(Vector2.UnitX);
        }

        if (pickupBounds.Right >= _pickupArea.Right)
        {
            _pickup.Bounce(-Vector2.UnitX);
        }
    }

    private void CollectPickup()
    {
        // Apply the collected type before spawning and choosing the next one.
        PickupType type = _pickup.Type;
        _powerups.Collect(type);
        if (type == PickupType.Beer)
        {
            _beerBurst.Emit(_pickup.Center);
        }
        else if (type == PickupType.Vodka)
        {
            _screenShake.Restart();
        }

        if (PickupRules.GrowsConvoy(type))
        {
            _convoy.Grow();
        }

        _scorePermille = PickupRules.ApplyScore(_scorePermille, type);
        _ui.UpdateScoreText(_scorePermille);
        Core.Audio.PlaySoundEffect(_collectSoundEffect);
        SpawnPickupAwayFromConvoy();
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
        _convoy.Initialize(_parkingLot.StartingPosition);
        _beerBurst.Clear();

        SpawnPickupAwayFromConvoy();

        _scorePermille = 0m;
        PlayTime = TimeSpan.Zero;
        _screenShake.Reset();
        _powerups.Reset();
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

    private void SpawnPickupAwayFromConvoy()
    {
        float parkingCenterX = _pickupArea.X + _pickupArea.Width * 0.5f;
        float parkingCenterY = _pickupArea.Y + _pickupArea.Height * 0.5f;
        Vector2 parkingCenter = new(parkingCenterX, parkingCenterY);

        Circle convoyBounds = _convoy.GetBounds();
        Vector2 convoyCenter = new(convoyBounds.X, convoyBounds.Y);

        Vector2 centerToConvoy = convoyCenter - parkingCenter;

        Circle pickupBounds = _pickup.GetBounds();
        int padding = pickupBounds.Radius * 2;

        // Position is the texture's top-left, so the far-side random limit
        // must also reserve its center offset, not just the collision radius.
        var newPickupPosition = Vector2.Zero;
        if (Math.Abs(centerToConvoy.X) > Math.Abs(centerToConvoy.Y))
        {
            newPickupPosition.Y = Random.Shared.Next(
                _pickupArea.Top + padding, _pickupArea.Bottom - padding * 2
            );

            if (centerToConvoy.X > 0)
            {
                newPickupPosition.X = _pickupArea.Left + padding;
            }
            else
            {
                newPickupPosition.X = _pickupArea.Right - padding * 2;
            }
        }
        else
        {
            newPickupPosition.X = Random.Shared.Next(
                _pickupArea.Left + padding, _pickupArea.Right - padding * 2
            );

            if (centerToConvoy.Y > 0)
            {
                newPickupPosition.Y = _pickupArea.Top + padding;
            }
            else
            {
                newPickupPosition.Y = _pickupArea.Bottom - padding * 2;
            }
        }

        _pickup.Spawn(PickupRules.ChooseType(Random.Shared), newPickupPosition);
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

    private void OnConvoyCollision(object sender, EventArgs e)
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
