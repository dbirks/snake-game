using UnityEngine;
using UnityEngine.InputSystem;
using SnakeGame.Core;

namespace SnakeGame.Input
{
    /// <summary>
    /// Converts Input System events into simulation InputCommands.
    /// Supports:
    /// - Apple TV Siri Remote (touch surface → dpad)
    /// - MFi gamepads (left stick + dpad)
    /// - Bluetooth controllers (Nintendo Switch Pro, PS5, Xbox — all map to Gamepad)
    /// - Keyboard (WASD) for editor testing
    /// </summary>
    public class InputAdapter : MonoBehaviour
    {
        [SerializeField] private float deadzone = 0.3f;

        private InputCommand _pendingCommand = InputCommand.None;

        private void Awake()
        {
            ConfigureTvOSRemote();
        }

        /// <summary>
        /// Configure Apple TV Siri Remote behavior.
        /// Must be fully playable with Siri Remote alone (Apple requirement).
        /// </summary>
        private void ConfigureTvOSRemote()
        {
#if UNITY_TVOS
            // Allow the Menu button to exit to home (Apple requirement for tvOS)
            UnityEngine.tvOS.Remote.allowExitToHome = true;
            // Use relative swipe values (not absolute touch position)
            UnityEngine.tvOS.Remote.reportAbsoluteDpadValues = false;
            // Enable touch input from the Siri Remote surface
            UnityEngine.tvOS.Remote.touchesEnabled = true;
#endif
        }

        // Called by PlayerInput component via SendMessages
        // Fires for: gamepad left stick, gamepad dpad, Siri Remote swipes, keyboard WASD
        public void OnMove(InputValue value)
        {
            var v = value.Get<Vector2>();
            if (v.x < -deadzone)
                _pendingCommand = InputCommand.TurnLeft;
            else if (v.x > deadzone)
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
