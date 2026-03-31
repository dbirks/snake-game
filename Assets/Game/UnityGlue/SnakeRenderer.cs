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

        private GameObject _foodObject;
        private readonly System.Collections.Generic.List<GameObject> _segmentObjects = new();
        private Material _headMat;
        private Material _bodyMat;
        private Material _foodMat;

        private void Awake()
        {
            // Flat unlit materials
            var shader = Shader.Find("Unlit/Color");
            _headMat = new Material(shader) { color = snakeHeadColor };
            _bodyMat = new Material(shader) { color = snakeBodyColor };
            _foodMat = new Material(shader) { color = foodColor };

            _foodObject = CreateQuad("Food", _foodMat, 0.5f);
        }

        public void Render(SnakeState state)
        {
            if (state == null) return;

            // Ensure we have enough segment objects
            while (_segmentObjects.Count < state.Segments.Count)
            {
                bool isHead = _segmentObjects.Count == 0;
                var obj = CreateQuad(
                    isHead ? "Head" : $"Segment_{_segmentObjects.Count}",
                    isHead ? _headMat : _bodyMat,
                    segmentScale);
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

        private GameObject CreateQuad(string name, Material mat, float scale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = name;
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one * scale;
            obj.GetComponent<MeshRenderer>().material = mat;
            // Remove collider — we handle collision in simulation
            Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        private void OnDestroy()
        {
            if (_headMat != null) Destroy(_headMat);
            if (_bodyMat != null) Destroy(_bodyMat);
            if (_foodMat != null) Destroy(_foodMat);
        }
    }
}
