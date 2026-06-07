using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Systems.Network.Game
{
    public class GameManager : MonoBehaviour
    {
        
        public static GameManager Instance { get; private set; }

        private readonly Dictionary<ulong, int> _killCounts = new();
        private float _gameStartTime;

        public float MatchDuration => Time.time - _gameStartTime;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _gameStartTime = Time.time;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RecordKill(ulong clientId)
        {
            if (!_killCounts.TryAdd(clientId, 1))
                _killCounts[clientId]++;
        }

        public int GetKills(ulong clientId) =>
            _killCounts.TryGetValue(clientId, out int kills) ? kills : 0;
    }
}