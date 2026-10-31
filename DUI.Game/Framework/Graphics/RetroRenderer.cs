using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

/// <summary>Owns the 720p frame buffer and presents pixel artwork without smoothing or color reduction.</summary>
public sealed class RetroRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RenderTarget2D _scene;

    public RetroRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _scene = new RenderTarget2D(graphicsDevice, GameDisplay.RasterWidth, GameDisplay.RasterHeight);
    }

    public void BeginFrame()
    {
        _graphicsDevice.SetRenderTarget(_scene);
        _graphicsDevice.Clear(Color.Black);
    }

    public void Present(SpriteBatch spriteBatch, GameDisplay display)
    {
        _graphicsDevice.SetRenderTarget(null);
        spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_scene, display.OutputBounds, Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _scene.Dispose();
    }
}
