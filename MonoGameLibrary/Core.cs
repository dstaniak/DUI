using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Audio;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.ImGuiIntegration;
using MonoGameLibrary.Input;
using MonoGameLibrary.Scenes;

namespace MonoGameLibrary;

public class Core : Game
{
    internal static Core s_instance;
    private static Scene s_activeScene;
    private static Scene s_nextScene;

    public static Core Instance => s_instance;
    public static GraphicsDeviceManager Graphics { get; private set; }
    public static new GraphicsDevice GraphicsDevice { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public static new ContentManager Content { get; private set; }
    public static InputManager Input { get; private set; }
    public static bool ExitOnEscape { get; set; }
    public static AudioController Audio { get; private set; }
    public static ImGuiRenderer ImGuiRenderer { get; private set; }
    public bool DebugUiVisible { get; set; }
    public static bool DebugWantsMouse => ImGuiRenderer?.WantsMouse ?? false;
    public static bool DebugWantsKeyboard => ImGuiRenderer?.WantsKeyboard ?? false;

    public Core(string title, int width, int height, bool fullScreen)
    {
        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only a single Core instance can be created");
        }

        s_instance = this;

        Graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = width,
            PreferredBackBufferHeight = height,
            IsFullScreen = fullScreen
        };
        Window.Title = title;

        Content = base.Content;
        Content.RootDirectory = "Content";

        IsMouseVisible = true;
        ExitOnEscape = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        GraphicsDevice = base.GraphicsDevice;
        SpriteBatch = new(GraphicsDevice);
        Input = new();
        Audio = new();
#if DEBUG
        ImGuiRenderer = new(this);
        ImGuiRenderer.RebuildFontAtlas();
#endif
    }

    protected override void UnloadContent()
    {
        s_activeScene?.Dispose();
        s_activeScene = null;
        s_nextScene = null;
        Audio.Dispose();
        SpriteBatch.Dispose();
        ImGuiRenderer?.Dispose();
        ImGuiRenderer = null;
        s_instance = null;
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
        Audio.Update();

#if DEBUG
        if (Input.Keyboard.WasKeyJustPressed(Keys.F12))
        {
            DebugUiVisible = !DebugUiVisible;
        }

        Material.UpdateWatchedMaterials();
        ImGuiRenderer.BeforeLayout(gameTime, DebugUiVisible);
#endif

        if (ExitOnEscape && !DebugWantsKeyboard && Input.Keyboard.WasKeyJustPressed(Keys.Escape))
        {
            Exit();
        }

        if (s_nextScene != null)
        {
            TransitionScene();
        }

        if (s_activeScene != null && IsActive)
        {
            s_activeScene.Update(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawScene(gameTime);
        base.Draw(gameTime);
#if DEBUG
        if (DebugUiVisible)
        {
            Material.DrawVisibleDebugUi();
        }

        ImGuiRenderer.AfterLayout();
#endif
    }

    /// <summary>Draws the game before the unfiltered desktop debug overlay.</summary>
    protected virtual void DrawScene(GameTime gameTime)
    {
        if (s_activeScene != null)
        {
            s_activeScene.Draw(gameTime);
        }
    }

    public static void ChangeScene(Scene next)
    {
        if (s_activeScene != next)
        {
            s_nextScene = next;
        }
    }

    private static void TransitionScene()
    {
        if (s_activeScene != null)
        {
            s_activeScene.Dispose();
        }

        s_activeScene = s_nextScene;
        s_nextScene = null;

        if (s_activeScene != null)
        {
            s_activeScene.Initialize();
        }
    }
}
