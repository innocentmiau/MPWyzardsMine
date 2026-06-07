using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private int   minPlayers = 2;
    [SerializeField] private float timeForFirstSpawn = 2;
    [SerializeField] private float spawnInterval = 10;
    [SerializeField] private int   spawnCount = 5;
    [SerializeField] private Enemy enemyPrefab;

    private float          spawnTimer;

    private int currentPlayers => (NetworkManager.Singleton) ? (NetworkManager.Singleton.ConnectedClients.Count) : (0);

    void Start()
    {
        spawnTimer = timeForFirstSpawn;
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton?.IsServer ?? false)
        {
            if (currentPlayers >= minPlayers)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0.0f)
                {
                    Spawn();
                    spawnTimer = spawnInterval;
                }
            }
            else if (currentPlayers == 0)
            {
                var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    Destroy(enemy.gameObject);
                }
                spawnTimer = timeForFirstSpawn;
            }
        }
    }

    void Spawn()
    {
        // Get all players
        var wyzards = FindObjectsByType<Wyzard>(FindObjectsSortMode.None);
        if (wyzards.Length == 0) return;

        float xMin = wyzards[0].transform.position.x;
        float yMin = wyzards[0].transform.position.y;
        float xMax = xMin;
        float yMax = yMin;

        foreach (var wyzard in wyzards)
        {
            xMin = Mathf.Min(xMin, wyzard.transform.position.x);
            xMax = Mathf.Max(xMax, wyzard.transform.position.x);
            yMin = Mathf.Min(yMin, wyzard.transform.position.y);
            yMax = Mathf.Max(yMax, wyzard.transform.position.y);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float x = Random.Range(xMin - 20, xMax + 20);
            float y = Random.Range(yMin - 20, yMax + 20);

            Enemy spawnedObject = Instantiate(enemyPrefab, new Vector3(x, y, 0), Quaternion.identity);
            NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
            networkObject.Spawn(true);
        }
    }
}
