using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Captures all Debug.Log output and pushes to Grafana Cloud Loki.
    /// Enables runtime debugging of tvOS builds from any browser via
    /// Grafana Cloud Explore → {job="snake-game"}
    ///
    /// Configure via build-time constants set by the CI pipeline.
    /// Disabled if LOKI_URL is empty.
    /// </summary>
    public class RemoteLogger : MonoBehaviour
    {
        private string _lokiUrl;
        private string _lokiUser;
        private string _lokiToken;

        private static RemoteLogger _instance;
        private readonly List<string[]> _logBuffer = new List<string[]>();
        private bool _isSending;
        private float _lastSendTime;
        private const float SendInterval = 5f; // batch logs every 5 seconds

        private void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load Loki credentials from Resources (written at build time by BuildScript)
            var config = Resources.Load<TextAsset>("loki_config");
            if (config != null)
            {
                var lines = config.text.Split('\n');
                if (lines.Length >= 3)
                {
                    _lokiUrl = lines[0].Trim();
                    _lokiUser = lines[1].Trim();
                    _lokiToken = lines[2].Trim();
                }
            }

            if (string.IsNullOrEmpty(_lokiUrl))
            {
                Debug.Log("[RemoteLogger] Loki config not found — remote logging disabled");
                return;
            }

            Application.logMessageReceived += HandleLog;
            Debug.Log("[RemoteLogger] Pushing logs to Grafana Cloud Loki");
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L).ToString();
            string logLine = type == LogType.Exception || type == LogType.Error
                ? $"[{type}] {message}\n{stackTrace}"
                : $"[{type}] {message}";

            _logBuffer.Add(new[] { timestamp, logLine });
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_lokiUrl)) return;
            if (_logBuffer.Count == 0) return;
            if (_isSending) return;
            if (Time.realtimeSinceStartup - _lastSendTime < SendInterval) return;

            StartCoroutine(SendBatch());
        }

        private IEnumerator SendBatch()
        {
            _isSending = true;
            _lastSendTime = Time.realtimeSinceStartup;

            // Grab current buffer and clear
            var batch = new List<string[]>(_logBuffer);
            _logBuffer.Clear();

            // Build Loki push payload
            var sb = new StringBuilder();
            sb.Append("{\"streams\":[{\"stream\":{");
            sb.Append("\"job\":\"snake-game\",");
            sb.Append("\"platform\":\"tvos\",");
            sb.Append("\"version\":\"").Append(Application.version).Append("\"");
            sb.Append("},\"values\":[");

            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("[\"").Append(batch[i][0]).Append("\",\"");
                // Escape JSON special characters
                sb.Append(EscapeJson(batch[i][1]));
                sb.Append("\"]");
            }

            sb.Append("]}]}");

            string url = _lokiUrl + "/loki/api/v1/push";
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_lokiUser + ":" + _lokiToken));

            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(sb.ToString());
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Basic " + auth);
                req.timeout = 10;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    // Don't log errors about logging to avoid infinite loop
                    // Just silently re-queue failed logs (drop if buffer too large)
                    if (_logBuffer.Count < 500)
                    {
                        _logBuffer.AddRange(batch);
                    }
                }
            }

            _isSending = false;
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }
    }
}
