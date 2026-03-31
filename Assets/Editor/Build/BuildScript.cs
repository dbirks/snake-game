using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System;
using System.Linq;

namespace SnakeGame.Editor
{
    /// <summary>
    /// CI build entrypoint. Invoke via:
    ///   Unity -batchmode -quit -executeMethod SnakeGame.Editor.BuildScript.BuildTvOS
    /// </summary>
    public static class BuildScript
    {
        private const string TvOSOutputPath = "build/tvOS";

        public static void BuildTvOS()
        {
            Debug.Log("[BuildScript] Starting tvOS build...");

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes in Build Settings!");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = TvOSOutputPath,
                target = BuildTarget.tvOS,
                options = BuildOptions.None
            };

            var result = BuildPipeline.BuildPlayer(options);

            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] Build failed: {result.summary.result}");
                Debug.LogError($"[BuildScript] Errors: {result.summary.totalErrors}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[BuildScript] Build succeeded! Output: {TvOSOutputPath}");
            Debug.Log($"[BuildScript] Size: {result.summary.totalSize} bytes");
        }
    }
}
