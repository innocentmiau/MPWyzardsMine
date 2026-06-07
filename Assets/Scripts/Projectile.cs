using Scripts.Systems.Network.Game;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    public Faction  faction;
    public float    speed = 200.0f;
    public float    damage = 100.0f;
    public float    duration = 5.0f;
    public ulong    shooterClientId;
    public NetworkVariable<float> shotTime = new();
    public NetworkVariable<Vector3> origin = new();
    
    private Vector3 _prevPos;
    private TrailRenderer _trailRenderer;
    private Vector3 _pendingOrigin;
    private float _pendingTime;

    public void SetSpawnParameters(Vector3 originPos, float time)
    {
        _pendingOrigin = originPos;
        _pendingTime = time;
    }

    private void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            origin.Value = _pendingOrigin;
            shotTime.Value = _pendingTime;
        }

        _prevPos = origin.Value;
        ComputePosition();
        _trailRenderer.emitting = true;
    }

    void Update()
    {
        ComputePosition();
        if (NetworkManager.IsServer)
        {
            // Improved collision detection, works better on server side
            var hits = Physics2D.LinecastAll(_prevPos, transform.position);
            foreach (var hit in hits)
            {
                var healthSystem = hit.collider.GetComponent<HealthSystem>();
                if ((healthSystem != null) && (!healthSystem.isDead))
                {
                    // Check factions
                    if (healthSystem.faction != faction)
                    {
                        bool killed = healthSystem.DealDamage(damage);
                        if (killed && hit.collider.GetComponent<Enemy>() != null)
                            GameManager.Instance?.RecordKill(shooterClientId);
                        Destroy(gameObject);
                    }
                }
            }
            duration -= Time.deltaTime;
            if (duration <= 0.0f)
            {
                Destroy(gameObject);
            }
            else
            {
                _prevPos = transform.position;
            }
        }
    }

    void ComputePosition()
    {
        if (!IsSpawned) return;
        float timeElapsed = (NetworkManager.ServerTime.TimeAsFloat - shotTime.Value);
        if (timeElapsed < 0) timeElapsed = 0;
        transform.position = origin.Value + transform.up * timeElapsed * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (NetworkManager.IsClient) return;

        // Check if collision was with something with a health system that's still alive
        var healthSystem = collision.GetComponent<HealthSystem>();
        if ((healthSystem != null) && (!healthSystem.isDead))
        {
            // Check factions
            if (healthSystem.faction == faction) return;

            // Run damage
            healthSystem.DealDamage(damage);

            Destroy(gameObject);
        }
    }
}
