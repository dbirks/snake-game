using UnityEngine;
using SnakeGame.Core;
using SnakeGame.Input;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Wires the deterministic simulation to Unity's game loop.
    /// Runs simulation at a fixed tick rate via FixedUpdate.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private int randomSeed = 42;

        [Header("References")]
        [SerializeField] private SnakeRenderer snakeRenderer;
        [SerializeField] private InputAdapter inputAdapter;

        private SnakeSimulation _simulation;
        private GUIStyle _scoreStyle;
        private GUIStyle _gameOverStyle;

        public SnakeSimulation Simulation => _simulation;

        private void Start()
        {
            _simulation = new SnakeSimulation(randomSeed);
            Time.fixedDeltaTime = 1f / 60f;

            // Diagnostic: log input system state for debugging controller issues
            Debug.Log($"[GameManager] Started. InputAdapter={(inputAdapter != null ? "wired" : "NULL")}");
            Debug.Log($"[GameManager] Input backends: {UnityEngine.InputSystem.InputSystem.settings?.supportedDevices}");

            // Log all connected devices
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                Debug.Log($"[GameManager] Input device: {device.displayName} ({device.GetType().Name})");

            // Log when new devices connect
            UnityEngine.InputSystem.InputSystem.onDeviceChange += (device, change) =>
                Debug.Log($"[GameManager] Device {change}: {device.displayName} ({device.GetType().Name})");
        }

        private void FixedUpdate()
        {
            if (_simulation == null) return;

            var command = inputAdapter != null
                ? inputAdapter.ConsumeCommand()
                : InputCommand.None;

            _simulation.Tick(Time.fixedDeltaTime, command);

            // Auto-restart on death after a short delay
            if (!_simulation.State.IsAlive && _simulation.State.TickCount % 180 == 0)
                RestartGame();
        }

        private void LateUpdate()
        {
            if (_simulation == null || snakeRenderer == null) return;
            snakeRenderer.Render(_simulation.State);
        }

        private void OnGUI()
        {
            if (_simulation == null) return;

            if (_scoreStyle == null)
            {
                _scoreStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight
                };
                _scoreStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            }

            if (_gameOverStyle == null)
            {
                _gameOverStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 72,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _gameOverStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 0.9f);
            }

            // Score display
            GUI.Label(new Rect(Screen.width - 250, 20, 230, 60),
                $"Score: {_simulation.State.Score}", _scoreStyle);

            // Game over message
            if (!_simulation.State.IsAlive)
            {
                GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100),
                    "GAME OVER", _gameOverStyle);
            }
        }

        public void RestartGame()
        {
            _simulation = new SnakeSimulation(Random.Range(0, int.MaxValue));
        }
    }
}
