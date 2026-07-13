using UnityEngine;

/// <summary>
/// Bagian MonsterBase yang mengurus sistem makan : durasi makan, cooldown, dan hook reaksi
/// setelah makan selesai (OnMonsterFed / OnFedDuringCooldown).
/// </summary>
public partial class MonsterBase
{
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
    // Events
    //────────────────────────────────────────────────────────

    public System.Action<FoodType> OnFed;
    public System.Action OnFeedFinished;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

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

    /// <summary>True selagi monster sedang dalam proses makan (dari Feed() sampai durasi makan habis).</summary>
    public bool IsFeeding => feedDurationTimer > 0f;

    /// <summary>Boleh diberi makan hanya kalau tidak sedang makan DAN cooldown sudah habis.</summary>
    public bool CanBeFed => !IsFeeding && feedCooldownTimer <= 0f;

    //────────────────────────────────────────────────────────
    // Tick
    //────────────────────────────────────────────────────────

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

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    protected virtual void OnMonsterFed(FoodType food) { }
    protected virtual void OnFedDuringCooldown(FoodType food) { }

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

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