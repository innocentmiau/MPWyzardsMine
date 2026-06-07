using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI    levelText;
    [SerializeField] private Image              xpBarFill;

    Wyzard localPlayer;

    // Update is called once per frame
    void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = Wyzard.GetLocalPlayer();
        }
        if (localPlayer != null)
        {
            levelText.text = $"Level {localPlayer.level}";

            float p = (float)localPlayer.xp / (float)localPlayer.maxXP;
            xpBarFill.transform.localScale = new Vector3(p, 1.0f, 1.0f);
        }
    }
}
