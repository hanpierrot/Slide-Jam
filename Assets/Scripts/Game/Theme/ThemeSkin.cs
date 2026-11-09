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

        [Serializable] // added: where things sit inside the basket art, as fractions of the basket width; they belong to this art, not to the rules
        public class BasketLayout
        {
            public float InteriorWidth = 0.6f;
            public float PipY = 0.053f;
            public float PipWidth = 0.213f;
            public float PipFill = 0.95f;
            public float FruitInPip = 0.9f;
        }

        [SerializeField] private Color[] _palette = // added: colour index 0..4 (DESIGN 9); the fallback colour of a block, queue unit and so on
        {
            new Color(0.90f, 0.25f, 0.30f),
            new Color(0.25f, 0.45f, 0.90f),
            new Color(0.45f, 0.75f, 0.30f),
            new Color(0.98f, 0.60f, 0.15f),
            new Color(0.60f, 0.35f, 0.75f),
        };

        [SerializeField] private BasketLayout _basketLayout = new BasketLayout(); // added
        [SerializeField] private Sprite _queueOpen;
        [SerializeField] private Sprite _queueLocked;
        [SerializeField] private FruitArt[] _fruits = new FruitArt[5];

        [SerializeField] private Sprite _basketShadow; // added
        [SerializeField] private Sprite _basketFront; // added

        public int PaletteLength => _palette.Length; // added
        public BasketLayout Basket => _basketLayout; // added
        public Color ColorOf(int color) => _palette[color]; // added
        public Sprite QueueOpen => _queueOpen;
        public Sprite QueueLocked => _queueLocked;
        public Sprite BasketShadow => _basketShadow; // added
        public Sprite BasketFront => _basketFront; // added

        public FruitArt Fruit(int color)
        {
            return color >= 0 && color < _fruits.Length ? _fruits[color] : default;
        }
    }
}