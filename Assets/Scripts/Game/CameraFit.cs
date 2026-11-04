using UnityEngine;

namespace SlideJam.Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private float _margin = 0.5f;
        [SerializeField, HideInInspector] private int _previewCols;
        [SerializeField, HideInInspector] private int _previewRows;

        private Camera _camera;
        private int _cols;
        private int _rows;
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

        public void Fit(int cols, int rows)
        {
            _cols = cols;
            _rows = rows;
            _screen = ScreenSize();
            _hasFit = true;
            Apply(cols, rows, _screen);
        }

        private void Update()
        {
            if (Application.isPlaying && _hasFit && ScreenSize() != _screen) Fit(_cols, _rows);
        }

        private void Apply(int cols, int rows, Vector2Int screen)
        {
            if (screen.x <= 0 || screen.y <= 0) return;

            float aspect = (float)screen.x / screen.y;
            float byWidth = (cols + 2f * _margin) / (2f * aspect);
            float byHeight = (rows + 2f * _margin) / 2f;
            Cam.orthographicSize = Mathf.Max(byWidth, byHeight);
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
        public void SetPreviewSize(int cols, int rows)
        {
            if (_previewCols == cols && _previewRows == rows) return;

            _previewCols = cols;
            _previewRows = rows;
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
            _screen = Vector2Int.zero;
        }

        private void EditorTick()
        {
            if (this == null || Application.isPlaying) return;
            if (_previewCols <= 0 || _previewRows <= 0) return;

            Vector2Int size = ScreenSize();
            if (size == _screen) return;

            _screen = size;
            Apply(_previewCols, _previewRows, size);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif
    }
}
