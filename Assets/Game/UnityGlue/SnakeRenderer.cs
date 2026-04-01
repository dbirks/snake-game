using UnityEngine;
using SnakeGame.Core;

namespace SnakeGame.UnityGlue
{
    /// <summary>
    /// Renders SnakeState to Unity GameObjects.
    /// Orange snake with a flat look.
    /// </summary>
    public class SnakeRenderer : MonoBehaviour
    {
        [Header("Snake Appearance")]
        [SerializeField] private Color snakeHeadColor = new Color(1f, 0.55f, 0f); // orange
        [SerializeField] private Color snakeBodyColor = new Color(1f, 0.65f, 0.15f); // lighter orange
        [SerializeField] private Color foodColor = new Color(0.9f, 0.2f, 0.2f); // red
        [SerializeField] private float segmentScale = 0.35f;
        [SerializeField] private float foodScale = 0.5f;

        private GameObject _foodObject;
        private readonly System.Collections.Generic.List<GameObject> _segmentObjects = new();
        private Material _headMat;
        private Material _bodyMat;
        private Material _foodMat;

        private void Awake()
        {
            _headMat = CreateFlatMaterial(snakeHeadColor);
            _bodyMat = CreateFlatMaterial(snakeBodyColor);
            _foodMat = CreateFlatMaterial(foodColor);

            _foodObject = CreateCircle("Food", _foodMat, foodScale);
        }

        /// <summary>
        /// Create a flat colored material that works on all platforms including tvOS.
        /// Shader.Find("Unlit/Color") can return null on tvOS if the shader is stripped.
        /// Using a runtime-generated shader to guarantee availability.
        /// </summary>
        private Material CreateFlatMaterial(Color color)
        {
            // Try Unlit/Color first (works in Editor)
            var shader = Shader.Find("Unlit/Color");

            // Fallback to shaders that are always included
            if (shader == null)
                shader = Shader.Find("UI/Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Hidden/InternalColored");

            var mat = new Material(shader);
            mat.color = color;
            // For Sprites/Default shader, set the tint via _Color
            mat.SetColor("_Color", color);
            return mat;
        }

        public void Render(SnakeState state)
        {
            if (state == null) return;

            // Ensure we have enough segment objects
            while (_segmentObjects.Count < state.Segments.Count)
            {
                bool isHead = _segmentObjects.Count == 0;
                var obj = CreateCircle(
                    isHead ? "Head" : $"Segment_{_segmentObjects.Count}",
                    isHead ? _headMat : _bodyMat,
                    isHead ? segmentScale * 1.2f : segmentScale);
                _segmentObjects.Add(obj);
            }

            // Hide excess segments
            for (int i = state.Segments.Count; i < _segmentObjects.Count; i++)
                _segmentObjects[i].SetActive(false);

            // Position segments
            for (int i = 0; i < state.Segments.Count; i++)
            {
                var seg = state.Segments[i];
                _segmentObjects[i].SetActive(true);
                _segmentObjects[i].transform.position = new Vector3(seg.X, seg.Y, 0f);
            }

            // Position food
            _foodObject.transform.position = new Vector3(
                state.FoodPosition.X, state.FoodPosition.Y, 0f);
        }

        /// <summary>
        /// Create a circle-like sprite for a flat look (instead of 3D quad).
        /// Using SpriteRenderer for guaranteed tvOS compatibility.
        /// </summary>
        private GameObject CreateCircle(string name, Material mat, float scale)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.material = mat;
            sr.color = mat.color;
            obj.transform.localScale = Vector3.one * scale;

            return obj;
        }

        /// <summary>
        /// Generate a simple filled circle sprite at runtime.
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Smooth edge with anti-aliasing
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            return Sprite.Create(tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        private void OnDestroy()
        {
            if (_headMat != null) Destroy(_headMat);
            if (_bodyMat != null) Destroy(_bodyMat);
            if (_foodMat != null) Destroy(_foodMat);
        }
    }
}
