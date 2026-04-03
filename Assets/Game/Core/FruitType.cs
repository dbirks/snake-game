using System;

namespace SnakeGame.Core
{
    public enum FruitType
    {
        Apple,      // standard growth (+3)
        Banana,     // speed boost
        Grape,      // bonus points (3x)
        Orange,     // slow down
        Strawberry, // shrink (-2 segments)
        Watermelon, // mega growth (+10)
    }

    public static class FruitEffects
    {
        public static int GrowthAmount(FruitType fruit) => fruit switch
        {
            FruitType.Apple      => 5,
            FruitType.Banana     => 3,
            FruitType.Grape      => 3,
            FruitType.Orange     => 3,
            FruitType.Strawberry => 0,
            FruitType.Watermelon => 15,
            _ => 5
        };

        public static int ScoreValue(FruitType fruit) => fruit switch
        {
            FruitType.Apple      => 1,
            FruitType.Banana     => 1,
            FruitType.Grape      => 3,
            FruitType.Orange     => 1,
            FruitType.Strawberry => 2,
            FruitType.Watermelon => 5,
            _ => 1
        };

        /// <summary>
        /// Speed multiplier applied for a duration after eating.
        /// 1.0 = normal speed.
        /// </summary>
        public static float SpeedMultiplier(FruitType fruit) => fruit switch
        {
            FruitType.Banana => 1.5f,
            FruitType.Orange => 0.6f,
            _ => 1f
        };

        /// <summary>
        /// Duration in seconds for speed effect.
        /// </summary>
        public static float EffectDuration(FruitType fruit) => fruit switch
        {
            FruitType.Banana => 3f,
            FruitType.Orange => 4f,
            _ => 0f
        };

        /// <summary>
        /// How many segments to remove (for shrink fruit).
        /// </summary>
        public static int ShrinkAmount(FruitType fruit) => fruit switch
        {
            FruitType.Strawberry => 2,
            _ => 0
        };

        /// <summary>
        /// Spawn weight — higher = more common.
        /// </summary>
        public static int SpawnWeight(FruitType fruit) => fruit switch
        {
            FruitType.Apple      => 10,
            FruitType.Banana     => 3,
            FruitType.Grape      => 3,
            FruitType.Orange     => 3,
            FruitType.Strawberry => 2,
            FruitType.Watermelon => 1,
            _ => 10
        };
    }
}
