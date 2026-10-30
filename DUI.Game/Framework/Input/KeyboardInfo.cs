using Microsoft.Xna.Framework.Input;

namespace MonoGameLibrary.Input;

public class KeyboardInfo
{
    public KeyboardState PreviousState { get; private set; }
    public KeyboardState CurrentState { get; private set; }

    public KeyboardInfo()
    {
        PreviousState = new KeyboardState();
        CurrentState = Keyboard.GetState();
    }

    public void Update()
    {
        PreviousState = CurrentState;
        CurrentState = Keyboard.GetState();
    }

    public bool IsKeyDown(Keys key) =>
        CurrentState.IsKeyDown(key);

    public bool WasKeyJustPressed(Keys key) =>
        CurrentState.IsKeyDown(key) && PreviousState.IsKeyUp(key);

    public bool WasKeyJustReleased(Keys key) =>
        CurrentState.IsKeyUp(key) && PreviousState.IsKeyDown(key);
}
