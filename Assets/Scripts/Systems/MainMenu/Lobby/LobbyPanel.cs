using System.Collections.Generic;
using Scripts.Systems.Interface;
using Scripts.Systems.Network;
using Scripts.Systems.Network.Lobby;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Scripts.Systems.MainMenu.Lobby
{
    public class LobbyPanel : MonoBehaviour
    {
        
        [SerializeField] private TMP_Text tmpLobbyOwner;
        [SerializeField] private TMP_Text tmpLobbyCode;
        [SerializeField] private RectTransform playersListTransform;
        [SerializeField] private BetterButton startButton;
        [SerializeField] private GameObject cancelLobbyPart;
        [SerializeField] private TMP_Text tmpLobbyStatus;
        [SerializeField] private GameObject leaveLobbyButton;
        
        public void UpdateWindow()
        {
            tmpLobbyOwner.text = SessionManager.Instance.Session.Host;
            tmpLobbyCode.text = $"Code: {SessionManager.Instance.Session.Code}";
            
            bool isHost = SessionManager.Instance.Session.IsHost;
            startButton.SetInteractable(isHost);
            startButton.gameObject.SetActive(isHost);
            cancelLobbyPart.SetActive(isHost);
            leaveLobbyButton.SetActive(!isHost);
            
            tmpLobbyStatus.gameObject.SetActive(!isHost);
            if (!isHost)
                UpdateLobbyStatus("<i>Waiting on host to start...");
            
            //UpdatePlayersInLobby();
        }

        public void UpdateLobbyStatus(string newText)
        {
            if (tmpLobbyStatus) tmpLobbyStatus.text = newText;
        }

        private List<LobbyPlayerListed> _lobbyPlayersListed;

        private List<LobbyPlayerListed> LobbyPlayersListed {
            get
            {
                if (_lobbyPlayersListed == null)
                {
                    _lobbyPlayersListed = new List<LobbyPlayerListed>();
                    _lobbyPlayersListed.AddRange(playersListTransform.GetComponentsInChildren<LobbyPlayerListed>(true));
                }
                return _lobbyPlayersListed;
            }
        }
        
        public void UpdatePlayersInLobby(LobbyInformation lobbyInformation)
        {
            if (!SessionManager.Instance || SessionManager.Instance.Session == null) return;
            if (lobbyInformation.Players.Count == 0) return;
            
            string hostName = lobbyInformation.Players[0].Name;
            tmpLobbyOwner.text = hostName;

            // doing for in case i ever increase the MaxPlayers for the lobby, we never know about the future, who wouldn't want a 50 player lobby vampire survivors?
            
            //ISession currentSession = SessionManager.Instance.Session;
            
            int playerCount = lobbyInformation.Players.Count;
            for (int i = 0; i < playerCount; i++)
            {
                string playerName = lobbyInformation.Players[i].Name;
                string ratingText = $"{lobbyInformation.Players[i].Rating} rating";
                playerName = playerName.Replace("#", "<color=#8D8D8D>#");
                LobbyPlayersListed[i]?.UpdateWithTexts(playerName, ratingText);
            }

            for (int i = playerCount; i < LobbyPlayersListed.Count; i++)
                LobbyPlayersListed[i].UpdateWithTexts("", "");
        } 

        public void CopyLobbyCode()
        {
            if (SessionManager.Instance == null || SessionManager.Instance.Session == null) return;
            GUIUtility.systemCopyBuffer = SessionManager.Instance.Session.Code ?? "";
        }

    }
}