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

        public SnakeSimulation Simulation => _simulation;

        private void Start()
        {
            _simulation = new SnakeSimulation(randomSeed);

            // Set fixed timestep to 60 ticks/sec for smooth movement
            Time.fixedDeltaTime = 1f / 60f;
        }

        private void FixedUpdate()
        {
            if (_simulation == null) return;

            var command = inputAdapter != null
                ? inputAdapter.ConsumeCommand()
                : InputCommand.None;

            _simulation.Tick(Time.fixedDeltaTime, command);
        }

        private void LateUpdate()
        {
            if (_simulation == null || snakeRenderer == null) return;
            snakeRenderer.Render(_simulation.State);
        }

        public void RestartGame()
        {
            _simulation = new SnakeSimulation(randomSeed);
        }
    }
}
