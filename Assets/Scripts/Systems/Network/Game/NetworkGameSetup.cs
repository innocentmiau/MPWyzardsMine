using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Scripts.Systems.Network.Game
{
    public class NetworkGameSetup : MonoBehaviour
    {

        [SerializeField] private List<Transform> playerSpawnLocations;
        [SerializeField] private List<Wyzard> playerPrefabs;
        [SerializeField] private TextMeshProUGUI textJoinCode;
        [SerializeField] private bool enableAnalytics;

        private int _playerPrefabIndex = 0;

        private void Start()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            bool isHost = networkManager.IsHost;

            if (isHost)
            {
                //networkManager.OnClientConnectedCallback += OnClientConnected;
                networkManager.OnClientDisconnectCallback += OnClientDisconnected;

                if (textJoinCode != null && SessionManager.Instance?.Session != null)
                {
                    textJoinCode.text = $"JoinCode:{SessionManager.Instance.Session.Code}";
                    textJoinCode.gameObject.SetActive(true);
                }
                
                SpawnAllPlayers();
            }
            else
            {
                if (textJoinCode != null)
                    textJoinCode.gameObject.SetActive(false);
            }

            InitAnalytics();
        }

        private void SpawnAllPlayers()
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                SpawnPlayer(clientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            Debug.Log($"Spawning player for client {clientId}, prefab index = {_playerPrefabIndex}");

            Vector3 spawnPos = Vector3.zero;
            Wyzard[] currentPlayers = FindObjectsByType<Wyzard>(FindObjectsSortMode.None);
            foreach (Transform spawnLocation in playerSpawnLocations)
            {
                float closestDist = float.MaxValue;
                foreach (Wyzard player in currentPlayers)
                {
                    float d = Vector3.Distance(player.transform.position, spawnLocation.position);
                    closestDist = Mathf.Min(closestDist, d);
                }
                if (closestDist > 20)
                {
                    spawnPos = spawnLocation.position;
                    break;
                }
            }

            Wyzard spawnedObject = Instantiate(playerPrefabs[_playerPrefabIndex], spawnPos, Quaternion.identity);
            NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, true);
            _playerPrefabIndex = (_playerPrefabIndex + 1) % playerPrefabs.Count;
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Player {clientId} disconnected!");
        }

        private void InitAnalytics()
        {
            if (enableAnalytics)
            {
                ConsentState consentState = EndUserConsent.GetConsentState();
                consentState.AnalyticsIntent = ConsentStatus.Granted;
                EndUserConsent.SetConsentState(consentState);
            }
        }
    }
}