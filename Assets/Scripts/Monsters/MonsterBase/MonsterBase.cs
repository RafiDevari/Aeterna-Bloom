using UnityEngine;

/// <summary>
/// Base class semua monster.
/// MonsterBase di-split jadi beberapa file partial class biar tidak jadi 1 file raksasa :
///   - MonsterBase.cs           : identity, visual, mood, environment, references, lifecycle Unity (file ini)
///   - MonsterBase.Growth.cs    : GrowthState, growth stages, sprite-per-state
///   - MonsterBase.Feeding.cs   : sistem makan (Feed, cooldown, durasi)
///   - MonsterBase.Research.cs  : sistem research (level, condition, trigger, durasi)
/// Semua file di atas adalah SATU class yang sama (partial) -- child class (mis. MonsterTest1234)
/// tidak perlu tahu ini di-split, API-nya sama persis seperti sebelum di-pecah.
/// </summary>
public partial class MonsterBase : MonoBehaviour
{
    //────────────────────────────────────────────────────────
    // Identity
    //────────────────────────────────────────────────────────

    [Header("Identity")]
    [SerializeField] private string monsterName = "Unknown Monster";

    public string MonsterName
    {
        get => monsterName;
        protected set => monsterName = value;
    }

    //────────────────────────────────────────────────────────
    // Visual
    //────────────────────────────────────────────────────────

    [Header("Visual")]
    [Tooltip("SpriteRenderer untuk menampilkan sprite monster. Auto-cari kalau kosong.")]
    [SerializeField] protected SpriteRenderer monsterRenderer;
    [SerializeField] protected Sprite monsterSprite;

    [Tooltip("Otomatis sesuaikan ukuran BoxCollider2D monster ini mengikuti bounds sprite, " +
             "tiap kali sprite berubah (mis. pas GrowthState pindah dan sprite ganti ukuran).")]
    [SerializeField] private bool autoFitCollider = true;

    [Tooltip("BoxCollider2D milik monster ini sendiri (bukan milik ContainmentUnit). Opsional — " +
             "auto-cari kalau kosong. Kalau prefab tidak punya Collider2D, fitur ini otomatis di-skip.")]
    [SerializeField] private BoxCollider2D monsterCollider;

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

    private void ApplySprite()
    {
        if (monsterRenderer != null && monsterSprite != null)
            monsterRenderer.sprite = monsterSprite;

        FitColliderToSprite();
    }

    /// <summary>
    /// Sesuaikan ukuran & offset monsterCollider mengikuti bounds sprite saat ini.
    /// No-op kalau autoFitCollider mati atau monster ini tidak punya Collider2D
    /// sendiri (misal klik-nya sepenuhnya ditangani oleh ContainmentUnit).
    /// </summary>
    private void FitColliderToSprite()
    {
        if (!autoFitCollider || monsterCollider == null) return;
        if (monsterRenderer == null || monsterRenderer.sprite == null) return;

        Bounds spriteBounds = monsterRenderer.sprite.bounds;

        monsterCollider.size = spriteBounds.size;
        monsterCollider.offset = spriteBounds.center;
    }

    //────────────────────────────────────────────────────────
    // Mood
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    public System.Action<int> OnMoodChanged;

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

    protected virtual void OnMoodChange(int oldMood, int newMood) { }

    public void ModifyMood(int delta)
    {
        Mood += delta;
    }

    public void SetMood(int value)
    {
        Mood = value;
    }

    //────────────────────────────────────────────────────────
    // Environment
    //────────────────────────────────────────────────────────

    [Header("Environment")]
    [SerializeField]
    protected float suitableTemperature = 20f;

    public float SuitableTemperature
    {
        get => suitableTemperature;
        protected set => suitableTemperature = value;
    }

    //────────────────────────────────────────────────────────
    // References
    //────────────────────────────────────────────────────────

    protected ContainmentUnit myUnit;
    protected MonsterContext context;

    public MonsterContext Context => context;

    public ContainmentUnit Unit => myUnit;

    //────────────────────────────────────────────────────────
    // Init
    //────────────────────────────────────────────────────────

    public virtual void InitUnit(ContainmentUnit unit)
    {
        myUnit = unit;
        context = new MonsterContext(unit);
    }

    //────────────────────────────────────────────────────────
    // Unity Lifecycle
    //────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        if (monsterRenderer == null)
            monsterRenderer = GetComponentInChildren<SpriteRenderer>();

        if (monsterCollider == null)
            monsterCollider = GetComponentInChildren<BoxCollider2D>();

        ApplySprite();
        SyncInitialGrowthState();  // MonsterBase.Growth.cs
        CheckAutoResearch();       // MonsterBase.Research.cs -- jaga-jaga growth awal sudah penuhi syarat Auto
    }

    protected virtual void Update()
    {
        TickFeedDuration();     // MonsterBase.Feeding.cs
        TickFeedCooldown();     // MonsterBase.Feeding.cs
        TickPassiveGrowth();    // MonsterBase.Growth.cs
        TickResearchDuration(); // MonsterBase.Research.cs -- jalanin timer research Manual yang sedang berlangsung
        CheckAutoResearch();    // MonsterBase.Research.cs -- dicek tiap frame, kondisi Custom bisa berubah kapan saja

        switch (CurrentGrowthState)
        {
            case GrowthState.Seed:
                OnSeedUpdate();
                break;
            case GrowthState.Growing:
                OnGrowingUpdate();
                break;
            case GrowthState.Overgrowth:
                OnOvergrowthUpdate();
                break;
            case GrowthState.Mutated:
                OnMutatedUpdate();
                break;
        }

        OnMonsterUpdate(); // tetap ada, buat logika yang jalan di SEMUA state (mis. cooldown mood-zero effect)
    }

    protected virtual void OnMonsterUpdate() { }

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
}