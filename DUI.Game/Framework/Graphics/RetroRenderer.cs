using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

/// <summary>Owns the 720p frame buffer and applies full-view double vision and rainbow effects.</summary>
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
    private readonly Effect _doubleVisionEffect;
    private readonly Effect _rainbowEffect;

    public RetroRenderer(GraphicsDevice graphicsDevice, Effect doubleVisionEffect, Effect rainbowEffect)
    {
        _graphicsDevice = graphicsDevice;
        _scene = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);
        // The game's ContentManager owns both effects; this renderer owns only the buffer.
        _doubleVisionEffect = doubleVisionEffect;
        _rainbowEffect = rainbowEffect;
        _doubleVisionEffect.Parameters["GhostTexture"].SetValue(_scene);
    }

    public void BeginFrame()
    {
        _graphicsDevice.SetRenderTarget(_scene);
        _graphicsDevice.Clear(Color.Black);
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
            _rainbowEffect.Parameters["Phase"].SetValue(phase);
            // A translucent color-only pass overlays the already composed view,
            // including UI, without another render target or texture sampling.
            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, effect: _rainbowEffect);
            spriteBatch.Draw(_scene, display.OutputBounds, Color.White);
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        _scene.Dispose();
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
        _doubleVisionEffect.Parameters["GhostOffset"].SetValue(offset);
        float alpha = Math.Min(MaxGhostAlpha, BaseGhostAlpha + (float)scorePermille * AlphaPerPermille);
        _doubleVisionEffect.Parameters["GhostAlpha"].SetValue(alpha);
        return _doubleVisionEffect;
    }
}
