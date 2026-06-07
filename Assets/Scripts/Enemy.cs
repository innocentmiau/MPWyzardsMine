using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Enemy : Character
{
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] float damage = 5.0f;

    float cooldownTimer;

    protected override void Start()
    {
        base.Start();

        healthSystem.onDeath += HealthSystem_onDeath;

        cooldownTimer = cooldown;
    }

    private void HealthSystem_onDeath()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Think();
        }
        UpdateAnimation();
    }

    void Think()
    {
        var wyzards = FindObjectsByType<Wyzard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Wyzard closestWyzard = null;
        float minDist = float.MaxValue;

        // Find closest
        foreach (var wyzard in wyzards)
        {
            if (wyzard.isDead) continue;

            float dist = Vector3.Distance(transform.position, wyzard.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestWyzard = wyzard;
            }
        }

        if (closestWyzard is null) return;

        if (minDist > 15)
        {
            Vector3 dir = (closestWyzard.transform.position - transform.position).normalized;

            Vector3 moveVector = dir * speed;

            transform.Translate(moveVector * Time.deltaTime, Space.World);
        }
        else
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0.0f)
            {
                cooldownTimer = cooldown;

                closestWyzard.DealDamage(damage);
            }
        }
    }
}
