using UnityEngine;
using SnakeGame.Core;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Maps SnakeColor enum to Unity colors.
    /// </summary>
    public static class SnakeColorPalette
    {
        public static Color GetHeadColor(SnakeColor c) => c switch
        {
            SnakeColor.Orange => new Color(1f, 0.55f, 0f),
            SnakeColor.Green  => new Color(0.2f, 0.8f, 0.3f),
            SnakeColor.Blue   => new Color(0.3f, 0.5f, 1f),
            SnakeColor.Purple => new Color(0.7f, 0.3f, 0.9f),
            SnakeColor.Red    => new Color(0.9f, 0.2f, 0.2f),
            SnakeColor.Yellow => new Color(1f, 0.85f, 0.1f),
            SnakeColor.Pink   => new Color(1f, 0.4f, 0.7f),
            SnakeColor.Cyan   => new Color(0.2f, 0.9f, 0.9f),
            _ => Color.white
        };

        public static Color GetBodyColor(SnakeColor c)
        {
            var head = GetHeadColor(c);
            return Color.Lerp(head, Color.white, 0.2f);
        }
    }
}
