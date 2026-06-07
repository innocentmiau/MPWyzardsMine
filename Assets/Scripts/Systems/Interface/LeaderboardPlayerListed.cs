using TMPro;
using UnityEngine;

namespace Scripts.Systems.Interface
{
    public class LeaderboardPlayerListed : MonoBehaviour
    {
        [SerializeField] private TMP_Text tmpRank;
        [SerializeField] private TMP_Text tmpName;
        [SerializeField] private TMP_Text tmpRating;
        [SerializeField] private TMP_Text tmpTier;

        public void UpdateWithEntry(int rank, string playerName, int rating, string tier)
        {
            if (tmpRank)   tmpRank.text   = $"#{rank}";
            if (tmpName)   tmpName.text   = playerName;
            if (tmpRating) tmpRating.text = $"{rating}";
            if (tmpTier)   tmpTier.text   = tier ?? "Unranked";
        }

        public void Clear()
        {
            if (tmpRank)   tmpRank.text   = "";
            if (tmpName)   tmpName.text   = "";
            if (tmpRating) tmpRating.text = "";
            if (tmpTier)   tmpTier.text   = "";
        }
    }
}