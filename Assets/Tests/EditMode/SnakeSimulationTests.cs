using NUnit.Framework;
using SnakeGame.Core;
using System.Collections.Generic;

namespace SnakeGame.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class SnakeSimulationTests
    {
        private const float FixedDt = 1f / 60f;

        [Test]
        public void InitialState_SnakeIsAlive()
        {
            var sim = new SnakeSimulation(seed: 123);
            Assert.IsTrue(sim.State.IsAlive);
        }

        [Test]
        public void InitialState_HasCorrectSegmentCount()
        {
            var sim = new SnakeSimulation(seed: 123);
            Assert.AreEqual(SnakeSimulation.InitialLength, sim.State.Segments.Count);
        }

        [Test]
        public void InitialState_ScoreIsZero()
        {
            var sim = new SnakeSimulation(seed: 123);
            Assert.AreEqual(0, sim.State.Score);
        }

        [Test]
        public void InitialState_HeadIsInCenter()
        {
            var sim = new SnakeSimulation(seed: 123);
            var head = sim.State.Segments[0];
            Assert.AreEqual(SnakeSimulation.ArenaWidth / 2f, head.X, 0.01f);
            Assert.AreEqual(SnakeSimulation.ArenaHeight / 2f, head.Y, 0.01f);
        }

        [Test]
        public void Tick_NoInput_SnakeMovesForward()
        {
            var sim = new SnakeSimulation(seed: 123);
            var startX = sim.State.Segments[0].X;

            sim.Tick(FixedDt, InputCommand.None);

            var newX = sim.State.Segments[0].X;
            Assert.Greater(newX, startX, "Snake should move right (initial heading = 0)");
        }

        [Test]
        public void Tick_TurnLeft_HeadingIncreases()
        {
            var sim = new SnakeSimulation(seed: 123);
            var startAngle = sim.State.HeadingAngle;

            sim.Tick(FixedDt, InputCommand.TurnLeft);

            Assert.Greater(sim.State.HeadingAngle, startAngle);
        }

        [Test]
        public void Tick_TurnRight_HeadingDecreases()
        {
            var sim = new SnakeSimulation(seed: 123);
            var startAngle = sim.State.HeadingAngle;

            sim.Tick(FixedDt, InputCommand.TurnRight);

            Assert.Less(sim.State.HeadingAngle, startAngle);
        }

        [Test]
        public void Tick_SegmentCountStaysConstant_WhenNoFood()
        {
            var sim = new SnakeSimulation(seed: 123);
            int initialCount = sim.State.Segments.Count;

            // Run several ticks with no food pickup
            for (int i = 0; i < 10; i++)
                sim.Tick(FixedDt, InputCommand.None);

            Assert.AreEqual(initialCount, sim.State.Segments.Count);
        }

        [Test]
        public void Tick_TickCountIncrements()
        {
            var sim = new SnakeSimulation(seed: 123);
            Assert.AreEqual(0, sim.State.TickCount);

            sim.Tick(FixedDt, InputCommand.None);
            Assert.AreEqual(1, sim.State.TickCount);

            sim.Tick(FixedDt, InputCommand.None);
            Assert.AreEqual(2, sim.State.TickCount);
        }

        [Test]
        [Category("Determinism")]
        public void Determinism_SameSeed_SameResults()
        {
            var commands = new List<InputCommand>
            {
                InputCommand.None, InputCommand.None, InputCommand.TurnLeft,
                InputCommand.None, InputCommand.TurnRight, InputCommand.None,
                InputCommand.None, InputCommand.TurnLeft, InputCommand.None,
                InputCommand.None
            };

            // Run simulation twice with same seed
            var sim1 = new SnakeSimulation(seed: 42);
            sim1.RunTicks(FixedDt, commands);

            var sim2 = new SnakeSimulation(seed: 42);
            sim2.RunTicks(FixedDt, commands);

            // States must match exactly
            Assert.AreEqual(sim1.State.Segments.Count, sim2.State.Segments.Count);
            Assert.AreEqual(sim1.State.Score, sim2.State.Score);
            Assert.AreEqual(sim1.State.IsAlive, sim2.State.IsAlive);
            Assert.AreEqual(sim1.State.HeadingAngle, sim2.State.HeadingAngle, 0.0001f);

            for (int i = 0; i < sim1.State.Segments.Count; i++)
            {
                Assert.AreEqual(sim1.State.Segments[i].X, sim2.State.Segments[i].X, 0.0001f);
                Assert.AreEqual(sim1.State.Segments[i].Y, sim2.State.Segments[i].Y, 0.0001f);
            }
        }

        [Test]
        [Category("Determinism")]
        public void Determinism_DifferentSeed_DifferentFoodPosition()
        {
            var sim1 = new SnakeSimulation(seed: 1);
            var sim2 = new SnakeSimulation(seed: 2);

            // Food positions should differ with different seeds
            bool xDiffers = System.Math.Abs(sim1.State.FoodPosition.X - sim2.State.FoodPosition.X) > 0.01f;
            bool yDiffers = System.Math.Abs(sim1.State.FoodPosition.Y - sim2.State.FoodPosition.Y) > 0.01f;

            Assert.IsTrue(xDiffers || yDiffers,
                "Different seeds should produce different food positions");
        }

        [Test]
        public void DeadSnake_DoesNotAdvance()
        {
            var state = new SnakeState
            {
                Segments = new List<Vector2F> { new Vector2F(5, 5) },
                Direction = new Vector2F(1, 0),
                Speed = 4f,
                HeadingAngle = 0f,
                IsAlive = false,
                FoodPosition = new Vector2F(15, 5),
                TickCount = 100
            };

            var sim = new SnakeSimulation(state, seed: 0);
            sim.Tick(FixedDt, InputCommand.None);

            Assert.AreEqual(100, sim.State.TickCount, "Dead snake should not tick");
        }

        [Test]
        public void SnakeState_Clone_IsIndependent()
        {
            var sim = new SnakeSimulation(seed: 0);
            var clone = sim.State.Clone();

            sim.Tick(FixedDt, InputCommand.TurnLeft);

            Assert.AreNotEqual(sim.State.HeadingAngle, clone.HeadingAngle,
                "Clone should not be affected by simulation advancing");
        }

        [Test]
        public void RunTicks_ExecutesAllCommands()
        {
            // Use seed 123 and short run to ensure snake stays alive
            var sim = new SnakeSimulation(seed: 123);
            var commands = new List<InputCommand>();
            for (int i = 0; i < 5; i++)
                commands.Add(InputCommand.None);

            sim.RunTicks(FixedDt, commands);

            Assert.AreEqual(5, sim.State.TickCount);
            Assert.IsTrue(sim.State.IsAlive);
        }
    }
}
