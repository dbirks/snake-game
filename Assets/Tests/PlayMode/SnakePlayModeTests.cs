using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SnakeGame.Core;
using SnakeGame.UnityGlue;

namespace SnakeGame.Tests.PlayMode
{
    [TestFixture]
    [Category("Integration")]
    public class SnakePlayModeTests
    {
        private GameObject _gameRoot;

        [SetUp]
        public void SetUp()
        {
            _gameRoot = new GameObject("TestGameRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_gameRoot);
        }

        [UnityTest]
        public IEnumerator GameManager_Creates_Simulation_On_Start()
        {
            var manager = _gameRoot.AddComponent<GameManager>();

            // GameManager needs an InputAdapter reference — add a dummy
            var inputObj = new GameObject("InputAdapter");
            inputObj.transform.SetParent(_gameRoot.transform);
            var inputAdapter = inputObj.AddComponent<InputAdapter>();

            // Use reflection to set the serialized field since it's private
            var field = typeof(GameManager).GetField("inputAdapter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(manager, inputAdapter);

            // Wait one frame for Start() to run
            yield return null;

            Assert.IsNotNull(manager.Simulation, "Simulation should be created after Start");
            Assert.IsTrue(manager.Simulation.State.IsAlive);
        }

        [UnityTest]
        public IEnumerator SnakeRenderer_Does_Not_Throw_On_Render()
        {
            var rendererObj = new GameObject("Renderer");
            rendererObj.transform.SetParent(_gameRoot.transform);
            var renderer = rendererObj.AddComponent<SnakeRenderer>();

            yield return null; // Let Awake run

            var sim = new SnakeSimulation(seed: 0);

            // Should not throw
            Assert.DoesNotThrow(() => renderer.Render(sim.State));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Simulation_Advances_Over_FixedUpdate_Frames()
        {
            var manager = _gameRoot.AddComponent<GameManager>();

            var inputObj = new GameObject("InputAdapter");
            inputObj.transform.SetParent(_gameRoot.transform);
            var inputAdapter = inputObj.AddComponent<InputAdapter>();

            var field = typeof(GameManager).GetField("inputAdapter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(manager, inputAdapter);

            yield return null; // Start()

            int initialTick = manager.Simulation.State.TickCount;

            // Wait a few frames for FixedUpdate to run
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(manager.Simulation.State.TickCount, initialTick,
                "Simulation should advance via FixedUpdate");
        }
    }
}
