using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace _4DND;

public class InputManager
{
    public KeyboardState CurrentKeyboardState { get; private set; }
    public KeyboardState PreviousKeyboardState { get; private set; }
    public MouseState CurrentMouseState { get; private set; }
    public MouseState PreviousMouseState { get; private set; }

    public void Update()
    {
        PreviousKeyboardState = CurrentKeyboardState;
        CurrentKeyboardState = Keyboard.GetState();

        PreviousMouseState = CurrentMouseState;
        CurrentMouseState = Mouse.GetState();
    }

    public bool IsKeyPressed(Keys key) => CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);
    public bool IsKeyDown(Keys key) => CurrentKeyboardState.IsKeyDown(key);

    public bool IsLeftClick() => CurrentMouseState.LeftButton == ButtonState.Pressed && PreviousMouseState.LeftButton == ButtonState.Released;
    public bool IsLeftButtonDown() => CurrentMouseState.LeftButton == ButtonState.Pressed;

    public bool IsRightClick() => CurrentMouseState.RightButton == ButtonState.Pressed && PreviousMouseState.RightButton == ButtonState.Released;

    public int ScrollDelta => CurrentMouseState.ScrollWheelValue - PreviousMouseState.ScrollWheelValue;

    public Point MousePosition => CurrentMouseState.Position;
}
