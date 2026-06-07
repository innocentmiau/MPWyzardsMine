using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Scripts.Systems.Network.Game
{
    public class PingDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI pingText;
        private float _updateInterval = 1f;
        private float _timer;

        private void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

            _timer += Time.deltaTime;
            if (_timer < _updateInterval) return;
            _timer = 0f;

            int ping = GetLocalPing();
            pingText.text = $"{ping} ms";
            pingText.color = GetPingColor(ping);
        }

        private int GetLocalPing()
        {
            // Host pings himself as 0
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                return 0;

            return (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport
                .GetCurrentRtt(NetworkManager.ServerClientId) / 2);
        }

        private Color GetPingColor(int ping) => ping switch
        {
            < 60  => Color.green,
            < 120 => Color.yellow,
            < 200 => new Color(1f, 0.5f, 0f), // orange
            _     => Color.red
        };
    }
}