using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    public class BlockPool
    {
        private readonly BlockView _prefab;
        private readonly Transform _root;
        private readonly Stack<BlockView> _inactive = new Stack<BlockView>();

        public BlockPool(BlockView prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
        }

        public int Total { get; private set; }
        public int Instantiated { get; private set; }
        
        public void Adopt(BlockView view)
        {
            view.gameObject.SetActive(false);
            _inactive.Push(view);
            Total++;
        }

        public BlockView Get()
        {
            BlockView view;
            if (_inactive.Count > 0)
            {
                view = _inactive.Pop();
            }
            else
            {
                view = UnityEngine.Object.Instantiate(_prefab, _root);
                Total++;
                Instantiated++;
            }

            view.gameObject.SetActive(true);
            view.ResetScale();
            return view;
        }
        
        public void Release(BlockView view)
        {
            if (!view.gameObject.activeSelf)
            {
                Debug.LogError("A block was released twice.");
                return;
            }

            view.gameObject.SetActive(false);
            _inactive.Push(view);
        }
    }
}
