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
/// Bagian MonsterBase yang mengurus Growth & GrowthState : threshold, transisi Benih/Tumbuh/
/// Overgrowth/Mutated, sprite per state, dan passive growth over time.
/// </summary>
public partial class MonsterBase
{
    //────────────────────────────────────────────────────────
    // Base Stats (Growth)
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [Tooltip("0f = 0%, 1f = 100%, dst. Tidak di-clamp ke 0-1 karena dipakai untuk Overgrowth/Mutated.")]
    [SerializeField] protected float growth = 0f;

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

    [Tooltip("Sprite untuk tiap GrowthState. Kosongkan salah satu state kalau tidak mau sprite " +
             "berubah di state itu (sprite terakhir tetap dipakai). Beda-beda tiap prefab child class.")]
    [SerializeField] private GrowthStateSprite[] growthStateSprites;

    //────────────────────────────────────────────────────────
    // Events
    //────────────────────────────────────────────────────────

    public System.Action<float> OnGrowthChanged;
    public System.Action<GrowthState, GrowthState> OnGrowthStateChanged;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

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

    /// <summary>Threshold growth minimal agar monster menjadi Growing.</summary>
    public float GrowThreshold => growThreshold;

    /// <summary>True selagi status Mutated masih aktif (sticky sampai growth turun ke mutationRecoveryThreshold).</summary>
    public bool IsMutated => isMutated;

    //────────────────────────────────────────────────────────
    // Tick
    //────────────────────────────────────────────────────────

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

        // Auto research entry berbasis GrowthState (mis. level 9) akan tetap ke-selesaikan di frame ini
        // juga lewat CheckAutoResearch() yang jalan tiap frame di Update() (MonsterBase.cs).
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
        if (growthStateSprites == null) return null;

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
    // Virtual Hooks (dipanggil dari Update() di MonsterBase.cs)
    //────────────────────────────────────────────────────────

    protected virtual void OnSeedUpdate() { }
    protected virtual void OnGrowingUpdate() { }
    protected virtual void OnOvergrowthUpdate() { }
    protected virtual void OnMutatedUpdate() { }

    protected virtual void OnGrowthStateChange(GrowthState oldState, GrowthState newState) { }

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    public void ModifyGrowth(float delta)
    {
        if (delta > 0 && Mood == 0 && CurrentGrowthState == GrowthState.Seed)
        {
            return;
        }
        Growth += delta;
    }
}