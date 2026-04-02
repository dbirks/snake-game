using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Captures all Debug.Log/LogError/LogException output and POSTs it to a
    /// remote HTTP endpoint. Enables runtime debugging of tvOS builds from a
    /// headless Linux server without Xcode.
    ///
    /// To use: run a simple log receiver on your dev machine:
    ///   python3 -m http.server 8080
    /// Or use the provided Tools/ci/log_server.py
    ///
    /// Set the LOG_SERVER_URL in the build or leave empty to disable.
    /// </summary>
    public class RemoteLogger : MonoBehaviour
    {
        private static RemoteLogger _instance;
        private string _serverUrl;
        private readonly Queue<string> _logQueue = new Queue<string>();
        private bool _isSending;

        private void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Set via environment or hardcode for development
            // Empty string = remote logging disabled (no overhead)
            _serverUrl = System.Environment.GetEnvironmentVariable("LOG_SERVER_URL") ?? "";

            if (!string.IsNullOrEmpty(_serverUrl))
            {
                Application.logMessageReceived += HandleLog;
                Debug.Log($"[RemoteLogger] Sending logs to {_serverUrl}");
            }
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var entry = JsonUtility.ToJson(new LogEntry
            {
                level = type.ToString(),
                message = message,
                stack = (type == LogType.Exception || type == LogType.Error) ? stackTrace : "",
                time = Time.realtimeSinceStartup
            });
            _logQueue.Enqueue(entry);

            if (!_isSending)
                StartCoroutine(SendLogs());
        }

        private IEnumerator SendLogs()
        {
            _isSending = true;
            while (_logQueue.Count > 0)
            {
                string entry = _logQueue.Dequeue();
                using (var req = new UnityWebRequest(_serverUrl, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(entry);
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 3;
                    yield return req.SendWebRequest();
                    // Silently drop failures to avoid recursive logging
                }
            }
            _isSending = false;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        [System.Serializable]
        private struct LogEntry
        {
            public string level;
            public string message;
            public string stack;
            public float time;
        }
    }
}
