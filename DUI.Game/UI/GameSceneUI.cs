using System;
using System.Globalization;
using DUI.GameObjects;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DUI.UI;

public class GameSceneUI : ContainerRuntime
{
    public event EventHandler ResumeButtonClick;
    public event EventHandler PauseButtonClick;
    public event EventHandler RetryButtonClick;

    private static readonly CultureInfo s_scoreCulture = CultureInfo.GetCultureInfo("pl-PL");
    private const float IndicatorSize = 7f;
    private const float IndicatorGap = 3f;
    private const long BlinkHalfPeriodTicks = TimeSpan.TicksPerMillisecond * 300;
    private SoundEffect _uiSoundEffect;
    private Panel _pausePanel;
    private C64Button _resumeButton;
    private Panel _gameOverPanel;
    private C64Button _retryButton;
    private TextRuntime _scoreText;
    private SpriteRuntime _vodkaIndicator;
    private SpriteRuntime _powderIndicator;
    private SpriteRuntime _mushroomIndicator;
    private ScoreStars _scoreStars;
    private C64Button _pauseButton;

    public GameSceneUI()
    {
        Dock(Gum.Wireframe.Dock.Fill);
        this.AddToRoot();

        ContentManager content = Gum.GumService.Default.ContentLoader.XnaContentManager;
        _uiSoundEffect = content.Load<SoundEffect>("Audio/ui");

        var header = new ContainerRuntime();
        header.Dock(Gum.Wireframe.Dock.Fill);
        header.Anchor(Gum.Wireframe.Anchor.TopLeft);
        header.HeightUnits = DimensionUnitType.Absolute;
        header.Height = 12f;
        C64Theme.AddFrame(header, 1f);
        AddChild(header);

        _scoreText = CreateScoreText();
        AddChild(_scoreText);
        _vodkaIndicator = CreateEffectIndicator(content.Load<Texture2D>("Images/UI/vodka-indicator"));
        _powderIndicator = CreateEffectIndicator(content.Load<Texture2D>("Images/UI/powder-indicator"));
        _mushroomIndicator = CreateEffectIndicator(content.Load<Texture2D>("Images/UI/mushroom-indicator"));
        AddChild(_vodkaIndicator);
        AddChild(_powderIndicator);
        AddChild(_mushroomIndicator);
        _scoreStars = new ScoreStars(content.Load<Texture2D>("Images/UI/score-star"));
        AddChild(_scoreStars);
        PositionScoreAccessories();

        _pauseButton = new C64Button();
        _pauseButton.Text = "PAUSE";
        // Fit the 48-pixel shop fascia at the UI camera's 4x zoom.
        _pauseButton.Visual.Height = 10;
        _pauseButton.Anchor(Gum.Wireframe.Anchor.TopRight);
        _pauseButton.Click += OnPauseButtonClicked;
        AddChild(_pauseButton);

        _pausePanel = CreatePausePanel();
        AddChild(_pausePanel.Visual);

        _gameOverPanel = CreateGameOverPanel();
        AddChild(_gameOverPanel.Visual);
    }

    public void UpdateScoreText(decimal scorePermille)
    {
        _scoreText.Text = FormatScore(scorePermille);
        PositionScoreAccessories();
    }

    public void UpdateEffectIndicators(TimeSpan vodkaRemaining, TimeSpan powderRemaining,
        TimeSpan mushroomRemaining)
    {
        SetIndicatorVisibility(_vodkaIndicator, vodkaRemaining, ScreenShake.Duration);
        SetIndicatorVisibility(_powderIndicator, powderRemaining, PowerupEffects.Duration);
        SetIndicatorVisibility(_mushroomIndicator, mushroomRemaining, PowerupEffects.Duration);
    }

    public void UpdateScoreStars(int segmentCount)
    {
        _scoreStars.UpdateScore(segmentCount);
    }

    public void ShowPausePanel()
    {
        _pauseButton.IsVisible = false;
        _pausePanel.IsVisible = true;
        _resumeButton.IsFocused = true;
        _gameOverPanel.IsVisible = false;
    }

    public void HidePausePanel()
    {
        _resumeButton.IsFocused = false;
        _pauseButton.IsVisible = true;
        _pausePanel.IsVisible = false;
    }

    public void ShowGameOverPanel()
    {
        _pauseButton.IsVisible = false;
        _gameOverPanel.IsVisible = true;
        _retryButton.IsFocused = true;
        _pausePanel.IsVisible = false;
    }

    public void HideGameOverPanel()
    {
        _retryButton.IsFocused = false;
        _pauseButton.IsVisible = true;
        _gameOverPanel.IsVisible = false;
    }

