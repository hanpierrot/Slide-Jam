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

        [SerializeField] private Sprite _queueOpen;
        [SerializeField] private Sprite _queueLocked;
        [SerializeField] private FruitArt[] _fruits = new FruitArt[5];

        [SerializeField] private Sprite _basketShadow; // added
        [SerializeField] private Sprite _basketFront; // added

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