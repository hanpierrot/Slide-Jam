using UnityEngine;

namespace SlideJam.Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Slide Jam/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _capMin = 3;
        [SerializeField, Min(1)] private int _capMax = 5;

        public int CapMin => _capMin;
        public int CapMax => _capMax;
    }
}