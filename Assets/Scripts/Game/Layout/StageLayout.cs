using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    public class StageLayout : MonoBehaviour
    {
        [SerializeField] private Transform _slots;
        [SerializeField] private Transform _strip;
        [SerializeField] private Transform _queue;
        [SerializeField] private Transform _branch;
        [SerializeField] private Transform _overflow;
        [SerializeField] private float _overflowHeight = 0.7f;
        [SerializeField] private float _queueLift = 0.2f;
        [SerializeField] private Transform _grid;
        [SerializeField] private float _branchHeight = 1f;
        [SerializeField] private float _slotsHeight = 1.6f;
        [SerializeField] private float _stripHeight = 0.6f;
        [SerializeField] private float _queueHeight = 0.9f;
        [SerializeField] private float _gap = 0.35f;

        public float SlotsHeight => _slotsHeight;
        public float StripHeight => _stripHeight;
        public float QueueHeight => _queueHeight;
        public float Gap => _gap;
        
        public Rect Apply(int rows, int cols)
        {
            float branch = _branch != null ? _branchHeight + _gap : 0f;
            float lift = _overflow != null ? Mathf.Clamp(_queueLift, 0f, _gap) : 0f;
            float overflow = _overflow != null ? _overflowHeight - lift : 0f;
            float total = _slotsHeight + _stripHeight + _queueHeight + rows + 3f * _gap + branch + overflow;
            float top = total / 2f;

            float slotsY = top - _slotsHeight / 2f;
            float stripY = top - _slotsHeight - _gap - _stripHeight / 2f;
            float queueBottom = top - _slotsHeight - _gap - _stripHeight - _gap - _queueHeight;
            float queueY = queueBottom + _queueHeight / 2f + lift;
            float overflowY = queueBottom + lift - _overflowHeight / 2f;
            float branchY = queueBottom - overflow - _gap - _branchHeight / 2f;
            float gridY = -top + rows / 2f;

            bool changed = Place(_slots, slotsY);
            changed |= Place(_strip, stripY);
            changed |= Place(_queue, queueY);
            changed |= Place(_overflow, overflowY);
            changed |= Place(_branch, branchY);
            changed |= Place(_grid, gridY);
            changed |= ResetChildren(_grid);

#if UNITY_EDITOR
            if (changed && !Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

            Vector3 origin = transform.position;
            return new Rect(origin.x - cols / 2f, origin.y - top, cols, total);
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

        private static bool Place(Transform anchor, float y)
        {
            if (anchor == null) return false;

            var position = new Vector3(0f, y, 0f);
            if (anchor.localPosition == position) return false;

            anchor.localPosition = position;
            return true;
        }
    }
}
