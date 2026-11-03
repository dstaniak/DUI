using System;
using DUI.Graphics;
using DUI.Scenes;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Content;

namespace DUI;

public sealed class DuiGame : Core
{
    public static GameDisplay Display { get; } = new();
    private GameScene _gameScene;
    private SoundEffect _themeSong;
    private Point _screenSize;
    private RetroRenderer _renderer;

    public DuiGame() : base("DUI — Snake", 1280, 720, false)
    {
        // The collectible moves 0.5 pixels per update; keep its 60 Hz pace
        // regardless of the display refresh rate.
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
    }

    protected override void Initialize()
    {
        base.Initialize();
        GameController.Initialize(Window);
        InitializeGum();
        _renderer = new RetroRenderer(GraphicsDevice,
            Content.WatchMaterial("Effects/doubleVisionEffect"),
            Content.WatchMaterial("Effects/rainbowEffect"));
        UpdateDisplay();
        Audio.SongVolume = 0.35f;
        Audio.PlaySong(_themeSong);
        _gameScene = new GameScene();
        ChangeScene(_gameScene);
    }

    protected override void LoadContent()
    {
        _themeSong = Content.Load<SoundEffect>("Audio/theme");
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateDisplay();
        base.Update(gameTime);
        // The scene has now processed collection, expiry and RETRY this frame.
        Audio.SongPlaybackRate = _gameScene.MovementSpeedMultiplier;
    }

    protected override void DrawScene(GameTime gameTime)
    {
        _renderer.BeginFrame();
        base.DrawScene(gameTime);
        _renderer.BeginUiFrame();
        _gameScene.DrawUI();
        _renderer.Present(SpriteBatch, Display, _gameScene.ScorePermille, _gameScene.PlayTime,
            _gameScene.VisualEffectsSuppressed, _gameScene.RainbowActive);
    }

    protected override void UnloadContent()
    {
        GameController.Shutdown();
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
        GameController.ResetInput();
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
