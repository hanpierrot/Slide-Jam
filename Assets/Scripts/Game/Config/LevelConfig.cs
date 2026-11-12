using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    [Serializable]
    public struct TargetEntry
    {
        public int Color;
        public int Cap;
    }
    
    [Serializable]
    public class SpecialEntry
    {
        // Unity fills a new list element with zeros and ignores the initial values of a field, so every "none" is a zero here:
        // a switch (UseColor, Dual) guards each colour, instead of a -1 that a new entry would not get
        public int Row;
        public int Col;
        public bool UseColor; // false: the block keeps the colour that was rolled for its cell
        public int Color;
        public bool X2;
        public bool Dual; // true: a dual block, whose second colour is DualColor
        public int DualColor;
        public int Seal;
        public bool Spring;
        public int ChainLayers;
        public ChainLock ChainLock;
    }
    
    [CreateAssetMenu(fileName = "LevelConfig_", menuName = "Slide Jam/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private int _rows = 6;
        [SerializeField] private int _cols = 6;
        [SerializeField] private int _colors = 5;
        [SerializeField] private int _seed;
        [SerializeField] private int _activeSlots = 3;
        [SerializeField] private int _queueMax = 5;
        [SerializeField] private int _qOpenStart = 5;
        [SerializeField] private bool _qGrow;
        [SerializeField] private List<TargetEntry> _targets = new List<TargetEntry>();
        [SerializeField] private int _lockedSlotIndex = -1;
        [SerializeField] private List<SpecialEntry> _specials = new List<SpecialEntry>();
        
        [SerializeField, Min(1)] private int _number = 1;
        public int Number => _number;

        public int Rows => _rows;
        public int Cols => _cols;
        
        public LevelData ToLevelData(GameConfig config)
        {
            return LevelConverter.Convert(name, _rows, _cols, _colors, _seed, _activeSlots, _queueMax,
                _qOpenStart, _qGrow, _targets, _specials, _lockedSlotIndex, config.CapMin, config.CapMax);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Fill Reference Level 1")]
        private void FillReferenceLevel1()
        {
            UnityEditor.Undo.RecordObject(this, "Fill Reference Level 1");

            _rows = 6;
            _cols = 6;
            _colors = 5;
            _seed = 20261031;
            _activeSlots = 3;
            _queueMax = 5;
            _qOpenStart = 5;
            _qGrow = false;
            _lockedSlotIndex = -1;
            _specials = new List<SpecialEntry>();

            int[] colors = { 0, 1, 2, 3, 4, 1, 0, 3, 2, 4, 0, 1 };
            _targets = new List<TargetEntry>();
            foreach (int color in colors) _targets.Add(new TargetEntry { Color = color, Cap = 3 });

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
