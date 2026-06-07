using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Pellet : MonoBehaviour
{
    protected abstract void Grab(Wyzard wyzard);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var player = collision.GetComponent<Wyzard>();
            if (player != null)
            {
                Grab(player);
                Destroy(gameObject);
            }
        }
    }
}
