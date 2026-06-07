using TMPro;
using UnityEngine;

namespace Scripts.Systems.Interface
{
    public class LobbyPlayerListed : MonoBehaviour
    {

        [SerializeField] private TMP_Text tmpLeftSide;
        [SerializeField] private TMP_Text tmpRightSide;

        public void UpdateWithTexts(string leftSide, string rightSide)
        {
            if (tmpLeftSide) tmpLeftSide.text = leftSide;
            if (tmpRightSide) tmpRightSide.text = rightSide;
        }
        
    }
}