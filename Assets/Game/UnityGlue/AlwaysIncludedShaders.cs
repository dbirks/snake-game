using UnityEngine;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Forces Unity to include these shaders in the build by referencing them.
    /// Without this, Shader.Find() returns null on tvOS because Unity strips
    /// "unused" shaders during the build process.
    /// </summary>
    public class AlwaysIncludedShaders : MonoBehaviour
    {
        // These references prevent Unity from stripping the shaders
        [SerializeField, HideInInspector]
        private Shader unlitColor;

        [SerializeField, HideInInspector]
        private Shader spritesDefault;

        private void OnValidate()
        {
            unlitColor = Shader.Find("Unlit/Color");
            spritesDefault = Shader.Find("Sprites/Default");
        }
    }
}
