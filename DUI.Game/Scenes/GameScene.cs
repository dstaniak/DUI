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

    private Slime _slime;
    private Bat _bat;
    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private SoundEffect _collectSoundEffect;
    private int _score;
    private GameSceneUI _ui;
    private GameState _state;
    private Effect _grayscaleEffect;
    private float _saturation = 1.0f;
    private const float FadeSpeed = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        Core.ExitOnEscape = false;

        _roomBounds = new Rectangle(0, 0, GameDisplay.Width, GameDisplay.Height);
        _roomBounds.Inflate(-_tilemap.TileWidth, -_tilemap.TileHeight);

        _slime.BodyCollision += OnSlimeBodyCollision;

        Gum.GumService.Default.Root.Children.Clear();

        InitializeUI();
        InitializeNewGame();
    }

    public override void LoadContent()
    {
        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "Images/atlas-definition.xml");
        _tilemap = Tilemap.FromFile(Content, "Images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(4.0f, 4.0f);
        var slimeAnimation = atlas.CreateAnimatedSprite("slime-animation");
        slimeAnimation.Scale = new Vector2(4.0f, 4.0f);
        _slime = new(slimeAnimation);
        var batAnimation = atlas.CreateAnimatedSprite("bat-animation");
        var bounceEffect = Content.Load<SoundEffect>("Audio/bounce");
        batAnimation.Scale = new Vector2(4.0f, 4.0f);
        _bat = new(batAnimation, bounceEffect);
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

        _slime.QueueDirection(GameController.ReadDirection());
        _slime.Update(gameTime);
        _bat.Update(gameTime);

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

        // The atlas uses binary alpha; transparent texels must contribute no RGB.
        Core.SpriteBatch.Begin(
            blendState: BlendState.NonPremultiplied,
            samplerState: SamplerState.PointClamp,
            effect: sceneEffect,
            transformMatrix: Core.Display.Transform);

        _tilemap.Draw(Core.SpriteBatch);
        _slime.Draw();
        _bat.Draw();
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
        var slimeBounds = _slime.GetBounds();
        var batBounds = _bat.GetBounds();

        if (slimeBounds.Intersects(batBounds))
        {
            PositionBatAwayFromSlime();
            _bat.RandomizeVelocity();
            _slime.Grow();
            _score += 100;
            _ui.UpdateScoreText(_score);
            Core.Audio.PlaySoundEffect(_collectSoundEffect);
        }

        if (slimeBounds.Top < _roomBounds.Top ||
            slimeBounds.Bottom > _roomBounds.Bottom ||
            slimeBounds.Left < _roomBounds.Left ||
            slimeBounds.Right > _roomBounds.Right)
        {
            GameOver();
            return;
        }

        if (batBounds.Top <= _roomBounds.Top)
        {
            _bat.Bounce(Vector2.UnitY);
        }

        if (batBounds.Bottom >= _roomBounds.Bottom)
        {
            _bat.Bounce(-Vector2.UnitY);
        }

        if (batBounds.Left <= _roomBounds.Left)
        {
            _bat.Bounce(Vector2.UnitX);
        }

        if (batBounds.Right >= _roomBounds.Right)
        {
            _bat.Bounce(-Vector2.UnitX);
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
        Vector2 slimePos = new();
        slimePos.X = (_tilemap.Columns / 2) * _tilemap.TileWidth;
        slimePos.Y = (_tilemap.Rows / 2) * _tilemap.TileHeight;

        // TODO(dstaniak) : What if TileWidth is different than TileHeight.
        _slime.Initialize(slimePos, _tilemap.TileWidth);

        _bat.RandomizeVelocity();
        PositionBatAwayFromSlime();

        _score = 0;
        _ui.UpdateScoreText(_score);
        _ui.HidePausePanel();
        _ui.HideGameOverPanel();
        GameController.ResetPointer();

        _state = GameState.Playing;

        if (!Core.Instance.IsActive)
        {
            Pause();
        }
    }

    private void PositionBatAwayFromSlime()
    {
        float roomCenterX = _roomBounds.X + _roomBounds.Width * 0.5f;
        float roomCenterY = _roomBounds.Y + _roomBounds.Height * 0.5f;
        Vector2 roomCenter = new(roomCenterX, roomCenterY);

        Circle slimeBounds = _slime.GetBounds();
        Vector2 slimeCenter = new(slimeBounds.X, slimeBounds.Y);

        Vector2 centerToSlime = slimeCenter - roomCenter;

        Circle batBounds = _bat.GetBounds();
        int padding = batBounds.Radius * 2;

        var newBatPosition = Vector2.Zero;
        if (Math.Abs(centerToSlime.X) > Math.Abs(centerToSlime.Y))
        {
            newBatPosition.Y = Random.Shared.Next(
                _roomBounds.Top + padding, _roomBounds.Bottom - padding
            );

            if (centerToSlime.X > 0)
            {
                newBatPosition.X = _roomBounds.Left + padding;
            }
            else
            {
                newBatPosition.X = _roomBounds.Right - padding * 2;
            }
        }
        else
        {
            newBatPosition.X = Random.Shared.Next(
                _roomBounds.Left + padding, _roomBounds.Right - padding
            );

            if (centerToSlime.Y > 0)
            {
                newBatPosition.Y = _roomBounds.Top + padding;
            }
            else
            {
                newBatPosition.Y = _roomBounds.Bottom - padding * 2;
            }
        }

        _bat.Position = newBatPosition;
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

    private void OnSlimeBodyCollision(object sender, EventArgs e)
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
