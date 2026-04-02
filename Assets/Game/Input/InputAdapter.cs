using UnityEngine;
using SnakeGame.Core;

namespace SnakeGame.Input
{
    /// <summary>
    /// Reads input using the old Input Manager (Input.GetAxis).
    /// Supports multiple players via joystick index.
    ///
    /// Player 1: default axes (Horizontal/Vertical) + keyboard
    /// Player 2: joystick-specific axes (Joy2 Axis X / Joy2 Axis Y)
    /// </summary>
    public class InputAdapter : MonoBehaviour
    {
        [SerializeField] private float deadzone = 0.3f;
        private int _playerIndex = 1;
        private InputCommand _pendingCommand;
        private int _logCount;

        public void SetPlayerIndex(int index) { _playerIndex = index; }

        private void Awake()
        {
#if UNITY_TVOS
            UnityEngine.tvOS.Remote.allowExitToHome = true;
            UnityEngine.tvOS.Remote.reportAbsoluteDpadValues = false;
            UnityEngine.tvOS.Remote.touchesEnabled = true;
#endif
        }

        private void Update()
        {
            float h, v;

            if (_playerIndex == 1)
            {
                // Player 1: default axes (Siri Remote + first gamepad + keyboard)
                h = UnityEngine.Input.GetAxis("Horizontal");
                v = UnityEngine.Input.GetAxis("Vertical");
            }
            else
            {
                // Player 2: read second joystick directly
                // Joystick axes are named "joystick N axis X" where N=1-based
                h = UnityEngine.Input.GetAxis($"Joy{_playerIndex} Axis 1");
                v = UnityEngine.Input.GetAxis($"Joy{_playerIndex} Axis 2");
            }

            if (_logCount < 5 && (Mathf.Abs(h) > deadzone || Mathf.Abs(v) > deadzone))
            {
                Debug.Log($"[InputAdapter P{_playerIndex}] h={h:F2} v={v:F2}");
                _logCount++;
            }

            if (h * h + v * v > deadzone * deadzone)
                _pendingCommand = InputCommand.FromStick(h, v);
            else
                _pendingCommand = InputCommand.None;
        }

        public InputCommand ConsumeCommand()
        {
            return _pendingCommand;
        }
    }
}
