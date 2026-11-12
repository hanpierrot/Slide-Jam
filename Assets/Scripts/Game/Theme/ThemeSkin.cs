using System;
using UnityEngine;

namespace SlideJam.Game
{
    [CreateAssetMenu(fileName = "ThemeSkin", menuName = "Slide Jam/Theme Skin")]
    public class ThemeSkin : ScriptableObject
    {
        [Serializable]
        public struct FruitArt
        {
            public Sprite Icon;
            public Sprite BasketBack;
            public Sprite BasketEmpty;
            public Sprite BasketLocked;
            public Sprite PipEmpty;
            public Sprite Chip;
        }

        [Serializable]
        public class BasketLayout
        {
            public float InteriorWidth = 0.6f;
            public float PipY = 0.053f;
            public float PipWidth = 0.213f;
            public float PipFill = 0.95f;
            public float FruitInPip = 0.9f;
        }

        [SerializeField] private Color[] _palette =
        {
            new Color(0.90f, 0.25f, 0.30f),
            new Color(0.25f, 0.45f, 0.90f),
            new Color(0.45f, 0.75f, 0.30f),
            new Color(0.98f, 0.60f, 0.15f),
            new Color(0.60f, 0.35f, 0.75f),
        };

        [SerializeField] private BasketLayout _basketLayout = new BasketLayout(); 
        [SerializeField] private Sprite _queueOpen;
        [SerializeField] private Sprite _queueLocked;
        [SerializeField] private FruitArt[] _fruits = new FruitArt[5];

        [SerializeField] private Sprite _basketShadow;
        [SerializeField] private Sprite _basketFront; 
        [SerializeField] private Sprite _badgeX2;
        [SerializeField] private Sprite _badgeSpring;
        [SerializeField] private Sprite _dualDivider;
        [SerializeField] private Sprite _sealOutline;
        [SerializeField] private Sprite[] _sealBadges = new Sprite[3];

        public int PaletteLength => _palette.Length;
        public BasketLayout Basket => _basketLayout; 
        public Color ColorOf(int color) => _palette[color];
        public Sprite QueueOpen => _queueOpen;
        public Sprite QueueLocked => _queueLocked;
        public Sprite BasketShadow => _basketShadow;
        public Sprite BasketFront => _basketFront;
        public Sprite BadgeX2 => _badgeX2;
        public Sprite BadgeSpring => _badgeSpring;
        public Sprite DualDivider => _dualDivider;
        public Sprite SealOutline => _sealOutline;

        public Sprite SealBadge(int number)
        {
            if (_sealBadges == null || _sealBadges.Length == 0 || number < 1) return null;

            return _sealBadges[Mathf.Min(number, _sealBadges.Length) - 1];
        }

        public FruitArt Fruit(int color)
        {
            return color >= 0 && color < _fruits.Length ? _fruits[color] : default;
        }
    }
}