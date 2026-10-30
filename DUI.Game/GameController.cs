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
    private static int? s_touchId;
    private static Vector2 s_swipeDirection;

    private static KeyboardInfo s_keyboard => Core.Input.Keyboard;
    private static GamePadInfo s_gamepad => Core.Input.GamePads[(int)PlayerIndex.One];

    public static void Update()
    {
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
        Vector2 position = Core.Display.ToGamePosition(mouse.Position.ToVector2());
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

    public static Vector2 ReadDirection()
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

        return s_swipeDirection;
    }

    public static void ResetPointer()
    {
        s_swipe.Reset();
        s_touchId = null;
        s_swipeDirection = Vector2.Zero;
    }

    private static void UpdateTouch(TouchCollection touches)
    {
        foreach (TouchLocation touch in touches)
        {
            Vector2 position = Core.Display.ToGamePosition(touch.Position);
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

    public static bool MoveUp()
    {
        return
            s_keyboard.WasKeyJustPressed(Keys.Up) ||
            s_keyboard.WasKeyJustPressed(Keys.W) ||
            s_gamepad.WasButtonJustPressed(Buttons.DPadUp) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickUp);
    }

    public static bool MoveDown()
    {
        return
            s_keyboard.WasKeyJustPressed(Keys.Down) ||
            s_keyboard.WasKeyJustPressed(Keys.S) ||
            s_gamepad.WasButtonJustPressed(Buttons.DPadDown) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickDown);
    }

    public static bool MoveLeft()
    {
        return
            s_keyboard.WasKeyJustPressed(Keys.Left) ||
            s_keyboard.WasKeyJustPressed(Keys.A) ||
            s_gamepad.WasButtonJustPressed(Buttons.DPadLeft) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickLeft);
    }

    public static bool MoveRight()
    {
        return
            s_keyboard.WasKeyJustPressed(Keys.Right) ||
            s_keyboard.WasKeyJustPressed(Keys.D) ||
            s_gamepad.WasButtonJustPressed(Buttons.DPadRight) ||
            s_gamepad.WasButtonJustPressed(Buttons.LeftThumbstickRight);
    }

    public static bool Pause()
    {
        return
            s_keyboard.WasKeyJustPressed(Keys.Escape) ||
            s_gamepad.WasButtonJustPressed(Buttons.Start) ||
            s_gamepad.WasButtonJustPressed(Buttons.Back);
    }
}
