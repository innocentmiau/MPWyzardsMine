using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupSelector : MonoBehaviour
{
    [SerializeField] private int                nOptions = 2;
    [SerializeField] private PowerupOption[]    options;
    [SerializeField] private Transform          container;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        // Delete all items
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        var availableOptions = new List<PowerupOption>(options);

        for (int i = 0; i < nOptions; i++)
        {
            int r = Random.Range(0, availableOptions.Count);

            var newOption = Instantiate(availableOptions[r], container);
            availableOptions.RemoveAt(r);
        }
    }
}
