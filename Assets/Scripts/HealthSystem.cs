using Analytics.Events;
using Unity.Netcode;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : NetworkBehaviour
{
    [SerializeField] Faction        _faction;
    [SerializeField] float          maxHealth = 100.0f;
    [SerializeField] GameObject     healthDisplay;
    [SerializeField] Image          fill;
    [SerializeField] GameObject[]   loot;
    [SerializeField] Color          flashColor = Color.white;


    private NetworkVariable<float>  health = new(1);
    private Flasher                 flasher;

    public bool isDead => (health.Value <= 0.0f);
    public Faction faction => _faction;

    public delegate void OnDeath();
    public event OnDeath onDeath;

    void Start()
    {
        flasher = GetComponent<Flasher>();

        if (NetworkManager.IsServer)
            health.Value = maxHealth;

        health.OnValueChanged += UpdateDisplay;
    }

    void UpdateDisplay(float prevValue, float currentValue)
    {
        if (flasher) flasher.Flash(flashColor, 0.2f);

        float p = Mathf.Clamp01(health.Value / maxHealth);
        if (fill)
        {
            fill.transform.localScale = new Vector3(p, 1.0f, 1.0f);
        }

        if ((healthDisplay != null) && (p <= 0.0f))
        {
            healthDisplay.SetActive(false);
        }
    }

    public bool DealDamage(float damage)
    {
        if (!NetworkManager.IsServer) return false;

        health.Value = Mathf.Clamp(health.Value - damage, 0, maxHealth);

        if (isDead)
        {
            if (loot.Length > 0)
            {
                var drop = loot[UnityEngine.Random.Range(0, loot.Length)];

                var lootObj = Instantiate(drop, transform.position, Quaternion.identity);
                var networkObject = lootObj.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
            }

            if (onDeath != null) onDeath();

            AnalyticsService.Instance.RecordEvent(new KillEnemy(0));
            return true;
        }
        return false;
    }
}
