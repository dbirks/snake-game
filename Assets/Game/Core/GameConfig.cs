using System;

namespace SnakeGame.Core
{
    /// <summary>
    /// Game configuration chosen from the menu.
    /// Pure data — no Unity dependency.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public int PlayerCount = 1; // 1 or 2
        public SnakeColor Player1Color = SnakeColor.Orange;
        public SnakeColor Player2Color = SnakeColor.Blue;
    }

    public enum SnakeColor
    {
        Orange,
        Green,
        Blue,
        Purple,
        Red,
        Yellow,
        Pink,
        Cyan
    }
}
