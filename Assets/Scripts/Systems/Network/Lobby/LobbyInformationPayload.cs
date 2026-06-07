using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Systems.Interface;
using Unity.Netcode;

namespace Scripts.Systems.Network.Lobby
{
    public struct LobbyInformationPayload : INetworkSerializable, IEquatable<LobbyInformationPayload>
    {
        
        private PlayerLobbyInformation[] _players;
        public IReadOnlyList<PlayerLobbyInformation> Players => _players ?? Array.Empty<PlayerLobbyInformation>();
        
        public static LobbyInformationPayload FromLobbyInformation(LobbyInformation info)
        {
            return new LobbyInformationPayload
            {
                _players = info.Players.ToArray()
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int length = _players?.Length ?? 0;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader)
                _players = new PlayerLobbyInformation[length];

            for (int i = 0; i < length; i++)
                serializer.SerializeValue(ref _players[i]);
        }

        public bool Equals(LobbyInformationPayload other)
        {
            if (_players == null && other._players == null) return true;
            if (_players == null || other._players == null) return false;
            if (_players.Length != other._players.Length) return false;
            for (int i = 0; i < _players.Length; i++)
                if (!_players[i].Equals(other._players[i])) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is LobbyInformationPayload other && Equals(other);
        public override int GetHashCode() => _players?.GetHashCode() ?? 0;
        
        public LobbyInformation ToLobbyInformation()
        {
            var info = new LobbyInformation();
            foreach (var player in _players ?? Array.Empty<PlayerLobbyInformation>())
                info.AddPlayer(player);
            return info;
        }
        
    }
}