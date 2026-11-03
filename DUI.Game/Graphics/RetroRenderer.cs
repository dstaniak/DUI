using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace DUI.Graphics;

/// <summary>Composes an affected world and a separate, unfiltered UI layer at 720p.</summary>
public sealed class RetroRenderer : IDisposable
{
    private const double OrbitPeriodSeconds = 8.0;
    private const double RainbowPeriodSeconds = 6.0;
    private const float BaseGhostAlpha = 0.1f;
    private const float MaxGhostAlpha = 0.5f;
    // Each 0.1 per mille adds 0.01 to the copy's opacity.
    private const float AlphaPerPermille = 0.1f;
    // At 720p, each per mille adds 12 pixels to the copy's orbit radius.
    private const float RadiusPerPermille = 12.0f / GameDisplay.RasterHeight;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderTarget2D _scene;
    private readonly RenderTarget2D _ui;
    private readonly Material _doubleVisionEffect;
    private readonly Material _rainbowEffect;

    public RetroRenderer(GraphicsDevice graphicsDevice, Material doubleVisionEffect, Material rainbowEffect)
    {
        _graphicsDevice = graphicsDevice;
        _scene = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);
        _ui = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);
        // This renderer owns both watched materials and both frame buffers.
        _doubleVisionEffect = doubleVisionEffect;
        _rainbowEffect = rainbowEffect;
        _doubleVisionEffect.SetParameter("GhostTexture", _scene);
        _doubleVisionEffect.IsDebugVisible = true;
        _rainbowEffect.IsDebugVisible = true;
    }

    public void BeginFrame()
    {
        _graphicsDevice.SetRenderTarget(_scene);
        _graphicsDevice.Clear(Color.Black);
    }

    public void BeginUiFrame()
    {
        // Keep Gum's 720p layout and cursor mapping at every window size.
        _graphicsDevice.SetRenderTarget(_ui);
        _graphicsDevice.Clear(Color.Transparent);
    }

    public void Present(SpriteBatch spriteBatch, GameDisplay display, decimal scorePermille,
        TimeSpan playTime, bool visualEffectsSuppressed, bool rainbowActive)
    {
        _graphicsDevice.SetRenderTarget(null);
        Effect effect = visualEffectsSuppressed ? null :
            PrepareDoubleVision(display.OutputBounds, scorePermille, playTime);
        spriteBatch.Begin(
            blendState: BlendState.Opaque,
            samplerState: SamplerState.PointClamp,
            effect: effect);
        spriteBatch.Draw(_scene, display.OutputBounds, Color.White);
        spriteBatch.End();

        if (rainbowActive && !visualEffectsSuppressed)
        {
            float phase = (float)(playTime.TotalSeconds % RainbowPeriodSeconds / RainbowPeriodSeconds);
            _rainbowEffect.SetParameter("Phase", phase);
            // A translucent color-only pass overlays the world before the UI.
            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: _rainbowEffect.Effect);
            spriteBatch.Draw(_scene, display.OutputBounds, Color.White);
            spriteBatch.End();
        }

        // Gum's transparent render target contains premultiplied colors.
        // Composite it last so no gameplay shader or shake can affect UI.
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_ui, display.OutputBounds, Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _doubleVisionEffect.Dispose();
        _rainbowEffect.Dispose();
        _scene.Dispose();
        _ui.Dispose();
    }

    private Effect PrepareDoubleVision(Rectangle outputBounds, decimal scorePermille, TimeSpan playTime)
    {
        if (scorePermille <= 0m)
        {
            return null;
        }

        double angle = playTime.TotalSeconds % OrbitPeriodSeconds / OrbitPeriodSeconds * Math.Tau;
        float radius = (float)scorePermille * RadiusPerPermille *
            Math.Min(outputBounds.Width, outputBounds.Height);

        // Normalize screen-space offsets separately so the orbit stays circular
        // even when the game view is stretched to a different aspect ratio.
        Vector2 offset = new(
            radius * (float)Math.Cos(angle) / outputBounds.Width,
            radius * (float)Math.Sin(angle) / outputBounds.Height);
        _doubleVisionEffect.SetParameter("GhostOffset", offset);
        float alpha = Math.Min(MaxGhostAlpha, BaseGhostAlpha + (float)scorePermille * AlphaPerPermille);
        _doubleVisionEffect.SetParameter("GhostAlpha", alpha);
        return _doubleVisionEffect.Effect;
    }
}
