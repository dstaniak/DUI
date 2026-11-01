using System;
using DUI.Scenes;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI;

public sealed class DuiGame : Core
{
    private GameScene _gameScene;
    private Song _themeSong;
    private Point _screenSize;
    private RetroRenderer _renderer;

    public DuiGame() : base("DUI — Snake", 1280, 720, false)
    {
        // The collectible moves 0.5 pixels per update; keep its 60 Hz pace
        // regardless of a phone's display refresh rate.
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitializeGum();
        _renderer = new RetroRenderer(GraphicsDevice,
            Content.Load<Effect>("Effects/doubleVisionEffect"),
            Content.Load<Effect>("Effects/rainbowEffect"));
        UpdateDisplay();
        Audio.PlaySong(_themeSong);
        _gameScene = new GameScene();
        ChangeScene(_gameScene);
    }

    protected override void LoadContent()
    {
        _themeSong = Content.Load<Song>("Audio/theme");
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateDisplay();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.BeginFrame();
        base.Draw(gameTime);
        _renderer.Present(SpriteBatch, Display, _gameScene.ScorePermille, _gameScene.PlayTime,
            _gameScene.VisualEffectsSuppressed, _gameScene.RainbowActive);
    }

    protected override void UnloadContent()
    {
        _renderer?.Dispose();
        base.UnloadContent();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        _gameScene?.Pause();
        Audio?.PauseAudio();
        base.OnDeactivated(sender, args);
    }

    protected override void OnActivated(object sender, EventArgs args)
    {
        // Returning to the app leaves gameplay paused until the player resumes.
        ResetElapsedTime();
        Audio?.ResumeAudio();
        base.OnActivated(sender, args);
    }

    private void InitializeGum()
    {
        var service = Gum.GumService.Default;
        service.Initialize(this, DefaultVisualsVersion.V3);
        service.ContentLoader.XnaContentManager = Content;

        FrameworkElement.KeyboardsForUiControl.Add(service.Keyboard);
        FrameworkElement.GamePadsForUiControl.AddRange(service.Gamepads);
        FrameworkElement.TabReverseKeyCombos.Add(
            new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Up });
        FrameworkElement.TabKeyCombos.Add(
            new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Down });
    }

    private void UpdateDisplay()
    {
        Point screenSize = GraphicsDevice.PresentationParameters.Bounds.Size;
        if (_screenSize == screenSize)
        {
            return;
        }

        _screenSize = screenSize;
        Display.Resize(screenSize.X, screenSize.Y);

        var service = Gum.GumService.Default;
        service.CanvasWidth = GameDisplay.UiWidth;
        service.CanvasHeight = GameDisplay.UiHeight;
        service.Renderer.Camera.Zoom = GameDisplay.UiScale;
        // Gum applies its camera zoom after transforming the screen position.
        service.Cursor.TransformMatrix = Display.ScreenToRaster;
    }
}
