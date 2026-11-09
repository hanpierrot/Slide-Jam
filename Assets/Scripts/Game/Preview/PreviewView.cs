using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using TMPro;
using UnityEngine;

namespace SlideJam.Game
{
    public class PreviewView : MonoBehaviour
    {
        private const int RingSortingOrder = 25;
        private const int BoxSortingOrder = 1;

        [SerializeField] private Sprite _ringWhite;
        [SerializeField] private Sprite _ringOrange;
        [SerializeField] private Color _ringWhiteColor = new Color32(26, 204, 255, 255);
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private string _toQueueFormat = "+{0} to the queue";
        [SerializeField] private string _loseText = "Will overflow: you lose";
        [SerializeField] private Color _orange = new Color(0.96f, 0.55f, 0.17f);
        [SerializeField] private Color _red = new Color(0.86f, 0.22f, 0.22f);

        private BoardView _board;
        private QueueView _queue;
        private GameConfig _config;
        private SquarePool _rings;
        private SquarePool _boxes;
        private Vector3 _ringScale = Vector3.one;
        private int _ringCount;

        public void Init(BoardView board, QueueView queue, GameConfig config)
        {
            _board = board;
            _queue = queue;
            _config = config;

            _rings = SquarePool.Obtain(_rings, transform, "Rings", RingSortingOrder);
            _boxes = SquarePool.Obtain(_boxes, transform, "Boxes", BoxSortingOrder);
            _rings.Warm(config.PreviewPoolSize);
            _boxes.Warm(config.QueueUnitBuffer + 8);
            Clear();
        }

        public void Show(MovePreview preview, BoardState state)
        {
            if (preview == null)
            {
                Clear();
                return;
            }

            bool toQueue = preview.ToQueue > 0;
            Sprite ring = toQueue ? _ringOrange : _ringWhite;
            Color tint = toQueue ? Color.white : _ringWhiteColor; // changed: only the white ring is tinted
            float ringWidth = _board.transform.lossyScale.x / transform.lossyScale.x;

            _ringCount = 0;
            foreach (Cell cell in preview.Cells)
            {
                Vector3 local = transform.InverseTransformPoint(_board.CellToWorld(cell.Row, cell.Col));
                SpriteRenderer square = _rings.Fit(_ringCount, ring, local, ringWidth, tint); // changed: was Color.white
                _ringScale = square.transform.localScale;
                _ringCount++;
            }

            _rings.HideFrom(_ringCount);
            ShowBoxes(preview, state);
            ShowLabel(preview);
        }

        public void Clear()
        {
            _ringCount = 0;
            _rings?.HideFrom(0);
            _boxes?.HideFrom(0);
            if (_label != null) _label.text = string.Empty;
        }

        private void Update()
        {
            if (_ringCount == 0 || _config == null) return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time / _config.PreviewPulseSeconds * Mathf.PI * 2f);
            Vector3 scale = _ringScale * Mathf.Lerp(1f, _config.PreviewPulseScale, wave);
            for (int i = 0; i < _ringCount; i++) _rings.Peek(i).transform.localScale = scale;
        }

        private void ShowBoxes(MovePreview preview, BoardState state)
        {
            int shown = 0;
            if (preview.ToQueue > 0)
            {
                float width = _queue.ChairSize * _queue.transform.lossyScale.x / transform.lossyScale.x;
                for (int k = 0; k < preview.ToQueue; k++)
                {
                    int index = state.Queue.Count + k;
                    if (index >= _queue.ChairCount) break;

                    Vector3 local = transform.InverseTransformPoint(_queue.ChairWorld(index));
                    _boxes.Fit(shown, _ringOrange, local, width, new Color(1f, 1f, 1f, 0.9f)); // changed: back to the plain orange sprite
                    shown++;
                }
            }

            _boxes.HideFrom(shown);
        }
        
        private void ShowLabel(MovePreview preview)
        {
            if (_label == null) return;

            if (preview.Lose)
            {
                _label.text = _loseText;
                _label.color = _red;
            }
            else if (preview.ToQueue > 0)
            {
                _label.text = string.Format(_toQueueFormat, preview.ToQueue);
                _label.color = _orange;
            }
            else
            {
                _label.text = string.Empty;
            }
        }
    }
}
