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
/// Pasangan GrowthState -> GameObject, dipakai untuk konfigurasi object visual per state
/// lewat Inspector. Beda-beda tiap prefab child class.
/// </summary>
[System.Serializable]
public struct GrowthStateObject
{
    public GrowthState state;
    public GameObject visualObject;
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

    // Menandakan apakah broadcast pertumbuhan > 200% sudah pernah ter-trigger untuk tanaman ini
    private bool growth200BroadcastTriggered;

    [Header("Growth")]
    [SerializeField] protected float passiveGrowthInterval = 4f;
    [SerializeField] protected float passiveGrowthAmount = 0.01f;

    private float passiveGrowthTimer;

    [Tooltip("GameObject visual untuk tiap GrowthState. Aktifkan visual untuk state saat ini, " +
             "dan nonaktifkan yang lain. Beda-beda tiap prefab child class.")]
    [SerializeField] private GrowthStateObject[] growthStateObjects;

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

            // Trigger broadcast when growth crosses 200% (2.0f)
            if (growth >= 2.0f)
            {
                if (!growth200BroadcastTriggered)
                {
                    growth200BroadcastTriggered = true;
                    FacilityHUD.ShowBroadcast($"{MonsterName} growth is more than 200%!", "System");
                }
            }
            else
            {
                growth200BroadcastTriggered = false;
            }
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
        if (IsMutated)
        {
            if (Every(ref passiveGrowthTimer, passiveGrowthInterval * 2f))
            {
                ModifyGrowth(-passiveGrowthAmount * GetGrowthSpeedMultiplier());
            }
        }
        else
        {
            if (Every(ref passiveGrowthTimer, passiveGrowthInterval))
            {
                ModifyGrowth(passiveGrowthAmount * GetGrowthSpeedMultiplier());
            }
        }
    }

    protected virtual float GetGrowthSpeedMultiplier()
    {
        return 1f;
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
        growth200BroadcastTriggered = growth >= 2.0f;
        currentGrowthState = ComputeGrowthState();

        ApplyVisualObjectForState(currentGrowthState, currentGrowthState);
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

        ApplyVisualObjectForState(currentGrowthState, previous);

        OnGrowthStateChanged?.Invoke(previous, computed);
        OnGrowthStateChange(previous, computed);

        Debug.Log($"[{MonsterName}] Growth State : {previous} -> {computed} (growth = {GrowthPercent:F0}%)");

        // Auto research entry berbasis GrowthState (mis. level 9) akan tetap ke-selesaikan di frame ini
        // juga lewat CheckAutoResearch() yang jalan tiap frame di Update() (MonsterBase.cs).
    }

    /// <summary>
    /// Mencari GameObject visual yang aktif berdasarkan GrowthState saat ini.
    /// </summary>
    public GameObject GetActiveVisualObject()
    {
        return GetVisualObjectForState(currentGrowthState);
    }

    //────────────────────────────────────────────────────────
    // GameObject Visual per Growth State
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Cari GameObject visual untuk GrowthState tertentu dari array growthStateObjects.
    /// Override ini kalau child class butuh logika lebih rumit.
    /// Return null kalau tidak ada yang cocok.
    /// </summary>
    protected virtual GameObject GetVisualObjectForState(GrowthState state)
    {
        if (growthStateObjects == null) return null;

        foreach (var entry in growthStateObjects)
        {
            if (entry.state == state && entry.visualObject != null)
                return entry.visualObject;
        }

        return null;
    }

    protected void ApplyVisualObjectForState(GrowthState state, GrowthState previousState)
    {
        if (growthStateObjects != null)
        {
            // Tentukan target visual object terlebih dahulu
            GameObject target = GetVisualObjectForState(state);

            // Jika target visual object ada, kita lakukan toggle active state.
            // Namun jika target null (sengaja dikosongkan), kita biarkan visual yang
            // sedang aktif tetap menyala (sebagai fallback).
            if (target != null)
            {
                foreach (var entry in growthStateObjects)
                {
                    if (entry.visualObject != null)
                    {
                        entry.visualObject.SetActive(entry.visualObject == target);
                    }
                }

                // Update monsterAnimator ke Animator milik objek visual yang baru aktif jika ada.
                // Jika objek visual tidak punya Animator sendiri, coba cari di root.
                var targetAnimator = target.GetComponent<Animator>();
                if (targetAnimator != null)
                {
                    monsterAnimator = targetAnimator;
                }
                else
                {
                    // Fallback ke Animator di root jika visual baru tidak punya Animator sendiri
                    if (monsterAnimator == null || monsterAnimator.gameObject != gameObject)
                    {
                        monsterAnimator = GetComponent<Animator>();
                    }
                }
            }
        }

        // Set parameter setelah memperbarui referensi animator
        if (monsterAnimator != null)
        {
            monsterAnimator.SetInteger("PreviousGrowthState", (int)previousState);
            monsterAnimator.SetInteger("GrowthState", (int)state);
        }

        FitColliderToSprite();
        SyncSortingOrderWithUnit();
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

    public void SetGrowth(float value)
    {
        Growth = value;
    }

    public void ModifyGrowth(float delta)
    {
        if (delta > 0 && Mood == 0 && CurrentGrowthState == GrowthState.Seed)
        {
            return;
        }
        Growth += delta;
    }
}