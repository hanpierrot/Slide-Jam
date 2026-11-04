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
        
        public int Id { get; private set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        public void Bind(int id, Sprite sprite, Color color)
        {
            Id = id;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = color;
        }
    }
}
