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
        public const float TurnRate = 8f; // radians per second (snappy steering)
        public const float FoodRadius = 0.5f;
        public const float SegmentSpacing = 0.4f;
        public const int InitialLength = 5;

        public const int MaxFood = 3;
        public const float FoodPickupRadius = 0.8f; // more generous than FoodRadius for easier pickup

        private readonly Random _rng;

        public SnakeState State { get; private set; }
        public List<FoodItem> Foods { get; private set; } = new List<FoodItem>();

        public SnakeSimulation(int seed = 0)
        {
            _rng = new Random(seed);
            State = CreateInitialState();
            SpawnInitialFood();
        }

        public SnakeSimulation(SnakeState state, int seed = 0)
        {
            _rng = new Random(seed);
            State = state;
            if (Foods.Count == 0) SpawnInitialFood();
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

            state.FoodPosition = RandomPosition();
            state.CurrentFruit = SpawnFruitType();
            state.SpeedEffect = 1f;
            return state;
        }

        /// <summary>
        /// Advance the simulation by one fixed tick.
        /// </summary>
        public void Tick(float dt, InputCommand command)
        {
            if (!State.IsAlive) return;

            // Steer toward the stick direction (360-degree free-flowing movement)
            if (command.HasAnalogInput)
            {
                // Calculate target angle from stick input
                float targetAngle = (float)Math.Atan2(command.StickY, command.StickX);

                // Smoothly rotate toward target using shortest path
                float angleDiff = targetAngle - State.HeadingAngle;

                // Normalize to [-PI, PI]
                while (angleDiff > Math.PI) angleDiff -= 2f * (float)Math.PI;
                while (angleDiff < -Math.PI) angleDiff += 2f * (float)Math.PI;

                // Apply turn rate limit for smooth steering
                float maxTurn = TurnRate * dt;
                if (angleDiff > maxTurn)
                    State.HeadingAngle += maxTurn;
                else if (angleDiff < -maxTurn)
                    State.HeadingAngle -= maxTurn;
                else
                    State.HeadingAngle = targetAngle;
            }

            // Update direction from heading
            State.Direction = new Vector2F(
                (float)Math.Cos(State.HeadingAngle),
                (float)Math.Sin(State.HeadingAngle));

            // Tick down speed effect
            if (State.EffectTimer > 0)
            {
                State.EffectTimer -= dt;
                if (State.EffectTimer <= 0)
                    State.SpeedEffect = 1f;
            }

            // Move head (apply speed effect)
            float currentSpeed = State.Speed * (State.SpeedEffect > 0 ? State.SpeedEffect : 1f);
            var head = State.Segments[0];
            var newHead = head + State.Direction * (currentSpeed * dt);

            // Wall collision — wrap around
            newHead = new Vector2F(
                Wrap(newHead.X, 0, ArenaWidth),
                Wrap(newHead.Y, 0, ArenaHeight));

            // Insert new head, body follows
            State.Segments.Insert(0, newHead);

            // Check food collision against all food items
            for (int fi = Foods.Count - 1; fi >= 0; fi--)
            {
                var food = Foods[fi];
                var toFood = new Vector2F(
                    newHead.X - food.Position.X,
                    newHead.Y - food.Position.Y);

                if (toFood.SqrMagnitude < FoodPickupRadius * FoodPickupRadius)
                {
                    var fruit = food.Fruit;
                    State.Score += FruitEffects.ScoreValue(fruit);
                    State.PendingGrowth += FruitEffects.GrowthAmount(fruit);

                    // Speed effect
                    float speedMul = FruitEffects.SpeedMultiplier(fruit);
                    float duration = FruitEffects.EffectDuration(fruit);
                    if (duration > 0)
                    {
                        State.SpeedEffect = speedMul;
                        State.EffectTimer = duration;
                    }

                    // Shrink effect
                    int shrink = FruitEffects.ShrinkAmount(fruit);
                    for (int s = 0; s < shrink && State.Segments.Count > 3; s++)
                        State.Segments.RemoveAt(State.Segments.Count - 1);

                    // Remove eaten food and spawn replacement
                    Foods.RemoveAt(fi);
                    SpawnOneFood();
                    break; // only eat one per tick
                }
            }

            // Grow or trim tail
            if (State.PendingGrowth > 0)
                State.PendingGrowth--;
            else
                State.Segments.RemoveAt(State.Segments.Count - 1);

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

        private void SpawnInitialFood()
        {
            Foods.Clear();
            for (int i = 0; i < MaxFood; i++)
                SpawnOneFood();
            // Keep legacy field in sync for renderer backwards compat
            if (Foods.Count > 0)
            {
                State.FoodPosition = Foods[0].Position;
                State.CurrentFruit = Foods[0].Fruit;
            }
        }

        private void SpawnOneFood()
        {
            Foods.Add(new FoodItem
            {
                Position = RandomPosition(),
                Fruit = SpawnFruitType()
            });
        }

        /// <summary>
        /// Check if the given head position collides with another snake's body.
        /// Call this from GameManager for 2-player cross-collision.
        /// </summary>
        public void CheckCrossCollision(SnakeSimulation other)
        {
            if (!State.IsAlive || other == null || !other.State.IsAlive) return;

            var myHead = State.Segments[0];
            for (int i = 0; i < other.State.Segments.Count; i++)
            {
                var seg = other.State.Segments[i];
                var diff = new Vector2F(myHead.X - seg.X, myHead.Y - seg.Y);
                if (diff.SqrMagnitude < 0.15f * 0.15f)
                {
                    State.IsAlive = false;
                    break;
                }
            }
        }

        private Vector2F RandomPosition()
        {
            const float margin = 1f;
            float x = margin + (float)(_rng.NextDouble() * (ArenaWidth - 2 * margin));
            float y = margin + (float)(_rng.NextDouble() * (ArenaHeight - 2 * margin));
            return new Vector2F(x, y);
        }

        private FruitType SpawnFruitType()
        {
            var values = (FruitType[])Enum.GetValues(typeof(FruitType));
            int totalWeight = 0;
            foreach (var f in values)
                totalWeight += FruitEffects.SpawnWeight(f);

            int roll = _rng.Next(totalWeight);
            int cumulative = 0;
            foreach (var f in values)
            {
                cumulative += FruitEffects.SpawnWeight(f);
                if (roll < cumulative) return f;
            }
            return FruitType.Apple;
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
