using System.Collections;
using System.Collections.Generic;
using Scripts.Systems.Network;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class Character : NetworkBehaviour
{
    [SerializeField] public     Faction faction;
    [SerializeField] protected  float speed = 50.0f;

    protected Animator      animator;
    protected Vector3       prevPos;
    protected NetworkObject networkObject;
    protected HealthSystem  healthSystem;
    protected int           projectileId = 0;

    public bool isDead => healthSystem?.isDead ?? false;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        networkObject = GetComponent<NetworkObject>();
    }

    protected void UpdateAnimation()
    {
        Vector3 velocity = (transform.position - prevPos) / Time.deltaTime;

        animator.SetFloat("Speed", velocity.magnitude);

        if ((velocity.x < 0) && (transform.right.x > 0)) transform.rotation = Quaternion.Euler(0, 180, 0);
        else if ((velocity.x > 0) && (transform.right.x < 0)) transform.rotation = Quaternion.identity;

        prevPos = transform.position;
    }

    public void DealDamage(float damage)
    {
        if (healthSystem.DealDamage(damage))
        {
            // PLAYER died, return true = deaths
            SessionConnector.Instance.PlayerLost(networkObject.OwnerClientId);
        }
    }
}
