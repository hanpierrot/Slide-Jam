using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        public static GameManager Instance { get; private set; }

        public GameConfig Config => _config;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("There is more than one GameManager in the scene; the extra component is removed.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            if (_config == null) Debug.LogError("GameManager has no GameConfig assigned.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
