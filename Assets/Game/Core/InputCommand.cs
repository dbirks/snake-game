using System;

namespace SnakeGame.Core
{
    /// <summary>
    /// Input state for the snake simulation each tick.
    /// Supports both analog stick (360-degree) and discrete dpad (left/right) input.
    /// </summary>
    [Serializable]
    public struct InputCommand
    {
        /// <summary>
        /// Analog stick direction. Zero vector means no input.
        /// X = horizontal (-1 left, +1 right), Y = vertical (-1 down, +1 up).
        /// </summary>
        public float StickX;
        public float StickY;

        public bool HasAnalogInput => StickX * StickX + StickY * StickY > 0.09f; // deadzone 0.3

        public static readonly InputCommand None = new InputCommand();

        public static InputCommand FromStick(float x, float y) =>
            new InputCommand { StickX = x, StickY = y };

        public static InputCommand TurnLeft =>
            new InputCommand { StickX = -1f, StickY = 0f };

        public static InputCommand TurnRight =>
            new InputCommand { StickX = 1f, StickY = 0f };
    }
}
