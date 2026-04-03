using System;
using System.Collections.Generic;

namespace SnakeGame.Core
{
    /// <summary>
    /// Serializable snapshot of the entire game state at a point in time.
    /// Pure data — no Unity dependencies.
    /// </summary>
    [Serializable]
    public struct Vector2F
    {
        public float X;
        public float Y;

        public Vector2F(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2F operator +(Vector2F a, Vector2F b) =>
            new Vector2F(a.X + b.X, a.Y + b.Y);

        public static Vector2F operator *(Vector2F v, float s) =>
            new Vector2F(v.X * s, v.Y * s);

        public float SqrMagnitude => X * X + Y * Y;
    }

    [Serializable]
    public class SnakeState
    {
        public List<Vector2F> Segments = new List<Vector2F>();
        public Vector2F Direction;
        public float Speed;
        public float HeadingAngle; // radians
        public int Score;
        public bool IsAlive;
        // Food is now managed by SnakeSimulation (shared across players)
        // These are kept for backwards compat but not used in new code
        public Vector2F FoodPosition;
        public FruitType CurrentFruit;
        public int TickCount;
        public int PendingGrowth; // segments to add before trimming tail
        public float SpeedEffect;  // multiplier (1.0 = normal)
        public float EffectTimer;  // seconds remaining

        public SnakeState Clone()
        {
            return new SnakeState
            {
                Segments = new List<Vector2F>(Segments),
                Direction = Direction,
                Speed = Speed,
                HeadingAngle = HeadingAngle,
                Score = Score,
                IsAlive = IsAlive,
                FoodPosition = FoodPosition,
                TickCount = TickCount,
                PendingGrowth = PendingGrowth,
                CurrentFruit = CurrentFruit,
                SpeedEffect = SpeedEffect,
                EffectTimer = EffectTimer
            };
        }
    }
}
