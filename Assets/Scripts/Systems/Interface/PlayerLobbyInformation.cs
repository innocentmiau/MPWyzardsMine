using System;
using Unity.Netcode;
using Unity.Collections;

namespace Scripts.Systems.Interface
{
    public struct PlayerLobbyInformation : INetworkSerializable, IEquatable<PlayerLobbyInformation>
    {
        private FixedString64Bytes _id;
        private FixedString64Bytes _name;
        private int _rating;
        private int _rank;
        private FixedString32Bytes _tier;

        public string Id => _id.ToString();
        public string Name => _name.ToString();
        public int Rating => _rating;
        public int Rank => _rank;
        public string Tier => _tier.ToString();

        public PlayerLobbyInformation(string id, string name, int rating, int rank, string tier)
        {
            _id = id;
            _name = name;
            _rating = rating;
            _rank = rank;
            _tier = tier;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _id);
            serializer.SerializeValue(ref _name);
            serializer.SerializeValue(ref _rating);
            serializer.SerializeValue(ref _rank);
            serializer.SerializeValue(ref _tier);
        }

        public bool Equals(PlayerLobbyInformation other) => _id == other._id && _name == other._name && _rating == other._rating && _rank == other._rank && _tier == other._tier;
        
        public override bool Equals(object obj) => obj is PlayerLobbyInformation other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_id, _name, _rating, _rank, _tier);
        
    }
}