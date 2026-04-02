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

            // CRITICAL: Enable the new Input System backend.
            // Without this, the entire com.unity.inputsystem package is non-functional
            // at runtime — no controller, no Siri Remote, no keyboard input.
            // Default is "Input Manager (Old)" which ignores all Input System bindings.
            PlayerSettings.activeInputHandler = PlayerSettings.ActiveInputHandler.Both;
            Debug.Log("[BuildScript] Set Active Input Handling to Both (Old + New)");

            Debug.Log("[BuildScript] Set bundle ID to dev.birks.snakegame");

            // Configure remote logging (Grafana Cloud Loki)
            // Write credentials from CI env vars into a Resources text file
            // that RemoteLogger reads at runtime
            var lokiUrl = System.Environment.GetEnvironmentVariable("GRAFANA_LOKI_URL") ?? "";
            var lokiUser = System.Environment.GetEnvironmentVariable("GRAFANA_LOKI_USER") ?? "";
            var lokiToken = System.Environment.GetEnvironmentVariable("GRAFANA_LOKI_TOKEN") ?? "";
            if (!string.IsNullOrEmpty(lokiUrl))
            {
                Directory.CreateDirectory("Assets/Resources");
                File.WriteAllText("Assets/Resources/loki_config.txt",
                    $"{lokiUrl}\n{lokiUser}\n{lokiToken}");
                AssetDatabase.Refresh();
                Debug.Log("[BuildScript] Wrote Grafana Loki config to Resources");
            }

            // Ensure required shaders are included in the build
            // (Shader.Find returns null on tvOS if the shader is stripped)
            EnsureShadersIncluded();

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
        /// Add shaders to the Always Included Shaders list in Graphics Settings
        /// so they aren't stripped from the build.
        /// </summary>
        private static void EnsureShadersIncluded()
        {
            var graphicsSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.GraphicsSettings>(
                "ProjectSettings/GraphicsSettings.asset");

            string[] shaderNames = { "Unlit/Color", "Sprites/Default", "UI/Default" };
            foreach (var name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader != null)
                {
                    // Force the shader to be referenced so it's included in the build
                    var so = new SerializedObject(
                        AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
                    var arrayProp = so.FindProperty("m_AlwaysIncludedShaders");

                    bool found = false;
                    for (int i = 0; i < arrayProp.arraySize; i++)
                    {
                        if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        arrayProp.arraySize++;
                        arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1)
                            .objectReferenceValue = shader;
                        so.ApplyModifiedProperties();
                        Debug.Log($"[BuildScript] Added shader '{name}' to Always Included Shaders");
                    }
                }
            }
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

            // Load and assign Input Actions asset via SerializedObject
            // (setting fields before OnEnable fires avoids action map timing issues)
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Game/Input/SnakeInputActions.inputactions");
            if (inputActions != null)
            {
                var playerInput = inputObj.AddComponent<UnityEngine.InputSystem.PlayerInput>();
                var piso = new SerializedObject(playerInput);
                piso.FindProperty("m_Actions").objectReferenceValue = inputActions;
                piso.FindProperty("m_DefaultActionMap").stringValue = "Gameplay";
                piso.FindProperty("m_NotificationBehavior").enumValueIndex =
                    (int)UnityEngine.InputSystem.PlayerNotifications.SendMessages;
                piso.ApplyModifiedProperties();
                Debug.Log("[BuildScript] Wired up Input Actions asset via SerializedObject");
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

            // Remote logger for runtime debugging (disabled unless LOG_SERVER_URL is set)
            var loggerObj = new GameObject("RemoteLogger");
            loggerObj.AddComponent<RemoteLogger>();

            // Save scene
            EditorSceneManager.SaveScene(scene, MainScenePath);
            Debug.Log($"[BuildScript] Created Main scene at {MainScenePath}");
        }
    }
}