    public void Update(GameTime gameTime)
    {
        _scoreText.X = 20;
        _scoreText.Y = 2;
        PositionScoreAccessories();
        _pauseButton.X = -20;
        _pauseButton.Y = 1;
        Gum.GumService.Default.Update(gameTime);
    }

    public void Draw()
    {
        Gum.GumService.Default.Draw();
    }

    private static TextRuntime CreateScoreText()
    {
        TextRuntime text = C64Theme.CreateText(FormatScore(0m));
        text.Anchor(Gum.Wireframe.Anchor.TopLeft);
        text.X = 20f;
        text.Y = 2f;
        return text;
    }

    private static string FormatScore(decimal scorePermille)
    {
        return scorePermille.ToString("F1", s_scoreCulture) + "‰";
    }

    private static SpriteRuntime CreateEffectIndicator(Texture2D texture)
    {
        var indicator = new SpriteRuntime
        {
            Texture = texture,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = IndicatorSize,
            Height = IndicatorSize,
            Color = C64Theme.SymbolColor,
            Visible = false
        };
        indicator.Anchor(Gum.Wireframe.Anchor.TopLeft);
        indicator.Y = 2.5f;
        return indicator;
    }

    private static void SetIndicatorVisibility(SpriteRuntime indicator, TimeSpan remaining, TimeSpan duration)
    {
        // Use the effect's own elapsed time: no duplicate countdown can drift,
        // and refreshing the effect always starts with the icon switched on.
        long elapsedTicks = duration.Ticks - remaining.Ticks;
        indicator.Visible = remaining > TimeSpan.Zero &&
            elapsedTicks / BlinkHalfPeriodTicks % 2 == 0;
    }

    private void PositionScoreAccessories()
    {
        // Reposition immediately when the number changes width (e.g. 9,8→10,0),
        // and again when Update applies the logical HUD position.
        // The C64 font includes the per-mille glyph; icons follow the whole text.
        // Fixed slots preserve vodka / powder / mushroom order while blinking.
        _vodkaIndicator.X = _scoreText.X + _scoreText.AbsoluteWidth + IndicatorGap;
        _powderIndicator.X = _vodkaIndicator.X + IndicatorSize + IndicatorGap;
        _mushroomIndicator.X = _powderIndicator.X + IndicatorSize + IndicatorGap;
        _scoreStars.X = _mushroomIndicator.X + IndicatorSize + IndicatorGap;
        _scoreStars.Y = 2.5f;
    }

    private Panel CreatePausePanel()
    {
        Panel panel = CreatePanel("PAUSED");
        _resumeButton = CreatePanelButton("RESUME");
        _resumeButton.Click += OnResumeButtonClicked;
        _resumeButton.GotFocus += OnElementGotFocus;
        panel.AddChild(_resumeButton);
        return panel;
    }

    private Panel CreateGameOverPanel()
    {
        Panel panel = CreatePanel("GAME OVER");
        _retryButton = CreatePanelButton("RETRY");
        _retryButton.Click += OnRetryButtonClicked;
        _retryButton.GotFocus += OnElementGotFocus;
        panel.AddChild(_retryButton);
        return panel;
    }

    private static Panel CreatePanel(string title)
    {
        var panel = new Panel(new ContainerRuntime());
        panel.Anchor(Gum.Wireframe.Anchor.Center);
        panel.WidthUnits = DimensionUnitType.Absolute;
        panel.HeightUnits = DimensionUnitType.Absolute;
        panel.Width = 240f;
        panel.Height = 70f;
        panel.IsVisible = false;
        C64Theme.AddFrame(panel.Visual, 2f);

        TextRuntime text = C64Theme.CreateText(title, 2f);
        text.Anchor(Gum.Wireframe.Anchor.Top);
        text.Y = 12f;
        panel.AddChild(text);
        return panel;
    }

    private static C64Button CreatePanelButton(string text)
    {
        var button = new C64Button { Text = text };
        button.Anchor(Gum.Wireframe.Anchor.Bottom);
        button.Y = -10f;
        return button;
    }

    private void OnResumeButtonClicked(object sender, EventArgs e)
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        HidePausePanel();
        ResumeButtonClick?.Invoke(this, e);
    }

    private void OnRetryButtonClicked(object sender, EventArgs e)
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        HideGameOverPanel();
        RetryButtonClick?.Invoke(this, e);
    }

    private void OnPauseButtonClicked(object sender, EventArgs e)
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        PauseButtonClick?.Invoke(this, e);
    }

    private void OnElementGotFocus(object sender, EventArgs e)
    {
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
    }

}
