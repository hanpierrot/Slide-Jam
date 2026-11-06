using UnityEngine;

namespace SlideJam.Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private float _margin = 0.5f;
        [SerializeField] private float _padTop = 1f;
        [SerializeField] private float _padBottom = 1.5f;
        [SerializeField, HideInInspector] private Rect _previewBounds;

        private Camera _camera;
        private Rect _bounds;
        private Vector2Int _screen;
        private bool _hasFit;

        public Camera Cam
        {
            get
            {
                if (_camera == null) _camera = GetComponent<Camera>();
                return _camera;
            }
        }

        public void Fit(Rect bounds)
        {
            _bounds = bounds;
            _screen = ScreenSize();
            _hasFit = true;
            Apply(bounds, _screen);
        }

        private void Update()
        {
            if (Application.isPlaying && _hasFit && ScreenSize() != _screen) Fit(_bounds);
        }

        private void Apply(Rect bounds, Vector2Int screen)
        {
            if (screen.x <= 0 || screen.y <= 0) return;

            Compute(bounds, (float)screen.x / screen.y, _margin, _padTop, _padBottom, out float size, out Vector2 center); // changed
            Cam.orthographicSize = size;
            Vector3 position = transform.position;
            transform.position = new Vector3(center.x, center.y, position.z);
        }
        
        public static void Compute(Rect bounds, float aspect, float margin, float padTop, float padBottom,
            out float size, out Vector2 center)
        {
            float width = bounds.width + 2f * margin;
            float height = bounds.height + padTop + padBottom + 2f * margin;
            size = Mathf.Max(width / (2f * aspect), height / 2f);
            center = new Vector2(bounds.center.x, (bounds.yMax + padTop + bounds.yMin - padBottom) / 2f);
        }

        private static Vector2Int ScreenSize()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Vector2 view = UnityEditor.Handles.GetMainGameViewSize();
                return new Vector2Int(Mathf.RoundToInt(view.x), Mathf.RoundToInt(view.y));
            }
#endif
            return new Vector2Int(Screen.width, Screen.height);
        }


#if UNITY_EDITOR
        public void SetPreviewBounds(Rect bounds)
        {
            if (_previewBounds == bounds) return;

            _previewBounds = bounds;
            UnityEditor.EditorUtility.SetDirty(this);
            _screen = Vector2Int.zero;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.update += EditorTick;
            _screen = Vector2Int.zero;
        }

        private void OnDisable()
        {
            UnityEditor.EditorApplication.update -= EditorTick;
        }

        private void OnValidate()
        {
            _margin = Mathf.Max(0f, _margin);
            _padTop = Mathf.Max(0f, _padTop);
            _padBottom = Mathf.Max(0f, _padBottom);
            _screen = Vector2Int.zero;
        }

        private void EditorTick()
        {
            if (this == null || Application.isPlaying) return;
            if (_previewBounds.width <= 0f || _previewBounds.height <= 0f) return;

            Vector2Int size = ScreenSize();
            if (size == _screen) return;

            _screen = size;
            Apply(_previewBounds, size);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif
    }
}
