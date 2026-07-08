using UnityEngine;

/// <summary>
/// Base class semua monster.
/// MonsterBase hanya menyediakan data dasar dan API.
/// Mekanik mood/growth sepenuhnya ditentukan oleh subclass.
/// </summary>
/// 

public class MonsterBase : MonoBehaviour
{
    //────────────────────────────────────────────────────────
    // Identity
    //────────────────────────────────────────────────────────

    [Header("Identity")]
    [SerializeField] private string monsterName = "Unknown Monster";

    [Header("Visual")]
    [Tooltip("SpriteRenderer untuk menampilkan sprite monster. Auto-cari kalau kosong.")]
    [SerializeField] protected SpriteRenderer monsterRenderer;
    [SerializeField] protected Sprite monsterSprite;


    //────────────────────────────────────────────────────────
    // Base Stats
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [SerializeField] protected float growth = 0f;
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    [Header("Growth")]
    [SerializeField] protected float passiveGrowthInterval = 4f;
    [SerializeField] protected float passiveGrowthAmount = 0.01f;

    private float passiveGrowthTimer;

    //────────────────────────────────────────────────────────
    // Environment
    //────────────────────────────────────────────────────────

    [Header("Environment")]
    [SerializeField]
    protected float suitableTemperature = 20f;

    //────────────────────────────────────────────────────────
    // Feeding
    //────────────────────────────────────────────────────────

    [Header("Feeding")]
    [Tooltip("Berapa lama (detik) monster ini sedang 'makan' setelah di-Feed, sebelum dianggap selesai. Beda-beda tiap jenis monster.")]
    [SerializeField] protected float feedDuration = 1f;

    [Tooltip("Jeda waktu (detik) sebelum monster ini bisa diberi makan lagi, DIHITUNG SETELAH feedDuration selesai.")]
    [SerializeField] protected float feedCooldown = 5f;

    protected float feedDurationTimer = 0f;
    protected float feedCooldownTimer = 0f;

    //────────────────────────────────────────────────────────
    // References
    //────────────────────────────────────────────────────────

    protected ContainmentUnit myUnit;
    protected MonsterContext context;

    //────────────────────────────────────────────────────────
    // Events
    //────────────────────────────────────────────────────────

    public System.Action<int> OnMoodChanged;
    public System.Action<float> OnGrowthChanged;
    public System.Action<FoodType> OnFed;
    public System.Action OnFeedFinished;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    public string MonsterName
    {
        get => monsterName;
        protected set => monsterName = value;
    }

    public SpriteRenderer MonsterRenderer => monsterRenderer;

    public Sprite MonsterSprite
    {
        get => monsterSprite;
        set
        {
            monsterSprite = value;
            ApplySprite();
        }
    }

    public float Growth
    {
        get => growth;
        protected set
        {
            growth = Mathf.Clamp01(value);
            OnGrowthChanged?.Invoke(growth);
        }
    }

    public int Mood
    {
        get => mood;
        protected set
        {
            int previous = mood;

            mood = Mathf.Clamp(value, minMood, maxMood);

            if (previous == mood)
                return;

            OnMoodChanged?.Invoke(mood);
            OnMoodChange(previous, mood);

            Debug.Log($"[{MonsterName}] Mood : {previous} -> {mood}");
        }
    }

    public float SuitableTemperature
    {
        get => suitableTemperature;
        protected set => suitableTemperature = value;
    }

    public float FeedDuration
    {
        get => feedDuration;
        protected set => feedDuration = value;
    }

    public float FeedCooldown
    {
        get => feedCooldown;
        protected set => feedCooldown = value;
    }

    public MonsterContext Context => context;

    public ContainmentUnit Unit => myUnit;

    /// <summary>True selagi monster sedang dalam proses makan (dari Feed() sampai feedDuration habis).</summary>
    public bool IsFeeding => feedDurationTimer > 0f;

    /// <summary>Boleh diberi makan hanya kalau tidak sedang makan DAN cooldown sudah habis.</summary>
    public bool CanBeFed => !IsFeeding && feedCooldownTimer <= 0f;


    //────────────────────────────────────────────────────────
    // Init
    //────────────────────────────────────────────────────────

    public virtual void InitUnit(ContainmentUnit unit)
    {
        myUnit = unit;
        context = new MonsterContext(unit);
    }

    //────────────────────────────────────────────────────────
    // Unity
    //────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        if (monsterRenderer == null)
            monsterRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplySprite();
    }

    private void ApplySprite()
    {
        if (monsterRenderer != null && monsterSprite != null)
            monsterRenderer.sprite = monsterSprite;
    }
    protected virtual void Update()
    {
        TickFeedDuration();
        TickFeedCooldown();
        TickPassiveGrowth();
        OnMonsterUpdate();
    }

    //────────────────────────────────────────────────────────
    // Helper
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Helper timer.
    /// Return true setiap interval detik.
    /// </summary>
    protected bool Every(ref float timer, float interval)
    {
        timer += Time.deltaTime;

        if (timer < interval)
            return false;

        timer = 0f; 
        return true;
    }

    private void TickFeedDuration()
    {
        if (feedDurationTimer <= 0f)
            return;

        feedDurationTimer -= Time.deltaTime;

        if (feedDurationTimer <= 0f)
        {
            feedDurationTimer = 0f;
            feedCooldownTimer = feedCooldown; // cooldown baru mulai setelah makan selesai
            OnFeedFinished?.Invoke();

            Debug.Log($"[{MonsterName}] Selesai makan, cooldown mulai : {feedCooldown}s");
        }
    }

    private void TickFeedCooldown()
    {
        if (feedCooldownTimer > 0f)
            feedCooldownTimer -= Time.deltaTime;
    }

    protected virtual void TickPassiveGrowth()
    {
        if (Every(ref passiveGrowthTimer, passiveGrowthInterval))
        {
            ModifyGrowth(passiveGrowthAmount);
        }
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    protected virtual void OnMonsterUpdate() { }

    protected virtual void OnMoodChange(int oldMood, int newMood) { }

    protected virtual void OnMonsterFed(FoodType food) { }
    protected virtual void OnFedDuringCooldown(FoodType food) { }

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    public void ModifyMood(int delta)
    {
        Mood += delta;
    }

    public void SetMood(int value)
    {
        Mood = value;
    }

    public void ModifyGrowth(float delta)
    {
        Growth += delta;
    }
    public virtual bool Feed(FoodType food)
    {
        if (IsFeeding)
        {
            Debug.Log($"[{MonsterName}] Menolak makan : sedang dalam proses makan.");
            return false;
        }

        bool wasOnCooldown = feedCooldownTimer > 0f;

        if (wasOnCooldown)
            OnFedDuringCooldown(food);
        else
            OnMonsterFed(food);

        feedDurationTimer = feedDuration;

        OnFed?.Invoke(food);

        Debug.Log($"[{MonsterName}] Diberi makan : {food}, durasi makan : {feedDuration}s" + (wasOnCooldown ? " (masih dalam cooldown)" : ""));

        return true;
    }
}