using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    public class SquarePool
    {
        private const string Prefix = "Pool_";
        
        private readonly Transform _container;
        private readonly int _sortingOrder;
        private readonly List<SpriteRenderer> _items = new List<SpriteRenderer>();
        private bool _warned;
        
        // Edit Mode only: false stops the pool from touching undo and the dirty flag (the tests use it)
        public static bool TrackEdits = true;

        private SquarePool(Transform owner, string name, int sortingOrder, Transform found)
        {
            _sortingOrder = sortingOrder;
            if (found != null)
            {
                _container = found; // adopt: the objects already exist in the scene or prefab, only their content changes at run time
                foreach (Transform child in found)
                {
                    if (child.TryGetComponent(out SpriteRenderer renderer)) _items.Add(renderer);
                }

                return;
            }

            var go = new GameObject(Prefix + name);
            go.transform.SetParent(owner, false);
            Register(go);
            _container = go.transform;
        }

        public int Count => _items.Count;
        public int Warmed { get; private set; }
        public bool IsAlive => _container != null;
        
        public static SquarePool Obtain(SquarePool existing, Transform owner, string name, int sortingOrder)
        {
            if (existing != null && existing.IsAlive) return existing;

            return new SquarePool(owner, name, sortingOrder, owner.Find(Prefix + name));
        }

        public void Warm(int count)
        {
            while (_items.Count < count) Create();
            if (count > Warmed) Warmed = count;
        }

        public SpriteRenderer Get(int index)
        {
            if (index >= _items.Count && Warmed > 0 && !_warned)
            {
                _warned = true;
                Debug.LogWarning($"{_container.name} grew past its {Warmed} warmed squares (index {index}). Raise the warm-up count.");
            }

            while (_items.Count <= index) Create();
            return _items[index];
        }

        public SpriteRenderer Fit(int index, Sprite sprite, Vector3 position, float width, Color color)
        {
            SpriteRenderer square = Get(index);
            if (sprite == null)
            {
                square.gameObject.SetActive(false);
                return square;
            }

            square.sprite = sprite;
            float scale = width / sprite.bounds.size.x;
            Place(square, position, new Vector3(scale, scale, 1f), color);
            return square;
        }

        public SpriteRenderer Stretch(int index, Sprite sprite, Vector3 position, float width, float height, Color color)
        {
            SpriteRenderer square = Get(index);
            if (sprite == null)
            {
                square.gameObject.SetActive(false);
                return square;
            }

            square.sprite = sprite;
            Place(square, position, new Vector3(width, height, 1f), color);
            return square;
        }

        public void Hide(int index)
        {
            if (index >= 0 && index < _items.Count) _items[index].gameObject.SetActive(false);
        }

        public SpriteRenderer Peek(int index)
        {
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }

        public void HideFrom(int first)
        {
            for (int i = Mathf.Max(0, first); i < _items.Count; i++) _items[i].gameObject.SetActive(false);
        }

        private static void Place(SpriteRenderer square, Vector3 position, Vector3 scale, Color color)
        {
            square.transform.localPosition = position;
            square.transform.localScale = scale;
            square.color = color;
            square.gameObject.SetActive(true);
        }

        private void Create()
        {
            var go = new GameObject("Square " + _items.Count);
            go.transform.SetParent(_container, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = _sortingOrder;
            go.SetActive(false);
            Register(go);
            _items.Add(renderer);
        }

        private static void Register(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying || !TrackEdits) return;

            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Squares");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
#endif
        }
    }
}
