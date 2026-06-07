using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupOption : MonoBehaviour
{
    public void SelectOption(string option)
    {
        var powerupSelector = FindFirstObjectByType<PowerupSelector>();
        powerupSelector.gameObject.SetActive(false);

        var localPlayer = Wyzard.GetLocalPlayer();
        localPlayer.Upgrade(option);
    }
}
