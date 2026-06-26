using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace QiLight.Game.Input;

public enum MoveDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}

public class InputManager
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private GamePadState _previousGamePad;
    private GamePadState _currentGamePad;

    public MoveDirection Direction { get; private set; }
    public bool ActionPressed { get; private set; }
    public bool ActionHeld { get; private set; }
    public bool RetractHeld { get; private set; }
    public bool PausePressed { get; private set; }
    public bool SpeedBoost { get; private set; }
    public bool EnterPressed { get; private set; }
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousGamePad = _currentGamePad;
        _currentKeyboard = Keyboard.GetState();
        _currentGamePad = GamePad.GetState(PlayerIndex.One);

        Direction = MoveDirection.None;

        if (_currentKeyboard.IsKeyDown(Keys.Up) || _currentKeyboard.IsKeyDown(Keys.W) ||
            _currentGamePad.DPad.Up == ButtonState.Pressed)
            Direction = MoveDirection.Up;
        else if (_currentKeyboard.IsKeyDown(Keys.Down) || _currentKeyboard.IsKeyDown(Keys.S) ||
                 _currentGamePad.DPad.Down == ButtonState.Pressed)
            Direction = MoveDirection.Down;
        else if (_currentKeyboard.IsKeyDown(Keys.Left) || _currentKeyboard.IsKeyDown(Keys.A) ||
                 _currentGamePad.DPad.Left == ButtonState.Pressed)
            Direction = MoveDirection.Left;
        else if (_currentKeyboard.IsKeyDown(Keys.Right) || _currentKeyboard.IsKeyDown(Keys.D) ||
                 _currentGamePad.DPad.Right == ButtonState.Pressed)
            Direction = MoveDirection.Right;

        ActionPressed = IsKeyPressed(Keys.Space) ||
                        (_currentGamePad.Buttons.A == ButtonState.Pressed &&
                         _previousGamePad.Buttons.A == ButtonState.Released);

        ActionHeld = _currentKeyboard.IsKeyDown(Keys.Space) ||
                     _currentGamePad.Buttons.A == ButtonState.Pressed;

        RetractHeld = _currentKeyboard.IsKeyDown(Keys.Back) ||
                      _currentGamePad.Buttons.B == ButtonState.Pressed;

        PausePressed = IsKeyPressed(Keys.P) || IsKeyPressed(Keys.Escape) ||
                       (_currentGamePad.Buttons.Start == ButtonState.Pressed &&
                        _previousGamePad.Buttons.Start == ButtonState.Released);

        SpeedBoost = _currentKeyboard.IsKeyDown(Keys.LeftShift) ||
                     _currentGamePad.Triggers.Right > 0.5f;

        EnterPressed = IsKeyPressed(Keys.Enter);

        LeftPressed = IsKeyPressed(Keys.Left) || IsKeyPressed(Keys.A);
        RightPressed = IsKeyPressed(Keys.Right) || IsKeyPressed(Keys.D);
    }

    private bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}
