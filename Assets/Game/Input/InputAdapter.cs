using UnityEngine;
using UnityEngine.InputSystem;
using SnakeGame.Core;

namespace SnakeGame.Input
{
    /// <summary>
    /// Converts Input System events into simulation InputCommands.
    /// Supports full 360-degree analog stick input:
    /// - Gamepad left stick / dpad → full analog direction
    /// - Siri Remote touch surface → dpad-style direction
    /// - Keyboard WASD → 8-way direction
    /// </summary>
    public class InputAdapter : MonoBehaviour
    {
        [SerializeField] private float deadzone = 0.3f;

        private InputCommand _pendingCommand = InputCommand.None;

        private void Awake()
        {
            ConfigureTvOSRemote();
        }

        private void ConfigureTvOSRemote()
        {
#if UNITY_TVOS
            UnityEngine.tvOS.Remote.allowExitToHome = true;
            UnityEngine.tvOS.Remote.reportAbsoluteDpadValues = false;
            UnityEngine.tvOS.Remote.touchesEnabled = true;
#endif
        }

        // Called by PlayerInput via SendMessages — receives gamepad stick,
        // dpad, Siri Remote swipes, and keyboard WASD
        public void OnMove(InputValue value)
        {
            var v = value.Get<Vector2>();
            if (v.sqrMagnitude > deadzone * deadzone)
                _pendingCommand = InputCommand.FromStick(v.x, v.y);
            else
                _pendingCommand = InputCommand.None;
        }

        public InputCommand ConsumeCommand()
        {
            var cmd = _pendingCommand;
            // Don't reset — keep steering while stick is held
            return cmd;
        }
    }
}
