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
        [SerializeField, Min(0f)] private float _flightSeconds = 0.36f;
        [SerializeField, Min(0f)] private float _flightStaggerSeconds = 0.04f;
        [SerializeField, Range(0f, 1f)] private float _flightEndScale = 0.45f;
        [SerializeField, Min(0f)] private float _slotPopSeconds = 0.26f;
        [SerializeField, Min(1f)] private float _slotPopScale = 1.12f;
        [SerializeField, Min(0f)] private float _slotEnterSeconds = 0.26f;
        [SerializeField, Range(0.3f, 1f)] private float _overflowUnitScale = 0.8f;
        [SerializeField, Min(0f)] private float _overflowFadeSeconds = 0.15f;

        [Header("Input")]
        [SerializeField, Min(0f)] private float _lockDistance = 8f;
        [SerializeField, Min(1f)] private float _referenceWidth = 360f;

        [Header("Capacity")]
        [SerializeField, Min(0)] private int _queueUnitBuffer = 6;
        [SerializeField, Min(1)] private int _stripVisible = 5;
        [SerializeField, Min(1)] private int _flightPoolSize = 16;

        public int CapMin => _capMin;
        public int CapMax => _capMax;

        public float SnapSeconds => _snapSeconds;
        public float SnapBackSeconds => _snapBackSeconds;
        public float ClearSeconds => _clearSeconds;
        public float PulseScale => _pulseScale;
        public float FallBaseSeconds => _fallBaseSeconds;
        public float FallPerCellSeconds => _fallPerCellSeconds;
        public float ReviveSeconds => _reviveSeconds;
        public float FlightSeconds => _flightSeconds;
        public float FlightStaggerSeconds => _flightStaggerSeconds;
        public float FlightEndScale => _flightEndScale;
        public float SlotPopSeconds => _slotPopSeconds;
        public float SlotPopScale => _slotPopScale;
        public float SlotEnterSeconds => _slotEnterSeconds;
        public int FlightPoolSize => _flightPoolSize;
        public float OverflowUnitScale => _overflowUnitScale;
        public float OverflowFadeSeconds => _overflowFadeSeconds;
        
        public float LockDistance => _lockDistance;
        public float ReferenceWidth => _referenceWidth;

        public int QueueUnitBuffer => _queueUnitBuffer;
        public int StripVisible => _stripVisible;
    }
}
