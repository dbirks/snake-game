using System;
using System.Collections.Generic;

namespace SnakeGame.Core
{
    /// <summary>
    /// Deterministic, tick-based snake simulation.
    /// No Unity dependencies — pure C# so it can run in EditMode tests.
    /// </summary>
    public class SnakeSimulation
    {
        public const float ArenaWidth = 20f;
        public const float ArenaHeight = 12f;
        public const float DefaultSpeed = 4f;
        public const float TurnRate = 3.5f; // radians per second
        public const float FoodRadius = 0.5f;
        public const float SegmentSpacing = 0.4f;
        public const int InitialLength = 5;

        private readonly Random _rng;

        public SnakeState State { get; private set; }

        public SnakeSimulation(int seed = 0)
        {
            _rng = new Random(seed);
            State = CreateInitialState();
        }

        public SnakeSimulation(SnakeState state, int seed = 0)
        {
            _rng = new Random(seed);
            State = state;
        }

        private SnakeState CreateInitialState()
        {
            var state = new SnakeState
            {
                Speed = DefaultSpeed,
                HeadingAngle = 0f, // facing right
                Score = 0,
                IsAlive = true,
                TickCount = 0
            };

            // Place snake in center, segments trailing to the left
            state.Direction = new Vector2F(1f, 0f);
            for (int i = 0; i < InitialLength; i++)
            {
                state.Segments.Add(new Vector2F(
                    ArenaWidth / 2f - i * SegmentSpacing,
                    ArenaHeight / 2f));
            }

            state.FoodPosition = SpawnFood(state);
            return state;
        }

        /// <summary>
        /// Advance the simulation by one fixed tick.
        /// </summary>
        public void Tick(float dt, InputCommand command)
        {
            if (!State.IsAlive) return;

            // Apply turning
            if (command == InputCommand.TurnLeft)
                State.HeadingAngle += TurnRate * dt;
            else if (command == InputCommand.TurnRight)
                State.HeadingAngle -= TurnRate * dt;

            // Update direction from heading
            State.Direction = new Vector2F(
                (float)Math.Cos(State.HeadingAngle),
                (float)Math.Sin(State.HeadingAngle));

            // Move head
            var head = State.Segments[0];
            var newHead = head + State.Direction * (State.Speed * dt);

            // Wall collision — wrap around
            newHead = new Vector2F(
                Wrap(newHead.X, 0, ArenaWidth),
                Wrap(newHead.Y, 0, ArenaHeight));

            // Insert new head, body follows
            State.Segments.Insert(0, newHead);

            // Check food collision
            var toFood = new Vector2F(
                newHead.X - State.FoodPosition.X,
                newHead.Y - State.FoodPosition.Y);

            if (toFood.SqrMagnitude < FoodRadius * FoodRadius)
            {
                State.Score++;
                State.FoodPosition = SpawnFood(State);
                // Don't remove tail — snake grows
            }
            else
            {
                // Remove tail to maintain length
                State.Segments.RemoveAt(State.Segments.Count - 1);
            }

            // Self-collision check (skip head and nearby segments to avoid
            // false positives from tightly-spaced trailing body)
            const float collisionRadius = 0.15f;
            float collisionThresholdSqr = collisionRadius * collisionRadius;
            int skipSegments = Math.Max(8, (int)(InitialLength * 1.5f));
            for (int i = skipSegments; i < State.Segments.Count; i++)
            {
                var seg = State.Segments[i];
                var diff = new Vector2F(newHead.X - seg.X, newHead.Y - seg.Y);
                if (diff.SqrMagnitude < collisionThresholdSqr)
                {
                    State.IsAlive = false;
                    break;
                }
            }

            State.TickCount++;
        }

        /// <summary>
        /// Run multiple ticks with a sequence of commands.
        /// </summary>
        public void RunTicks(float dt, IReadOnlyList<InputCommand> commands)
        {
            foreach (var cmd in commands)
                Tick(dt, cmd);
        }

        private Vector2F SpawnFood(SnakeState state)
        {
            // Simple random placement within arena bounds with margin
            const float margin = 1f;
            float x = margin + (float)(_rng.NextDouble() * (ArenaWidth - 2 * margin));
            float y = margin + (float)(_rng.NextDouble() * (ArenaHeight - 2 * margin));
            return new Vector2F(x, y);
        }

        private static float Wrap(float value, float min, float max)
        {
            float range = max - min;
            while (value < min) value += range;
            while (value >= max) value -= range;
            return value;
        }
    }
}
