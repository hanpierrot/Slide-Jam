using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlockView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Vector3 _restScale;
        
        public int Id { get; private set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _restScale = transform.localScale;
        }

        public void Bind(int id, Color color)
        {
            Id = id;
            _spriteRenderer.color = color;
        }
        
        public void SetScaleFactor(float factor)
        {
            transform.localScale = _restScale * factor;
        }

        public void ResetScale()
        {
            transform.localScale = _restScale;
        }
    }
}
