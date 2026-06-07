using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Core;
using Scripts.Systems.Interface;
using Scripts.Systems.Network.Game;
using Scripts.Systems.Network.Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.Systems.Network
{
    public class SessionConnector : NetworkBehaviour
    {

        public event Action<LobbyInformation> NewPlayerJoined;
        public event Action GameStarting;
        
        public static SessionConnector Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public LobbyInformation LobbyInformation { get; private set; } = new LobbyInformation();

        public void ResetLobbyInformation() => LobbyInformation = new LobbyInformation();
        
        [Rpc(SendTo.Server)]
        public void SendToServerPlayerInformationRpc(PlayerLobbyInformation info)
        {
            LobbyInformation.AddPlayer(info);
            NewPlayerJoined?.Invoke(LobbyInformation);
            
            LobbyInformationPayload payLoad = LobbyInformationPayload.FromLobbyInformation(LobbyInformation);
            SendLobbyInformationToPlayerRpc(payLoad);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SendLobbyInformationToPlayerRpc(LobbyInformationPayload payLoad)
        {
            if (IsServer) return;
            LobbyInformation lobbyInformation = payLoad.ToLobbyInformation();
            NewPlayerJoined?.Invoke(lobbyInformation);
        }
        
        //[Rpc(SendTo.Server)]
        public void PlayerLost(ulong loserClientId)
        {
            Debug.Log("Player lost index: " + loserClientId);
            ulong winnerClientId = NetworkManager.Singleton.ConnectedClientsIds
                .First(id => id != loserClientId);

            PlayerLobbyInformation loserInfo = LobbyInformation.Players[(int)loserClientId];
            PlayerLobbyInformation winnerInfo = LobbyInformation.Players.First(p => !p.Equals(loserInfo));

            int winnerKills = GameManager.Instance?.GetKills(winnerClientId) ?? 0;
            int loserKills  = GameManager.Instance?.GetKills(loserClientId)  ?? 0;
            float matchDuration = GameManager.Instance?.MatchDuration ?? 0f;

            (int winV, int loseV) = CalculateElo(winnerInfo.Rating, loserInfo.Rating, matchDuration, winnerKills, loserKills);
            string debugMessage = $"---[ MATCH INFORMATION ]---\nMatch duration: {matchDuration:F0}s\nWinner '{winnerInfo.Name}': {winnerKills} kills.\nLoser: '{loserInfo.Name}': {loserKills} kills.";
            SendDebugToAllRpc(debugMessage);
            
            SendEndScreenToPlayerRpc(winV, true, RpcTarget.Single(winnerClientId, RpcTargetUse.Temp));
            SendEndScreenToPlayerRpc(loseV, false, RpcTarget.Single(loserClientId, RpcTargetUse.Temp));
            NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
        
        [Rpc(SendTo.Everyone)]
        public void SendDebugToAllRpc(string message) => Debug.Log(message);
        
        [Rpc(SendTo.SpecifiedInParams)]
        public void SendEndScreenToPlayerRpc(int eloChange, bool isWinner, RpcParams rpcParams = default)
        {
            //EndScreenReceived?.Invoke(eloChange, isWinner);
            string extraText = isWinner ? $"Since won, it's +{eloChange}" : $"Since lost, it's -{eloChange}";
            Debug.Log($"You {(isWinner?"Won":"Lost")}! Elo change: {eloChange} ({extraText})");
            if (SessionManager.Instance.IsRankedMatch)
                _ = SessionManager.Instance.UpdateEloAsync(isWinner, eloChange);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void RankedMatchIsStartingRpc()
        {
            if (SessionManager.Instance.IsLobbyHost)
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void CasualLobbyIsStartingRpc()
        {
            Debug.Log("Host wants to start the game.");
            GameStarting?.Invoke();
            if (SessionManager.Instance.IsLobbyHost)
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        
        private (int winnerValue, int loserValue) CalculateElo(int winnerRating, int loserRating, 
            float matchDuration, int winnerKills, int loserKills)
        {
            float baselineDuration = Constants.BASE_TIME_DURATION;
            float maxDuration = Constants.MAX_TIME_BONUS_DURATION;
            int maxBonusPoints = 12;

            float eWinner = 1f / (1f + Mathf.Pow(10f, (loserRating - winnerRating) / 400f));
            float expMod = (1f - eWinner) * 2f;

            int totalKills = winnerKills + loserKills;
            float killMod = totalKills > 0 ? Mathf.Lerp(0.65f, 1.35f, (float)winnerKills / totalKills) : 1f;

            int baseValue = Mathf.RoundToInt(Constants.BASE_ELO_EARNING * expMod * killMod);

            int timeBonus = matchDuration <= baselineDuration
                ? Mathf.RoundToInt(Mathf.Lerp(-maxBonusPoints, 0f, matchDuration / baselineDuration))
                : Mathf.RoundToInt(Mathf.Lerp(0f, maxBonusPoints, (matchDuration - baselineDuration) / (maxDuration - baselineDuration)));

            int winnerValue = baseValue + timeBonus;
            int loserValue = baseValue - timeBonus;

            return (winnerValue, loserValue);
        }
        
    }
}