using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Content;
using MonoGameLibrary.Graphics;

// Explicit GPU suite: build DUI.Desktop first and pass its compiled Content directory.
// This project references only the reusable library, not DUI.Game.
#if !DEBUG
throw new InvalidOperationException("Shader reload tests require a Debug build.");
#else
if (args.Length != 1)
{
    Console.Error.WriteLine("Pass the compiled DUI.Desktop Content directory.");
    return 2;
}

using var game = new ShaderTestGame(Path.GetFullPath(args[0]));
game.Run();
Console.WriteLine("PASS shader reload, invalid XNB recovery, parameter preservation, ImGui and disposal.");
return 0;
#endif

internal sealed class ShaderTestGame : Core
{
    private readonly string _temporary = Path.Combine(Path.GetTempPath(), "dui-shaders-" + Guid.NewGuid());
    private readonly byte[] _original;
    private readonly byte[] _grayscale;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Material _material;
    private Material _otherMaterial;
    private Texture2D _texture;
    private Effect _previous;
    private int _stage;
    private double _nextStage;

    public ShaderTestGame(string contentRoot) : base("MonoGameLibrary shader tests", 800, 600, false)
    {
        _original = File.ReadAllBytes(Path.Combine(contentRoot, "Effects/doubleVisionEffect.xnb"));
        _grayscale = File.ReadAllBytes(Path.Combine(contentRoot, "Effects/grayscaleEffect.xnb"));
        Directory.CreateDirectory(_temporary);
        File.WriteAllBytes(Path.Combine(_temporary, "shader.xnb"), _original);
        Content.RootDirectory = _temporary;
        DebugUiVisible = true;
        ExitOnEscape = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
        _material = Content.WatchMaterial("shader");
        _otherMaterial = Content.WatchMaterial("shader");
        Check(!ReferenceEquals(_material.Effect, _otherMaterial.Effect), "Materials must own independent effects.");
        _material.IsDebugVisible = true;
        _texture = new Texture2D(GraphicsDevice, 8, 8);
        _texture.SetData(new Color[64]);
        _material.SetParameter("GhostTexture", _texture);
        _material.SetParameter("GhostAlpha", 0.37f);
        _material.SetParameter("GhostOffset", new Vector2(-0.2f, 0.3f));
        _material.DebugOverride = true;
        _material.SetParameter("GhostAlpha", 0.8f);
        _material.SetParameter("GhostOffset", Vector2.One);
        VerifyParameters();
        Check(ImGuiRenderer.BindTexture(_texture) == ImGuiRenderer.BindTexture(_texture),
            "Texture previews must reuse their binding.");
        _previous = _material.Effect;
        _nextStage = _clock.Elapsed.TotalSeconds + 0.7;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (_clock.Elapsed.TotalSeconds < _nextStage)
        {
            return;
        }

        switch (_stage)
        {
            case 0:
                ReplaceShader(_original);
                break;
            case 1:
                Check(!ReferenceEquals(_previous, _material.Effect), "The changed XNB must be reloaded.");
                Check(_previous.IsDisposed, "The old effect must be released after copying parameters.");
                VerifyParameters();
                _previous = _material.Effect;
                ReplaceShader([0, 1, 2, 3]);
                break;
            case 2:
                Check(ReferenceEquals(_previous, _material.Effect) && !_previous.IsDisposed,
                    "An invalid XNB must leave the working shader alive.");
                Check(_material.Asset.LastReloadError != null, "Reload failure must be available to the inspector.");
                ReplaceShader(_original);
                break;
            case 3:
                Check(!ReferenceEquals(_previous, _material.Effect) && _material.Asset.LastReloadError == null,
                    "Reload must recover after the next successful build.");
                VerifyParameters();
                ReplaceShader(_grayscale);
                break;
            case 4:
                Check(!_material.TryGetParameter("GhostAlpha", out _) && _material.TryGetParameter("Saturation", out _),
                    "The parameter cache must follow the new shader interface.");
                Check(_material.DebugOverride, "Reload must preserve the developer's override setting.");
                _material.Dispose();
                _material.Dispose();
                Check(_material.Effect.IsDisposed, "Disposal must release the watched shader.");
                Check(!_otherMaterial.Effect.IsDisposed, "Disposal must not affect another material.");
                _otherMaterial.Dispose();
                Material.UpdateWatchedMaterials();
                Exit();
                break;
        }

        _stage++;
        _nextStage = _clock.Elapsed.TotalSeconds + 0.8;
    }

    protected override void DrawScene(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
    }

    protected override void UnloadContent()
    {
        _material?.Dispose();
        _otherMaterial?.Dispose();
        _texture?.Dispose();
        base.UnloadContent();
        Directory.Delete(_temporary, recursive: true);
    }

    private void VerifyParameters()
    {
        Check(Math.Abs(_material.Effect.Parameters["GhostAlpha"].GetValueSingle() - 0.37f) < 0.0001f,
            "Scalar values must survive override and reload.");
        Check(_material.Effect.Parameters["GhostOffset"].GetValueVector2() == new Vector2(-0.2f, 0.3f),
            "Vector values must survive override and reload.");
        Check(ReferenceEquals(_texture, _material.Effect.Parameters["GhostTexture"].GetValueTexture2D()),
            "Texture bindings must survive reload.");
    }

    private void ReplaceShader(byte[] bytes)
    {
        string path = Path.Combine(_temporary, "shader.xnb");
        File.WriteAllBytes(path, bytes);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(_stage + 1));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
