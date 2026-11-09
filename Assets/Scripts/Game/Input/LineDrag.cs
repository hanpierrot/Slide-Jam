using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public readonly struct LineDrag
    {
        public readonly Axis Axis;
        public readonly int Index;
        public readonly float Offset;

        public LineDrag(Axis axis, int index, float offset)
        {
            Axis = axis;
            Index = index;
            Offset = offset;
        }

        public int Notches => Mathf.RoundToInt(Offset);
    }
}
