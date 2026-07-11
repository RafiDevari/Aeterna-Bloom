using UnityEngine;

/// <summary>
/// State pertumbuhan monster, murni ditentukan dari nilai Growth.
/// Growth 0f = 0%, 1f = 100%, 2f = 200%, dst (Growth TIDAK di-clamp ke 0-1 lagi).
/// </summary>
public enum GrowthState
{
    Seed,        // Benih
    Growing,     // Tumbuh
    Overgrowth,  // Overgrowth (> 100%)
    Mutated      // Mutated (>= 200%)
}

/// <summary>
/// Pasangan GrowthState -> Sprite, dipakai untuk konfigurasi sprite per state
/// lewat Inspector. Beda-beda tiap prefab child class.
/// </summary>
[System.Serializable]
public struct GrowthStateSprite
{
    public GrowthState state;
    public Sprite sprite;
}

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

    [Tooltip("Sprite untuk tiap GrowthState. Kosongkan salah satu state kalau tidak mau sprite " +
             "berubah di state itu (sprite terakhir tetap dipakai). Beda-beda tiap prefab child class.")]
    [SerializeField] private GrowthStateSprite[] growthStateSprites;

    [Tooltip("Otomatis sesuaikan ukuran BoxCollider2D monster ini mengikuti bounds sprite, " +
             "tiap kali sprite berubah (mis. pas GrowthState pindah dan sprite ganti ukuran).")]
    [SerializeField] private bool autoFitCollider = true;

    [Tooltip("BoxCollider2D milik monster ini sendiri (bukan milik ContainmentUnit). Opsional — " +
             "auto-cari kalau kosong. Kalau prefab tidak punya Collider2D, fitur ini otomatis di-skip.")]
    [SerializeField] private BoxCollider2D monsterCollider;


    //────────────────────────────────────────────────────────
    // Base Stats
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [Tooltip("0f = 0%, 1f = 100%, dst. Tidak di-clamp ke 0-1 karena dipakai untuk Overgrowth/Mutated.")]
    [SerializeField] protected float growth = 0f;
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    //────────────────────────────────────────────────────────
    // Growth Stages
    //────────────────────────────────────────────────────────

    [Header("Growth Stages")]
    [Tooltip("Growth minimal (0-1 = 0%-100%) supaya monster berubah dari Benih ke Tumbuh.\n" +
             "Beda-beda tiap jenis monster — atur lewat Inspector di masing-masing prefab child class.")]
    [SerializeField, Range(0f, 1f)] protected float growThreshold = 0.3f;

    [Tooltip("Growth di ATAS nilai ini dianggap Overgrowth. Default 1 = 100%.")]
    [SerializeField] protected float overgrowthThreshold = 1f;

    [Tooltip("Growth mencapai/melewati nilai ini dianggap Mutated. Default 2 = 200%.")]
    [SerializeField] protected float mutatedThreshold = 2f;

    [Tooltip("Saat status sedang Mutated, growth harus turun SAMPAI nilai ini (biasanya 1 = 100%)\n" +
             "supaya lepas dari status Mutated. Selama belum sampai sini, monster TETAP dianggap Mutated\n" +
             "walaupun growth sudah sempat turun di bawah mutatedThreshold (200%).")]
    [SerializeField] protected float mutationRecoveryThreshold = 1f;

    [SerializeField] private GrowthState currentGrowthState = GrowthState.Seed;

    // Pernah mencapai state Tumbuh -> mengunci floor growth, growth tidak bisa turun lagi di bawah growThreshold.
    private bool hasGrown;

    // Flag sticky: sekali Mutated, tetap dianggap Mutated sampai growth turun ke mutationRecoveryThreshold.
    private bool isMutated;

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
    [Tooltip("Durasi 'makan' BAWAAN monster ini (detik), dipakai sebagai fallback kalau Feed() dipanggil tanpa durasi eksplisit dari luar. Beda-beda tiap jenis monster.")]
    [SerializeField] protected float feedDuration = 1f;

    [Tooltip("Jeda waktu (detik) sebelum monster ini bisa diberi makan lagi, DIHITUNG SETELAH proses makan selesai.")]
    [SerializeField] protected float feedCooldown = 5f;

    protected float feedDurationTimer = 0f;
    protected float feedCooldownTimer = 0f;

    private FoodType pendingFeedFood;
    private bool pendingFeedWasOnCooldown;
    private bool hasPendingFeedEffect;

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
    public System.Action<GrowthState, GrowthState> OnGrowthStateChanged;
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

    /// <summary>
    /// Nilai growth mentah. 1f = 100%. TIDAK di-clamp ke 0-1 (dipakai untuk Overgrowth/Mutated).
    /// Begitu monster pernah mencapai Tumbuh (hasGrown = true), nilai ini tidak bisa turun
    /// lagi di bawah growThreshold, jadi monster tidak akan pernah kembali jadi Benih.
    /// </summary>
    public float Growth
    {
        get => growth;
        protected set
        {
            float floor = hasGrown ? growThreshold : 0f;
            growth = Mathf.Max(value, floor);

            OnGrowthChanged?.Invoke(growth);
            UpdateGrowthState();
        }
    }

    /// <summary>Growth dalam bentuk persen, murni untuk kemudahan baca/tampilan (1f growth = 100f).</summary>
    public float GrowthPercent => growth * 100f;

    public GrowthState CurrentGrowthState => currentGrowthState;

    /// <summary>True kalau monster ini pernah mencapai state Tumbuh (tidak bisa balik jadi Benih lagi).</summary>
    public bool HasGrown => hasGrown;

    /// <summary>True selagi status Mutated masih aktif (sticky sampai growth turun ke mutationRecoveryThreshold).</summary>
    public bool IsMutated => isMutated;

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

    /// <summary>
    /// Durasi makan BAWAAN monster ini, tanpa modifikasi apapun dari luar.
    /// Ini murni data milik monster (dipakai Employee sebagai basis perhitungan
    /// durasi final lewat Employee.CalculateFeedDuration).
    /// </summary>
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

    /// <summary>True selagi monster sedang dalam proses makan (dari Feed() sampai durasi makan habis).</summary>
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

        if (monsterCollider == null)
            monsterCollider = GetComponentInChildren<BoxCollider2D>();

        ApplySprite();
        SyncInitialGrowthState();
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

    // di MonsterBase
    protected virtual void Update()
    {
        TickFeedDuration();
        TickFeedCooldown();
        TickPassiveGrowth();

        switch (currentGrowthState)
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

    protected virtual void OnSeedUpdate() { }
    protected virtual void OnGrowingUpdate() { }
    protected virtual void OnOvergrowthUpdate() { }
    protected virtual void OnMutatedUpdate() { }

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
            CompleteFeeding();
    }

    private void CompleteFeeding()
    {
        feedDurationTimer = 0f;
        feedCooldownTimer = feedCooldown; // cooldown baru mulai setelah makan selesai

        if (hasPendingFeedEffect)
        {
            hasPendingFeedEffect = false;

            if (pendingFeedWasOnCooldown)
                OnFedDuringCooldown(pendingFeedFood);
            else
                OnMonsterFed(pendingFeedFood);
        }

        OnFeedFinished?.Invoke();

        Debug.Log($"[{MonsterName}] Selesai makan, cooldown mulai : {feedCooldown}s");
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
    // Growth State Helper
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Sinkronisasi state di awal (Awake), berdasarkan nilai growth yang sudah
    /// di-set lewat Inspector, tanpa memicu event OnGrowthStateChanged.
    /// </summary>
    private void SyncInitialGrowthState()
    {
        hasGrown = growth >= growThreshold;
        isMutated = growth >= mutatedThreshold;
        currentGrowthState = ComputeGrowthState();

        ApplySpriteForState(currentGrowthState);
    }

    private GrowthState ComputeGrowthState()
    {
        if (growth >= mutatedThreshold)
            return GrowthState.Mutated;

        if (growth > overgrowthThreshold)
            return GrowthState.Overgrowth;

        if (growth >= growThreshold)
            return GrowthState.Growing;

        return GrowthState.Seed;
    }

    /// <summary>
    /// Dipanggil setiap kali Growth berubah. Menghitung ulang GrowthState,
    /// termasuk logika sticky Mutated (harus turun sampai mutationRecoveryThreshold
    /// dulu, bukan cuma di bawah mutatedThreshold, baru dianggap lepas dari Mutated).
    /// </summary>
    private void UpdateGrowthState()
    {
        GrowthState previous = currentGrowthState;
        GrowthState computed = ComputeGrowthState();

        if (computed >= GrowthState.Growing)
            hasGrown = true;

        if (isMutated)
        {
            if (growth <= mutationRecoveryThreshold)
                isMutated = false;
            else
                computed = GrowthState.Mutated;
        }
        else if (computed == GrowthState.Mutated)
        {
            isMutated = true;
        }

        if (computed == previous)
            return;

        currentGrowthState = computed;

        ApplySpriteForState(currentGrowthState);

        OnGrowthStateChanged?.Invoke(previous, computed);
        OnGrowthStateChange(previous, computed);

        Debug.Log($"[{MonsterName}] Growth State : {previous} -> {computed} (growth = {GrowthPercent:F0}%)");
    }

    //────────────────────────────────────────────────────────
    // Sprite per Growth State
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Cari sprite untuk GrowthState tertentu dari array growthStateSprites.
    /// Override ini kalau child class butuh logika lebih rumit (mis. random
    /// variant, kombinasi dengan mood, animasi transisi, dll).
    /// Return null kalau tidak ada yang cocok -> sprite lama tidak diganti.
    /// </summary>
    protected virtual Sprite GetSpriteForState(GrowthState state)
    {
        foreach (var entry in growthStateSprites)
        {
            if (entry.state == state && entry.sprite != null)
                return entry.sprite;
        }

        return null;
    }

    /// <summary>
    /// Terapkan sprite sesuai GrowthState (lewat GetSpriteForState). Dipanggil
    /// otomatis tiap kali GrowthState berubah, tapi boleh juga dipanggil manual.
    /// </summary>
    protected void ApplySpriteForState(GrowthState state)
    {
        Sprite target = GetSpriteForState(state);

        if (target != null)
            MonsterSprite = target; // pakai property, biar ApplySprite() ikut kepanggil
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    protected virtual void OnMonsterUpdate() { }

    protected virtual void OnMoodChange(int oldMood, int newMood) { }

    protected virtual void OnGrowthStateChange(GrowthState oldState, GrowthState newState) { }

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

    /// <summary>
    /// Beri makan monster ini.
    /// </summary>
    /// <param name="food">Jenis makanan yang diberikan.</param>
    /// <param name="feedDurationOverride">
    /// Durasi makan FINAL (detik) yang sudah dihitung oleh pemanggil (biasanya
    /// Employee.CalculateFeedDuration, yang nantinya bisa memperhitungkan
    /// multiplier per jenis employee / level / skill).
    /// Kalau null, fallback ke <see cref="FeedDuration"/> bawaan monster ini,
    /// supaya Feed() tetap aman dipanggil langsung tanpa lewat Employee
    /// (misal dari testing atau sistem lain).
    /// </param>
    public virtual bool Feed(FoodType food, float? feedDurationOverride = null)
    {
        if (IsFeeding)
        {
            Debug.Log($"[{MonsterName}] Menolak makan : sedang dalam proses makan.");
            return false;
        }

        pendingFeedFood = food;
        pendingFeedWasOnCooldown = feedCooldownTimer > 0f;
        hasPendingFeedEffect = true;

        feedDurationTimer = Mathf.Max(0f, feedDurationOverride ?? feedDuration);

        OnFed?.Invoke(food);

        Debug.Log($"[{MonsterName}] Mulai makan : {food}, durasi makan : {feedDurationTimer}s" +
            (pendingFeedWasOnCooldown ? " (masih dalam cooldown)" : ""));

        // Durasi 0 = makan instan, langsung selesai di frame yang sama.
        if (feedDurationTimer <= 0f)
            CompleteFeeding();

        return true;
    }
}