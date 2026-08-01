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

    [Tooltip("Animator untuk mengontrol animasi monster. Auto-cari kalau kosong.")]
    [SerializeField] protected Animator monsterAnimator;

    [Tooltip("Otomatis sesuaikan ukuran BoxCollider2D monster ini mengikuti bounds sprite, " +
             "tiap kali sprite berubah (mis. pas GrowthState pindah dan sprite ganti ukuran).")]
    [SerializeField] private bool autoFitCollider = true;

    [Tooltip("BoxCollider2D milik monster ini sendiri (bukan milik ContainmentUnit). Opsional — " +
             "auto-cari kalau kosong. Kalau prefab tidak punya Collider2D, fitur ini otomatis di-skip.")]
    [SerializeField] private BoxCollider2D monsterCollider;

    public SpriteRenderer MonsterRenderer
    {
        get
        {
            if (monsterRenderer != null) return monsterRenderer;
            GameObject activeVisual = GetActiveVisualObject();
            if (activeVisual != null)
            {
                var sr = activeVisual.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) return sr;
            }
            return null;
        }
    }

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

        // Coba hitung berdasarkan composite bounds dari objek visual yang aktif
        GameObject activeVisual = GetActiveVisualObject();
        if (activeVisual != null)
        {
            var renderers = activeVisual.GetComponentsInChildren<SpriteRenderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds combinedBounds = new Bounds();
                bool first = true;
                foreach (var sr in renderers)
                {
                    if (sr == null || sr.sprite == null) continue;
                    Bounds localBounds = GetLocalBoundsForRenderer(sr);
                    if (first)
                    {
                        combinedBounds = localBounds;
                        first = false;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(localBounds);
                    }
                }
                if (!first)
                {
                    monsterCollider.size = combinedBounds.size;
                    monsterCollider.offset = combinedBounds.center;
                    return;
                }
            }
        }

        // Fallback ke monsterRenderer bawaan jika objek visual aktif tidak ada/tidak punya SpriteRenderer
        if (monsterRenderer != null && monsterRenderer.sprite != null)
        {
            Bounds spriteBounds = monsterRenderer.sprite.bounds;
            monsterCollider.size = spriteBounds.size;
            monsterCollider.offset = spriteBounds.center;
        }
    }

    private Bounds GetLocalBoundsForRenderer(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return new Bounds();
        Vector3 worldMin = sr.bounds.min;
        Vector3 worldMax = sr.bounds.max;
        Vector3 localMin = transform.InverseTransformPoint(worldMin);
        Vector3 localMax = transform.InverseTransformPoint(worldMax);
        Bounds b = new Bounds();
        b.SetMinMax(localMin, localMax);
        return b;
    }

    /// <summary>
    /// Menyelaraskan sorting layer dan sorting order dari semua SpriteRenderer aktif pada visual tanaman
    /// dengan ContainmentUnit tempat ia berada, dengan tetap mempertahankan offset rendering relatifnya.
    /// </summary>
    public void SyncSortingOrderWithUnit()
    {
        if (myUnit == null) return;
        SpriteRenderer unitRenderer = myUnit.GetComponent<SpriteRenderer>();
        if (unitRenderer == null) return;

        int targetLayerID = unitRenderer.sortingLayerID;
        int targetOrder = unitRenderer.sortingOrder + 1;

        // Sinkronisasi untuk renderer root/legacy
        if (monsterRenderer != null)
        {
            monsterRenderer.sortingLayerID = targetLayerID;
            monsterRenderer.sortingOrder = targetOrder;
        }

        // Sinkronisasi untuk semua part dalam visual aktif
        GameObject activeVisual = GetActiveVisualObject();
        if (activeVisual != null)
        {
            var renderers = activeVisual.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                // Cari sorting order terkecil (paling belakang) untuk dipakai sebagai jangkar
                int minOrder = int.MaxValue;
                foreach (var sr in renderers)
                {
                    if (sr != null && sr.sortingOrder < minOrder)
                    {
                        minOrder = sr.sortingOrder;
                    }
                }

                // Hitung offset dan geser semua renderer agar mempertahankan hierarchy layer-nya
                int offset = targetOrder - minOrder;
                foreach (var sr in renderers)
                {
                    if (sr != null)
                    {
                        sr.sortingLayerID = targetLayerID;
                        sr.sortingOrder += offset;
                    }
                }
            }
        }
    }

    //────────────────────────────────────────────────────────
    // Mood
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    public int MaxMood => maxMood;
    public int MinMood => minMood;

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

            // Trigger small visual indicator (e.g. broken heart) on containment unit when mood drops
            if (mood < previous && myUnit != null)
            {
                myUnit.TriggerSmallEffect(AeternaBloom.Effects.Room.RoomEffectPaths.MoodDown);
            }

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

    [Header("Temperature Settings (Base)")]
    [Tooltip("Tolerance difference for suitable temperature.")]
    [SerializeField] protected float allowedDifference = 5f;

    [Tooltip("Interval (seconds) for mood to decay if temperature is out of range.")]
    [SerializeField] protected float moodInterval = 30f;

    protected float temperatureTimer;

    public float SuitableTemperature
    {
        get => suitableTemperature;
        protected set => suitableTemperature = value;
    }

    public void ModifySuitableTemperature(float delta)
    {
        suitableTemperature += delta;
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
        SyncSortingOrderWithUnit();
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

        if (monsterAnimator == null)
            monsterAnimator = GetComponentInChildren<Animator>();

        ApplySprite();
        SyncInitialGrowthState();  // MonsterBase.Growth.cs
        CheckAutoResearch();       // MonsterBase.Research.cs -- jaga-jaga growth awal sudah penuhi syarat Auto

        if (myUnit == null)
        {
            myUnit = GetComponentInParent<ContainmentUnit>();
            if (myUnit != null)
            {
                context = new MonsterContext(myUnit);
            }
        }
    }

    protected virtual void Update()
    {
        TickFeedDuration();     // MonsterBase.Feeding.cs
        TickFeedCooldown();     // MonsterBase.Feeding.cs
        TickPassiveGrowth();    // MonsterBase.Growth.cs
        TickResearchDuration(); // MonsterBase.Research.cs -- jalanin timer research Manual yang sedang berlangsung
        TickHarvestDuration();  // MonsterBase.Harvest.cs -- jalanin timer harvest yang sedang berlangsung
        CheckAutoResearch();    // MonsterBase.Research.cs -- dicek tiap frame, kondisi Custom bisa berubah kapan saja

        // Handle Temperature-based Mood Decay for all monsters in all states
        HandleTemperatureMoodDecay();

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

    /// <summary>
    /// Dipanggil ketika Employee yang terhipnosis tiba di ContainmentUnit tempat monster ini berada.
    /// Subclass monster dapat me-override method ini untuk menentukan efeknya (misal: membunuh employee, mengurangi HP/Mood, dll).
    /// </summary>
    public virtual void OnHypnotizedEmployeeArrived(Employee employee)
    {
        Debug.Log($"[{MonsterName}] Employee {employee?.EmployeeName} tiba di containment unit.");
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

    protected virtual void HandleTemperatureMoodDecay()
    {
        if (Context != null && Context.CurrentRoom != null)
        {
            float difference = Mathf.Abs(Context.CurrentRoom.Temperature - suitableTemperature);
            if (difference <= allowedDifference)
            {
                temperatureTimer = 0f;
            }
            else if (Every(ref temperatureTimer, moodInterval))
            {
                ModifyMood(-1);
                Debug.Log($"[{MonsterName}] Suhu tidak sesuai ({Context.CurrentRoom.Temperature:F1}°C, target: {suitableTemperature:F1}°C ± {allowedDifference}°C). Mood berkurang.");
            }
        }
    }
}