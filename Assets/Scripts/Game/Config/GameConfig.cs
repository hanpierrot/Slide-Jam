using UnityEngine;

namespace SlideJam.Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Slide Jam/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Rules (DESIGN 5.1)")]
        [SerializeField, Min(1)] private int _capMin = 3;
        [SerializeField, Min(1)] private int _capMax = 5;

        [Header("Feel: animation timings (DESIGN 7)")]
        [SerializeField, Min(0f)] private float _snapSeconds = 0.13f;
        [SerializeField, Min(0f)] private float _snapBackSeconds = 0.14f;
        [SerializeField, Min(0f)] private float _clearSeconds = 0.17f;
        [SerializeField, Min(1f)] private float _pulseScale = 1.15f;
        [SerializeField, Min(0f)] private float _fallBaseSeconds = 0.16f;
        [SerializeField, Min(0f)] private float _fallPerCellSeconds = 0.09f;
        [SerializeField, Min(0f)] private float _reviveSeconds = 0.35f;

        [Header("Input")]
        [SerializeField, Min(0f)] private float _lockDistance = 8f;
        [SerializeField, Min(1f)] private float _referenceWidth = 360f;

        [Header("Capacity")]
        [SerializeField, Min(0)] private int _queueUnitBuffer = 6;
        [SerializeField, Min(1)] private int _stripVisible = 5;

        public int CapMin => _capMin;
        public int CapMax => _capMax;

        public float SnapSeconds => _snapSeconds;
        public float SnapBackSeconds => _snapBackSeconds;
        public float ClearSeconds => _clearSeconds;
        public float PulseScale => _pulseScale;
        public float FallBaseSeconds => _fallBaseSeconds;
        public float FallPerCellSeconds => _fallPerCellSeconds;
        public float ReviveSeconds => _reviveSeconds;

        public float LockDistance => _lockDistance;
        public float ReferenceWidth => _referenceWidth;

        public int QueueUnitBuffer => _queueUnitBuffer;
        public int StripVisible => _stripVisible;
    }
}
