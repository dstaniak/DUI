using System;
using System.Globalization;
using Gum.DataTypes;
using Gum.Managers;
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
    private SoundEffect _uiSoundEffect;
    private Panel _pausePanel;
    private AnimatedButton _resumeButton;
    private Panel _gameOverPanel;
    private AnimatedButton _retryButton;
    private TextRuntime _scoreText;
    private SpriteRuntime _perMilleSymbol;
    private AnimatedButton _pauseButton;

    public GameSceneUI()
    {
        Dock(Gum.Wireframe.Dock.Fill);
        this.AddToRoot();

        ContentManager content = Gum.GumService.Default.ContentLoader.XnaContentManager;
        _uiSoundEffect = content.Load<SoundEffect>("Audio/ui");
        TextureAtlas atlas = TextureAtlas.FromFile(content, "Images/atlas-definition.xml");

        _scoreText = CreateScoreText();
        AddChild(_scoreText);
        _perMilleSymbol = CreatePerMilleSymbol(content.Load<Texture2D>("Images/per-mille"));
        AddChild(_perMilleSymbol);
        PositionPerMilleSymbol();

        _pauseButton = new AnimatedButton(atlas);
        _pauseButton.Text = "PAUSE";
        // Fit the 48-pixel shop fascia at the UI camera's 4x zoom.
        _pauseButton.Visual.Height = 10;
        _pauseButton.Anchor(Gum.Wireframe.Anchor.TopRight);
        _pauseButton.Click += OnPauseButtonClicked;
        AddChild(_pauseButton);

        _pausePanel = CreatePausePanel(atlas);
        AddChild(_pausePanel.Visual);

        _gameOverPanel = CreateGameOverPanel(atlas);
        AddChild(_gameOverPanel.Visual);
    }

    public void UpdateScoreText(decimal scorePermille)
    {
        _scoreText.Text = FormatScore(scorePermille);
        PositionPerMilleSymbol();
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
        _scoreText.Y = 4;
        PositionPerMilleSymbol();
        _pauseButton.X = -20;
        _pauseButton.Y = 1;
        Gum.GumService.Default.Update(gameTime);
    }

    public void Draw()
    {
        Gum.GumService.Default.Draw();
    }

    private TextRuntime CreateScoreText()
    {
        TextRuntime text = new();
        text.Anchor(Gum.Wireframe.Anchor.TopLeft);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.X = 20.0f;
        text.Y = 4.0f;
        text.UseCustomFont = true;
        text.CustomFontFile = "Fonts/04b_30.fnt";
        text.FontScale = 0.25f;
        text.Text = FormatScore(0m);

        return text;
    }

    private static string FormatScore(decimal scorePermille)
    {
        return scorePermille.ToString("F1", s_scoreCulture);
    }

    private static SpriteRuntime CreatePerMilleSymbol(Texture2D texture)
    {
        // The font has no U+2030 glyph. Draw the complete sign as one sprite
        // at the same quarter-size UI scale as the numeric text.
        var symbol = new SpriteRuntime
        {
            Texture = texture,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = texture.Width * 0.25f,
            Height = texture.Height * 0.25f
        };
        symbol.Anchor(Gum.Wireframe.Anchor.TopLeft);
        return symbol;
    }

    private void PositionPerMilleSymbol()
    {
        // Reposition immediately when the number changes width (e.g. 9,8→10,0),
        // and again when Update applies the logical HUD position.
        _perMilleSymbol.X = _scoreText.X + _scoreText.AbsoluteWidth + 1;
        _perMilleSymbol.Y = _scoreText.Y + 0.5f;
    }

    private Panel CreatePausePanel(TextureAtlas atlas)
    {
        Panel panel = new Panel();
        panel.Anchor(Gum.Wireframe.Anchor.Center);
        panel.WidthUnits = DimensionUnitType.Absolute;
        panel.HeightUnits = DimensionUnitType.Absolute;
        panel.Width = 264.0f;
        panel.Height = 70.0f;
        panel.IsVisible = false;

        TextureRegion backgroundRegion = atlas.GetRegion("panel-background");

        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Texture = backgroundRegion.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = backgroundRegion.Height;
        background.TextureWidth = backgroundRegion.Width;
        background.TextureTop = backgroundRegion.SourceRectangle.Top;
        background.TextureLeft = backgroundRegion.SourceRectangle.Left;
        panel.AddChild(background);

        TextRuntime text = new TextRuntime();
        text.Text = "PAUSED";
        text.UseCustomFont = true;
        text.CustomFontFile = "Fonts/04b_30.fnt";
        text.FontScale = 0.5f;
        text.X = 10.0f;
        text.Y = 10.0f;
        panel.AddChild(text);

        _resumeButton = new AnimatedButton(atlas);
        _resumeButton.Text = "RESUME";
        _resumeButton.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        _resumeButton.X = 9.0f;
        _resumeButton.Y = -9.0f;

        _resumeButton.Click += OnResumeButtonClicked;
        _resumeButton.GotFocus += OnElementGotFocus;

        panel.AddChild(_resumeButton);

        return panel;
    }

    private Panel CreateGameOverPanel(TextureAtlas atlas)
    {
        Panel panel = new Panel();
        panel.Anchor(Gum.Wireframe.Anchor.Center);
        panel.WidthUnits = DimensionUnitType.Absolute;
        panel.HeightUnits = DimensionUnitType.Absolute;
        panel.Width = 264.0f;
        panel.Height = 70.0f;
        panel.IsVisible = false;

        TextureRegion backgroundRegion = atlas.GetRegion("panel-background");

        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Texture = backgroundRegion.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = backgroundRegion.Height;
        background.TextureWidth = backgroundRegion.Width;
        background.TextureTop = backgroundRegion.SourceRectangle.Top;
        background.TextureLeft = backgroundRegion.SourceRectangle.Left;
        panel.AddChild(background);

        TextRuntime text = new TextRuntime();
        text.Text = "GAME OVER";
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.UseCustomFont = true;
        text.CustomFontFile = "Fonts/04b_30.fnt";
        text.FontScale = 0.5f;
        text.X = 10.0f;
        text.Y = 10.0f;
        panel.AddChild(text);

        _retryButton = new AnimatedButton(atlas);
        _retryButton.Text = "RETRY";
        _retryButton.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        _retryButton.X = 9.0f;
        _retryButton.Y = -9.0f;

        _retryButton.Click += OnRetryButtonClicked;
        _retryButton.GotFocus += OnElementGotFocus;

        panel.AddChild(_retryButton);

        return panel;
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
