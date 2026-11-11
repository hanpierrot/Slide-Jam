using UnityEngine;

namespace SlideJam.Game
{
    public class StageLayout : MonoBehaviour
    {
        [SerializeField] private StageLayoutData _data;
        [SerializeField] private Transform _slots;
        [SerializeField] private Transform _strip;
        [SerializeField] private Transform _queue;
        [SerializeField] private Transform _branch;
        [SerializeField] private Transform _dots;
        [SerializeField] private Transform _grid;

        public StageLayoutData Data => _data;

        public void SetData(StageLayoutData data)
        {
            _data = data;
        }

        // The Stage rectangle from the saved data; it moves nothing, so Play mode can use it to fit the camera.
        // The top is the top of the slots band and the bottom is the bottom of the board (the Grid anchor is the board's centre).
        public Rect Measure(int rows, int cols)
        {
            Vector3 origin = transform.position;
            if (_data == null) return new Rect(origin.x - cols / 2f, origin.y - rows / 2f, cols, rows);

            float top = _data.SlotsPosition.y + _data.SlotsHeight / 2f;
            float bottom = _data.GridPosition.y - rows / 2f;
            return new Rect(origin.x - cols / 2f, origin.y + bottom, cols, top - bottom);
        }

        // An authoring tool: puts the anchors where the data says. It does nothing in Play mode, where the scene keeps the
        // positions that were set in the Editor and only the camera follows the screen. Called by the Setup Scene and Refresh Layout menus.
        public Rect Apply(int rows, int cols)
        {
            if (Application.isPlaying || _data == null || !_data.HasPositions) return Measure(rows, cols);

            bool changed = Place(_slots, _data.SlotsPosition);
            changed |= Place(_strip, _data.StripPosition);
            changed |= Place(_queue, _data.QueuePosition);
            changed |= Place(_branch, _data.BranchPosition);
            changed |= Place(_dots, _data.DotsPosition);
            changed |= Place(_grid, _data.GridPosition);
            changed |= ResetChildren(_grid);

            if (_data.HasScales)
            {
                changed |= Scale(_slots, _data.SlotsScale);
                changed |= Scale(_strip, _data.StripScale);
                changed |= Scale(_queue, _data.QueueScale);
                changed |= Scale(_branch, _data.BranchScale);
                changed |= Scale(_dots, _data.DotsScale);
                changed |= Scale(_grid, _data.GridScale);
            }

#if UNITY_EDITOR
            if (changed) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

            return Measure(rows, cols);
        }

        private static bool ResetChildren(Transform anchor)
        {
            if (anchor == null) return false;

            bool changed = false;
            foreach (Transform child in anchor)
            {
                if (child.localPosition == Vector3.zero) continue;

                child.localPosition = Vector3.zero;
                changed = true;
            }

            return changed;
        }

        private static bool Scale(Transform anchor, Vector3 scale)
        {
            if (anchor == null) return false;
            if (anchor.localScale == scale) return false;

            anchor.localScale = scale;
            return true;
        }

        private static bool Place(Transform anchor, Vector3 position)
        {
            if (anchor == null) return false;
            if (anchor.localPosition == position) return false;

            anchor.localPosition = position;
            return true;
        }
    }
}
