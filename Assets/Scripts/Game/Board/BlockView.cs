using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlockView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private SpriteRenderer _badge;
        [SerializeField] private float _badgeWidth = 0.36f;
        [SerializeField] private Vector2 _badgeOffset = new Vector2(0.3f, 0.3f);
        [SerializeField] private SpriteRenderer _icon2;
        [SerializeField] private SpriteRenderer _divider;
        [SerializeField] private float _dualScale = 0.6f;
        [SerializeField] private float _dualOffset = 0.2f;
        [SerializeField] private SpriteRenderer _outline;
        [SerializeField, Range(0f, 1f)] private float _sealedAlpha = 0.45f;

        private SpriteRenderer _spriteRenderer;
        private Vector3 _restScale;
        private Vector3 _iconScale = Vector3.one;

        public int Id { get; private set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _restScale = transform.localScale;
            if (_icon != null) _iconScale = _icon.transform.localScale;
        }

        public void Bind(int id, Color color, Sprite icon, Sprite badge = null, Sprite secondIcon = null, Sprite divider = null,
            Sprite sealOutline = null)
        {
            Id = id;
            bool skinned = icon != null && _icon != null;
            bool dual = skinned && secondIcon != null && _icon2 != null;
            bool sealedBlock = sealOutline != null;
            Color tint = sealedBlock ? new Color(1f, 1f, 1f, _sealedAlpha) : Color.white;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = skinned;
                _icon.color = tint;
                _icon.transform.localScale = dual ? _iconScale * _dualScale : _iconScale;
                _icon.transform.localPosition = dual ? new Vector3(-_dualOffset, _dualOffset, 0f) : Vector3.zero;
            }

            ShowDual(dual, secondIcon, divider, tint);
            ShowOutline(sealOutline);
            _spriteRenderer.color = skinned ? Color.white : color;
            ShowBadge(badge);
        }

        public void SetScaleFactor(float factor)
        {
            transform.localScale = _restScale * factor;
        }

        public void ResetScale()
        {
            transform.localScale = _restScale;
        }

        private void ShowDual(bool dual, Sprite secondIcon, Sprite divider, Color tint) 
        {
            if (_icon2 != null)
            {
                _icon2.enabled = dual;
                if (dual)
                {
                    _icon2.sprite = secondIcon;
                    _icon2.transform.localScale = _iconScale * _dualScale;
                    _icon2.transform.localPosition = new Vector3(_dualOffset, -_dualOffset, 0f);
                }
            }

            if (_divider != null)
            {
                bool shown = dual && divider != null;
                _divider.enabled = shown;
                if (shown) _divider.sprite = divider;
            }
        }
        
        private void ShowOutline(Sprite outline)
        {
            if (_outline == null) return;

            _outline.enabled = outline != null;
            if (outline != null) _outline.sprite = outline;
        }

        private void ShowBadge(Sprite badge)
        {
            if (_badge == null) return;

            bool shown = badge != null;
            _badge.enabled = shown;
            if (!shown) return;

            _badge.sprite = badge;
            float scale = _badgeWidth / badge.bounds.size.x;
            _badge.transform.localScale = new Vector3(scale, scale, 1f);
            _badge.transform.localPosition = new Vector3(_badgeOffset.x, _badgeOffset.y, 0f);
        }
    }
}