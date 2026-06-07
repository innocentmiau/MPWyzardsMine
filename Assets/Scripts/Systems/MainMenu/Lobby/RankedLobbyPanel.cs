using System.Collections.Generic;
using Scripts.Systems.Interface;
using Scripts.Systems.Network;
using Scripts.Systems.Network.Lobby;
using TMPro;
using UnityEngine;

namespace Scripts.Systems.MainMenu.Lobby
{
    public class RankedLobbyPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform playersListTransform;
        [SerializeField] private TMP_Text tmpCountdown;

        private List<LobbyPlayerListed> _lobbyPlayersListed;
        private List<LobbyPlayerListed> LobbyPlayersListed
        {
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

        public void UpdateCountdown(float seconds)
        {
            if (tmpCountdown) tmpCountdown.text = $"Match starting in {seconds:F0}s";
        }
    }
}