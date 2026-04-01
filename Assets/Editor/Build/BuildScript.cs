using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using SnakeGame.Core;
using SnakeGame.UnityGlue;

namespace SnakeGame.Editor
{
    /// <summary>
    /// CI build entrypoint. Invoke via:
    ///   Unity -batchmode -quit -executeMethod SnakeGame.Editor.BuildScript.BuildTvOS
    /// </summary>
    public static class BuildScript
    {
        private const string TvOSOutputPath = "build/tvOS";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        public static void BuildTvOS()
        {
            Debug.Log("[BuildScript] Starting tvOS build...");

            // Set bundle ID and product name for tvOS
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.tvOS, "dev.birks.snakegame");
            PlayerSettings.productName = "Snake Game";
            PlayerSettings.companyName = "Birks";
            Debug.Log("[BuildScript] Set bundle ID to dev.birks.snakegame");

            // Ensure a main scene exists
            if (!File.Exists(MainScenePath))
            {
                Debug.Log("[BuildScript] No Main scene found, creating one...");
                CreateMainScene();
            }

            // Ensure the scene is in Build Settings
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0 || !scenes.Any(s => s.path == MainScenePath))
            {
                Debug.Log("[BuildScript] Adding Main scene to Build Settings...");
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MainScenePath, true)
                };
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            Debug.Log($"[BuildScript] Building with {enabledScenes.Length} scene(s)");

            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes,
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

        /// <summary>
        /// Creates a minimal Main scene with camera, GameManager, and snake components.
        /// This allows CI to build without needing the Unity Editor GUI.
        /// </summary>
        private static void CreateMainScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MainScenePath));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera — orthographic, centered on arena
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = SnakeSimulation.ArenaHeight / 2f + 1f;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.15f); // dark flat background
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.transform.position = new Vector3(
                SnakeSimulation.ArenaWidth / 2f,
                SnakeSimulation.ArenaHeight / 2f,
                -10f);
            cameraObj.tag = "MainCamera";

            // Game root with all components
            var gameObj = new GameObject("Game");

            // Snake renderer (child)
            var rendererObj = new GameObject("SnakeRenderer");
            rendererObj.transform.SetParent(gameObj.transform);
            rendererObj.AddComponent<SnakeRenderer>();

            // Input adapter (child) with PlayerInput
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(gameObj.transform);
            var inputAdapter = inputObj.AddComponent<SnakeGame.Input.InputAdapter>();

            // Load and assign Input Actions asset
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Game/Input/SnakeInputActions.inputactions");
            if (inputActions != null)
            {
                var playerInput = inputObj.AddComponent<UnityEngine.InputSystem.PlayerInput>();
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = "Gameplay";
                playerInput.notificationBehavior =
                    UnityEngine.InputSystem.PlayerNotifications.SendMessages;
                Debug.Log("[BuildScript] Wired up Input Actions asset");
            }
            else
            {
                Debug.LogWarning("[BuildScript] SnakeInputActions.inputactions not found");
            }

            // GameManager wired up
            var manager = gameObj.AddComponent<GameManager>();

            // Wire serialized fields via SerializedObject
            var so = new SerializedObject(manager);
            so.FindProperty("snakeRenderer").objectReferenceValue =
                rendererObj.GetComponent<SnakeRenderer>();
            so.FindProperty("inputAdapter").objectReferenceValue = inputAdapter;
            so.ApplyModifiedProperties();

            // Save scene
            EditorSceneManager.SaveScene(scene, MainScenePath);
            Debug.Log($"[BuildScript] Created Main scene at {MainScenePath}");
        }
    }
}
