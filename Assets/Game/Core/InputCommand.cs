namespace SnakeGame.Core
{
    /// <summary>
    /// Commands that can be issued to the snake simulation each tick.
    /// </summary>
    public enum InputCommand
    {
        None,
        TurnLeft,
        TurnRight,
        Accelerate
    }
}
