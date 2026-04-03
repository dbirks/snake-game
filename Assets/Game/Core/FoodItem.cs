using System;

namespace SnakeGame.Core
{
    [Serializable]
    public struct FoodItem
    {
        public Vector2F Position;
        public FruitType Fruit;
    }
}
