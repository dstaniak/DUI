using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using DUI.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;

namespace DUI;

public static class GameController
{
    private static readonly SwipeTracker s_swipe = new();
    private static readonly KeyboardSteering s_steering = new();
    private static GameWindow s_window;
    private static Vector2 s_fallbackDirection;
    private static int? s_touchId;
    private static Vector2 s_swipeDirection;

    private static KeyboardInfo s_keyboard => Core.Input.Keyboard;
    private static GamePadInfo s_gamepad => Core.Input.GamePads[(int)PlayerIndex.One];

    public static void Initialize(GameWindow window)
    {
        Shutdown();
        s_window = window;
        s_window.KeyDown += OnKeyDown;
        s_window.KeyUp += OnKeyUp;
        ResetInput();
    }

    public static void Shutdown()
    {
        if (s_window != null)
        {
            s_window.KeyDown -= OnKeyDown;
            s_window.KeyUp -= OnKeyUp;
            s_window = null;
        }
    }

    public static void Update()
    {
        if (Core.DebugWantsKeyboard)
        {
            s_steering.Reset(Keyboard.GetState());
        }

        UpdatePointer();
        s_fallbackDirection = ReadPadDirection();
        if (s_fallbackDirection == Vector2.Zero)
        {
            s_fallbackDirection = s_swipeDirection;
        }
    }

    public static bool TryReadDirection(out Vector2 direction)
    {
        if (s_steering.TryReadDirection(out direction))
        {
            return true;
        }

        direction = s_fallbackDirection;
        s_fallbackDirection = Vector2.Zero;
        return direction != Vector2.Zero;
    }

    public static void ResetInput()
    {
        s_steering.Reset(Keyboard.GetState());
        s_fallbackDirection = Vector2.Zero;
        ResetPointer();
    }

    private static void OnKeyDown(object sender, InputKeyEventArgs args)
    {
        if (Core.DebugWantsKeyboard)
        {
            return;
        }

        s_steering.KeyDown(args.Key);
    }

    private static void OnKeyUp(object sender, InputKeyEventArgs args)
    {
        s_steering.KeyUp(args.Key);
    }

    private static void UpdatePointer()
    {
        if (Core.DebugWantsMouse)
        {
            ResetPointer();
            return;
        }

        s_swipeDirection = Vector2.Zero;
        TouchCollection touches = TouchPanel.GetState();
        if (touches.Count > 0)
        {
            UpdateTouch(touches);
            return;
        }

        if (s_touchId.HasValue)
        {
            ResetPointer();
        }

        var mouse = Core.Input.Mouse;
        Vector2 position = DuiGame.Display.ToGamePosition(mouse.Position.ToVector2());
        if (mouse.WasButtonJustPressed(MouseButton.Left))
        {
            s_swipe.Begin(position);
        }

        if (mouse.IsButtonDown(MouseButton.Left) || mouse.WasButtonJustReleased(MouseButton.Left))
        {
            s_swipeDirection = s_swipe.Move(position);
        }

        if (mouse.WasButtonJustReleased(MouseButton.Left))
        {
            s_swipe.Reset();
        }
    }

    private static Vector2 ReadPadDirection()
    {
        if (MoveUp())
        {
            return -Vector2.UnitY;
        }

        if (MoveDown())
        {
            return Vector2.UnitY;
        }

        if (MoveLeft())
        {
            return -Vector2.UnitX;
        }

        if (MoveRight())
        {
            return Vector2.UnitX;
        }

        return Vector2.Zero;
    }

    private static void ResetPointer()
    {
        s_swipe.Reset();
        s_touchId = null;
        s_swipeDirection = Vector2.Zero;
    }

    private static void UpdateTouch(TouchCollection touches)
    {
        foreach (TouchLocation touch in touches)
        {
            Vector2 position = DuiGame.Display.ToGamePosition(touch.Position);
            if (touch.State == TouchLocationState.Pressed && !s_touchId.HasValue)
            {
                s_touchId = touch.Id;
                s_swipe.Begin(position);
            }

            if (touch.Id != s_touchId)
            {
                continue;
            }

            s_swipeDirection = s_swipe.Move(position);
            if (touch.State == TouchLocationState.Released || touch.State == TouchLocationState.Invalid)
            {
                s_swipe.Reset();
                s_touchId = null;
            }
        }
    }

    private static bool MoveUp()
    {
        return
            s_gamepad.WasButtonJustPressed(Buttons.DPadUp) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickUp);
    }

    private static bool MoveDown()
    {
        return
            s_gamepad.WasButtonJustPressed(Buttons.DPadDown) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickDown);
    }

    private static bool MoveLeft()
    {
        return
            s_gamepad.WasButtonJustPressed(Buttons.DPadLeft) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickLeft);
    }

    private static bool MoveRight()
    {
        return
            s_gamepad.WasButtonJustPressed(Buttons.DPadRight) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickRight);
    }

    public static bool Pause()
    {
        return
            (!Core.DebugWantsKeyboard && s_keyboard.WasKeyJustPressed(Keys.Escape)) ||
            s_gamepad.WasButtonJustPressed(Buttons.Start) ||
            s_gamepad.WasButtonJustPressed(Buttons.Back);
    }
}
