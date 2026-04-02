using UnityEngine;
using SnakeGame.Core;

namespace SnakeGame.Input
{
    /// <summary>
    /// Reads input using the OLD Input Manager (Input.GetAxis).
    /// This is the simplest and most reliable approach for tvOS —
    /// the Siri Remote touchpad and MFi/Bluetooth gamepads are
    /// automatically mapped without any configuration.
    ///
    /// No Input System package needed. No PlayerSettings changes needed.
    /// It just works.
    /// </summary>
    public class InputAdapter : MonoBehaviour
    {
        [SerializeField] private float deadzone = 0.3f;

        private InputCommand _pendingCommand;
        private int _logCount;

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
            // Old Input Manager — works on tvOS out of the box
            // Siri Remote touchpad swipes + gamepad left stick + keyboard arrows
            float h = UnityEngine.Input.GetAxis("Horizontal");
            float v = UnityEngine.Input.GetAxis("Vertical");

            if (_logCount < 5 && (Mathf.Abs(h) > deadzone || Mathf.Abs(v) > deadzone))
            {
                Debug.Log($"[InputAdapter] Input: h={h:F2} v={v:F2}");
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
