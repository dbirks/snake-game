using UnityEngine;
using SnakeGame.Core;
using SnakeGame.Input;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Wires the deterministic simulation to Unity's game loop.
    /// Handles menu → gameplay → game over → restart flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeRenderer snakeRenderer;
        [SerializeField] private InputAdapter inputAdapter;

        private MainMenu _menu;
        private SnakeSimulation _simulation;
        private SnakeSimulation _simulation2; // player 2
        private SnakeRenderer _renderer2;
        private InputAdapter _input2;
        private GameConfig _config;
        private GUIStyle _scoreStyle;
        private GUIStyle _gameOverStyle;
        private bool _gameActive;
        private float _deathTimer;

        public SnakeSimulation Simulation => _simulation;

        private void Start()
        {
            Debug.Log($"[GameManager] Started. InputAdapter={(inputAdapter != null ? "wired" : "NULL")}");
            Debug.Log("[GameManager] Using old Input Manager (Input.GetAxis)");

            // Add menu component
            _menu = gameObject.AddComponent<MainMenu>();
        }

        private void Update()
        {
            // Wait for menu
            if (_menu != null && !_menu.GameStarted) return;

            // First frame after menu — start game
            if (!_gameActive)
            {
                _config = _menu != null ? _menu.Config : new GameConfig();
                StartGame();
                if (_menu != null) { Destroy(_menu); _menu = null; }
            }
        }

        private void StartGame()
        {
            _gameActive = true;
            _deathTimer = 0;
            int seed = Random.Range(0, int.MaxValue);

            _simulation = new SnakeSimulation(seed);
            Time.fixedDeltaTime = 1f / 60f;

            // Apply color
            if (snakeRenderer != null && _config != null)
            {
                var headColor = SnakeColorPalette.GetHeadColor(_config.Player1Color);
                var bodyColor = SnakeColorPalette.GetBodyColor(_config.Player1Color);
                snakeRenderer.SetColors(headColor, bodyColor);
            }

            // Player 2
            if (_config != null && _config.PlayerCount == 2)
                SetupPlayer2(seed + 1);

            Debug.Log($"[GameManager] Game started: {_config?.PlayerCount}P, color={_config?.Player1Color}");
        }

        private void SetupPlayer2(int seed)
        {
            _simulation2 = new SnakeSimulation(seed);
            // Offset player 2 start position
            _simulation2.State.Segments[0] = new Vector2F(
                SnakeSimulation.ArenaWidth / 2f,
                SnakeSimulation.ArenaHeight / 4f);

            // Create renderer for player 2
            var p2Obj = new GameObject("Player2");
            p2Obj.transform.SetParent(transform);
            _renderer2 = p2Obj.AddComponent<SnakeRenderer>();
            var headColor = SnakeColorPalette.GetHeadColor(_config.Player2Color);
            var bodyColor = SnakeColorPalette.GetBodyColor(_config.Player2Color);
            _renderer2.SetColors(headColor, bodyColor);

            // Create input for player 2 (joystick 2)
            var inputObj = new GameObject("Input2");
            inputObj.transform.SetParent(transform);
            _input2 = inputObj.AddComponent<InputAdapter>();
            _input2.SetPlayerIndex(2);

            Debug.Log("[GameManager] Player 2 set up");
        }

        private void FixedUpdate()
        {
            if (!_gameActive) return;

            // Player 1
            if (_simulation != null)
            {
                var cmd = inputAdapter != null ? inputAdapter.ConsumeCommand() : InputCommand.None;
                _simulation.Tick(Time.fixedDeltaTime, cmd);
            }

            // Player 2 — share food list with player 1
            if (_simulation2 != null)
            {
                _simulation2.Foods = _simulation.Foods; // shared food pool
                var cmd = _input2 != null ? _input2.ConsumeCommand() : InputCommand.None;
                _simulation2.Tick(Time.fixedDeltaTime, cmd);
            }

            // Cross-collision between snakes
            if (_simulation != null && _simulation2 != null)
            {
                _simulation.CheckCrossCollision(_simulation2);
                _simulation2.CheckCrossCollision(_simulation);
            }

            // Check if all players dead
            bool allDead = _simulation != null && !_simulation.State.IsAlive;
            if (_simulation2 != null) allDead = allDead && !_simulation2.State.IsAlive;

            if (allDead)
            {
                _deathTimer += Time.fixedDeltaTime;
                if (_deathTimer > 3f) RestartGame();
            }
        }

        private void LateUpdate()
        {
            if (!_gameActive) return;
            if (snakeRenderer != null && _simulation != null)
            {
                snakeRenderer.Render(_simulation.State);
                // Render food once (shared between players)
                snakeRenderer.RenderFood(_simulation.Foods);
            }
            if (_renderer2 != null && _simulation2 != null)
                _renderer2.Render(_simulation2.State);
        }

        private void OnGUI()
        {
            if (!_gameActive || _simulation == null) return;

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

            // Score
            string scoreText = _simulation2 != null
                ? $"P1: {_simulation.State.Score}  P2: {_simulation2.State.Score}"
                : $"Score: {_simulation.State.Score}";
            GUI.Label(new Rect(Screen.width - 400, 20, 380, 60), scoreText, _scoreStyle);

            // Game over
            bool allDead = !_simulation.State.IsAlive;
            if (_simulation2 != null) allDead = allDead && !_simulation2.State.IsAlive;
            if (allDead)
            {
                GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100),
                    "GAME OVER", _gameOverStyle);
            }
        }

        private void RestartGame()
        {
            // Clean up player 2 objects
            if (_renderer2 != null) { Destroy(_renderer2.gameObject); _renderer2 = null; }
            if (_input2 != null) { Destroy(_input2.gameObject); _input2 = null; }
            _simulation2 = null;

            // Show menu again
            _gameActive = false;
            _menu = gameObject.AddComponent<MainMenu>();
        }
    }
}
