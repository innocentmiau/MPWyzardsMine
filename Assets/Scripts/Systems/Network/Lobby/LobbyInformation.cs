using System.Collections.Generic;
using Scripts.Systems.Interface;

namespace Scripts.Systems.Network.Lobby
{
    public class LobbyInformation
    {
        private List<PlayerLobbyInformation> _players = new();
        public IReadOnlyList<PlayerLobbyInformation> Players => _players.AsReadOnly();

        public void AddPlayer(PlayerLobbyInformation player) => _players.Add(player);
        public void RemovePlayer(PlayerLobbyInformation player) => _players.Remove(player);
    }
}