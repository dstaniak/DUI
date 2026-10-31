using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

/// <summary>Owns the low-resolution buffers; restricts scene and UI colors to the C64 palette before scaling.</summary>
public sealed class C64Renderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderTarget2D _scene;
    private readonly RenderTarget2D _indexedFrame;
    private readonly Effect _paletteEffect;

    public C64Renderer(GraphicsDevice graphicsDevice, Effect paletteEffect)
    {
        _graphicsDevice = graphicsDevice;
        _paletteEffect = paletteEffect;
        _scene = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);
        _indexedFrame = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);

        var colors = new Vector3[C64Palette.Colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = C64Palette.Colors[i].ToVector3();
        }
        _paletteEffect.Parameters["Palette"].SetValue(colors);
    }

    public void BeginFrame()
    {
        _graphicsDevice.SetRenderTarget(_scene);
        _graphicsDevice.Clear(C64Palette.LightBlue);
    }

    public void Present(SpriteBatch spriteBatch, GameDisplay display)
    {
        // Quantize only 320x200 pixels, including alpha blends and the UI.
        // Running this pass after upscaling would multiply its GPU cost.
        _graphicsDevice.SetRenderTarget(_indexedFrame);
        spriteBatch.Begin(blendState: BlendState.Opaque,
            samplerState: SamplerState.PointClamp, effect: _paletteEffect);
        spriteBatch.Draw(_scene, Vector2.Zero, Color.White);
        spriteBatch.End();

        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(C64Palette.Blue);
        spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_indexedFrame, display.OutputBounds, Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _scene.Dispose();
        _indexedFrame.Dispose();
        // The effect is owned by the game's ContentManager.
    }
}
