using UnityEngine;
using UnityEngine.InputSystem;
using SnakeGame.Core;

namespace SnakeGame.Input
{
    /// <summary>
    /// Converts Input System events into simulation InputCommands.
    /// Supports Apple TV remote swipe gestures and gamepad sticks.
    /// </summary>
    public class InputAdapter : MonoBehaviour
    {
        private InputCommand _pendingCommand = InputCommand.None;
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        // Called by PlayerInput component via SendMessages or UnityEvents
        public void OnMove(InputValue value)
        {
            var v = value.Get<Vector2>();
            if (v.x < -0.3f)
                _pendingCommand = InputCommand.TurnLeft;
            else if (v.x > 0.3f)
                _pendingCommand = InputCommand.TurnRight;
        }

        /// <summary>
        /// Consume and reset the pending command. Called once per simulation tick.
        /// </summary>
        public InputCommand ConsumeCommand()
        {
            var cmd = _pendingCommand;
            _pendingCommand = InputCommand.None;
            return cmd;
        }
    }
}
