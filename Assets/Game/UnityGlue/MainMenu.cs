using UnityEngine;
using SnakeGame.Core;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Main menu screen using OnGUI for tvOS compatibility.
    /// Navigable with Siri Remote and gamepad via old Input Manager.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public GameConfig Config { get; private set; } = new GameConfig();
        public bool GameStarted { get; private set; }

        private int _selectedItem;
        private bool _inSettings;
        private int _settingsItem;
        private float _inputCooldown;
        private string _titleEffect;
        private Color _titleColor;

        private readonly string[] _menuItems = { "Play", "Settings" };
        private readonly string[] _settingsItems = { "Snake Color", "Players: 1", "Back" };
        private readonly string[] _titleEffects = { "SNAKE GAME", "S N A K E", "~SNAKE~", "SNAKE!", "sNaKe GaMe" };

        private void Awake()
        {
            // Random title style
            _titleEffect = _titleEffects[Random.Range(0, _titleEffects.Length)];
            _titleColor = Color.HSVToRGB(Random.value, 0.6f, 1f);
        }

        private void Update()
        {
            if (GameStarted) return;

            _inputCooldown -= Time.deltaTime;
            if (_inputCooldown > 0) return;

            float v = UnityEngine.Input.GetAxis("Vertical");
            float h = UnityEngine.Input.GetAxis("Horizontal");
            bool select = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton14) ||
                          UnityEngine.Input.GetKeyDown(KeyCode.Return) ||
                          UnityEngine.Input.GetKeyDown(KeyCode.Space);

            if (_inSettings)
                HandleSettingsInput(v, h, select);
            else
                HandleMenuInput(v, select);
        }

        private void HandleMenuInput(float v, bool select)
        {
            if (v < -0.5f) { _selectedItem = Mathf.Min(_selectedItem + 1, _menuItems.Length - 1); _inputCooldown = 0.2f; }
            if (v > 0.5f) { _selectedItem = Mathf.Max(_selectedItem - 1, 0); _inputCooldown = 0.2f; }

            if (select)
            {
                if (_selectedItem == 0) GameStarted = true;
                if (_selectedItem == 1) { _inSettings = true; _settingsItem = 0; }
                _inputCooldown = 0.3f;
            }
        }

        private void HandleSettingsInput(float v, float h, bool select)
        {
            if (v < -0.5f) { _settingsItem = Mathf.Min(_settingsItem + 1, _settingsItems.Length - 1); _inputCooldown = 0.2f; }
            if (v > 0.5f) { _settingsItem = Mathf.Max(_settingsItem - 1, 0); _inputCooldown = 0.2f; }

            // Snake color — cycle with left/right
            if (_settingsItem == 0 && Mathf.Abs(h) > 0.5f)
            {
                int count = System.Enum.GetValues(typeof(SnakeColor)).Length;
                int current = (int)Config.Player1Color;
                Config.Player1Color = (SnakeColor)(((current + (h > 0 ? 1 : count - 1)) % count));
                _inputCooldown = 0.2f;
            }

            // Player count — toggle with left/right or select
            if (_settingsItem == 1 && (Mathf.Abs(h) > 0.5f || select))
            {
                Config.PlayerCount = Config.PlayerCount == 1 ? 2 : 1;
                _inputCooldown = 0.2f;
                if (select) return; // Don't also trigger "Back"
            }

            // Back
            if (_settingsItem == 2 && select)
            {
                _inSettings = false;
                _inputCooldown = 0.3f;
            }
        }

        private void OnGUI()
        {
            if (GameStarted) return;

            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;

            // Title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 96,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = _titleColor;
            GUI.Label(new Rect(0, cy - 250, Screen.width, 120), _titleEffect, titleStyle);

            // Subtitle
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, cy - 140, Screen.width, 50), "for Apple TV", subStyle);

            if (_inSettings)
                DrawSettings(cx, cy);
            else
                DrawMainMenu(cx, cy);
        }

        private void DrawMainMenu(float cx, float cy)
        {
            var normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 56,
                alignment = TextAnchor.MiddleCenter
            };

            var selectedStyle = new GUIStyle(normalStyle);
            selectedStyle.normal.textColor = Color.yellow;

            for (int i = 0; i < _menuItems.Length; i++)
            {
                var style = i == _selectedItem ? selectedStyle : normalStyle;
                string prefix = i == _selectedItem ? "> " : "  ";
                GUI.Label(new Rect(0, cy - 40 + i * 80, Screen.width, 70),
                    prefix + _menuItems[i], style);
            }
        }

        private void DrawSettings(float cx, float cy)
        {
            var normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                alignment = TextAnchor.MiddleCenter
            };

            var selectedStyle = new GUIStyle(normalStyle);
            selectedStyle.normal.textColor = Color.yellow;

            string[] labels = {
                $"< Snake Color: {Config.Player1Color} >",
                $"< Players: {Config.PlayerCount} >",
                "Back"
            };

            // 2P note
            if (Config.PlayerCount == 2)
            {
                var noteStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter
                };
                noteStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(0, cy + 180, Screen.width, 40),
                    "2P requires a second Bluetooth controller (Xbox, PS, Switch Pro)", noteStyle);
            }

            // Color preview
            var previewColor = SnakeColorPalette.GetHeadColor(Config.Player1Color);
            var previewRect = new Rect(cx - 20, cy - 100, 40, 40);
            GUI.DrawTexture(previewRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, previewColor, 0, 10);

            for (int i = 0; i < labels.Length; i++)
            {
                var style = i == _settingsItem ? selectedStyle : normalStyle;
                GUI.Label(new Rect(0, cy - 40 + i * 70, Screen.width, 60), labels[i], style);
            }
        }
    }
}
