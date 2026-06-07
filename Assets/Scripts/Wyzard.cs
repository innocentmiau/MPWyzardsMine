using Analytics.Events;
using Unity.Netcode;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Wyzard : Character
{
    [SerializeField] private Transform              arm;
    [SerializeField] private NetworkVariable<float> cooldown = new(0.25f);
    [SerializeField] private NetworkVariable<float> damage = new(10.0f);
    [SerializeField] private Projectile             shotPrefab;
    [SerializeField] private Transform              shootPoint;
    [SerializeField] private ParticleSystem         levelUpPS;

    float   cooldownTimer;
    NetworkVariable<int>     _level = new(1);
    NetworkVariable<int>     _xp = new(0);
    NetworkVariable<int>     _maxXP = new(15);

    public int level => _level.Value;
    public int xp => _xp.Value;
    public int maxXP => _maxXP.Value;
    public bool isLocalPlayer => (networkObject) ? (networkObject.IsLocalPlayer) : (false);

    private static Wyzard localPlayer;
    public static Wyzard GetLocalPlayer()
    {
        if (localPlayer != null) return localPlayer;

        var allPlayers = FindObjectsByType<Wyzard>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player.IsLocalPlayer)
            {
                localPlayer = player;
                break;
            }
        }

        return localPlayer;
    }

    protected override void Start()
    {
        base.Start();

        cooldownTimer = cooldown.Value;
    }

    private InputAction _movementAction;
    private InputAction MovementAction => _movementAction ??= InputSystem.actions.FindAction("Move");
    
    void Update()
    {
        if (isDead)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
            return;
        }

        if (networkObject.IsLocalPlayer)
        {
            Vector3 moveDir = MovementAction.ReadValue<Vector2>().normalized * (speed * Time.deltaTime);
            // Tive de trocar para o novo input system(não tive tive, mas queria, pelo hábito)
            // TEACHER CODE:
            //moveDir.x = speed * Input.GetAxis("Horizontal");
            //moveDir.y = speed * Input.GetAxis("Vertical");
            //moveDir *= Time.deltaTime;

            transform.Translate(moveDir, Space.World);

            var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Enemy closestEnemy = null;
            float minDist = float.MaxValue;

            // Find closest
            foreach (var enemy in enemies)
            {
                if (enemy.isDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEnemy = enemy;
                }
            }

            Vector3 targetVector = Vector3.down;
            if (closestEnemy != null)
            {
                targetVector = (closestEnemy.transform.position + (Vector3.up * 8) - arm.transform.position).normalized;
            }

            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, targetVector);

            arm.transform.rotation = Quaternion.RotateTowards(arm.transform.rotation, targetRotation, Time.deltaTime * 360.0f);

            if ((closestEnemy != null) && (shotPrefab != null))
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0.0f)
                {
                    ShootRpc(shootPoint.position, shootPoint.rotation);

                    cooldownTimer = cooldown.Value;
                }
            }
        }

        UpdateAnimation();
    }

    [Rpc(SendTo.Server)]
    protected void ShootRpc(Vector3 pos, Quaternion rotation)
    {
        Projectile spawnedObject = Instantiate(shotPrefab, pos, rotation);
        spawnedObject.damage = damage.Value;
        spawnedObject.shooterClientId = OwnerClientId;
        spawnedObject.SetSpawnParameters(pos, NetworkManager.ServerTime.TimeAsFloat);
        NetworkObject nObj = spawnedObject.GetComponent<NetworkObject>();
        nObj.Spawn(true);

        projectileId++;
    }

    public void AddXP(int ammount)
    {
        if (!IsServer) return;

        _xp.Value += ammount;

        if (_xp.Value >= _maxXP.Value)
        {
            // Level up
            _xp.Value -= _maxXP.Value;
            _maxXP.Value = (int)(_maxXP.Value * 1.5f);
            _level.Value++;

            LevelUpRpc();
            SelectPowerupRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void LevelUpRpc()
    {
        Instantiate(levelUpPS, transform.position, Quaternion.identity);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void SelectPowerupRpc(RpcParams rpcParams = default)
    {
        PowerupSelector powerupSelector = FindFirstObjectByType<PowerupSelector>(FindObjectsInactive.Include);
        powerupSelector.gameObject.SetActive(true);
    }


    public void Upgrade(string upgradeName)
    {
        AnalyticsService.Instance.RecordEvent(new LevelGained(_level.Value, upgradeName));

        UpgradeRpc(upgradeName);
    }

    [Rpc(SendTo.Server)]
    public void UpgradeRpc(string upgradeName)
    {
        switch (upgradeName)
        {
            case "cooldown":
                cooldown.Value *= 0.9f;
                break;
            case "damage":
                damage.Value *= 1.25f;
                break;
            default:
                break;
        }
    }
}
