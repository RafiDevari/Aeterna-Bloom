using UnityEngine;

/// <summary>
/// Bagian MonsterBase yang mengurus sistem Harvest : durasi proses harvest (mirror
/// Feeding/Research), syarat kapan monster bisa di-harvest, dan efek harvest itu sendiri.
///
/// Alur : growth monster lewat 100% (Overgrowth) -> tombol Harvest muncul di UI -> employee
/// dikirim buat harvest (ada durasi, sama seperti feed/research) -> begitu selesai, growth
/// direset ke MINIMAL growth untuk state Growing (monster "dipangkas" balik ke awal siklus
/// Growing, BUKAN balik ke Seed).
///
/// ASUMSI PENTING yang perlu dicek/disesuaikan terhadap MonsterBase.Growth.cs kalian yang
/// sebenarnya (saya belum pernah lihat isi file itu) :
///   - CurrentGrowthState == GrowthState.Overgrowth dipakai sebagai representasi "growth > 100%".
///     Kalau ternyata growth kalian itu angka float terpisah (mis. growthPercent) yang BISA
///     Overgrowth tapi belum tentu selalu >100%, sesuaikan IsOvergrown di bawah.
///   - ResetGrowthForHarvest() SENGAJA saya buat virtual + isinya cuma warning log. Isi
///     method ini (override di sini lewat edit langsung, atau override di child class)
///     supaya beneran mengeset growth value/timer kalian balik ke ambang minimal Growing
///     + set CurrentGrowthState = GrowthState.Growing.
/// </summary>
public partial class MonsterBase
{
    //────────────────────────────────────────────────────────
    // Harvest Timing
    //────────────────────────────────────────────────────────

    [Header("Harvest Timing")]
    [Tooltip("Durasi 'harvest' BAWAAN monster ini (detik), dipakai sebagai fallback kalau TryHarvest() " +
             "dipanggil tanpa durasi eksplisit dari luar. Beda-beda tiap jenis monster -- sama seperti feedDuration/researchDuration.")]
    [SerializeField] protected float harvestDuration = 1f;

    protected float harvestDurationTimer = 0f;

    //────────────────────────────────────────────────────────
    // Events
    //────────────────────────────────────────────────────────

    /// <summary>Invoked begitu proses harvest BENERAN selesai (durasi habis) -- mirror OnFeedFinished/OnResearchFinished.</summary>
    public System.Action OnHarvestFinished;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    public float HarvestDuration
    {
        get => harvestDuration;
        protected set => harvestDuration = value;
    }

    /// <summary>True selagi monster sedang dalam proses di-harvest.</summary>
    public bool IsHarvesting => harvestDurationTimer > 0f;

    /// <summary>
    /// True kalau growth monster sudah lewat 100% (representasi : CurrentGrowthState == Overgrowth).
    /// Dipakai UI (mis. ContainmentPopup) buat nentuin kapan tombol Harvest ditampilkan.
    /// </summary>
    public bool IsOvergrown => CurrentGrowthState == GrowthState.Overgrowth;

    /// <summary>Boleh mulai harvest baru hanya kalau growth-nya Overgrowth DAN tidak sedang harvest lain.</summary>
    public bool CanBeHarvested => IsOvergrown && !IsHarvesting;

    //────────────────────────────────────────────────────────
    // Tick
    //────────────────────────────────────────────────────────

    private void TickHarvestDuration()
    {
        if (harvestDurationTimer <= 0f)
            return;

        harvestDurationTimer -= Time.deltaTime;

        if (harvestDurationTimer <= 0f)
            CompleteHarvest();
    }

    private void CompleteHarvest()
    {
        harvestDurationTimer = 0f;

        OnMonsterHarvested(); // efek harvest -- isi di child class, lihat hook di bawah

        ResetGrowthForHarvest(); // kembalikan growth ke minimal Growing -- lihat catatan asumsi di atas

        OnHarvestFinished?.Invoke();

        Debug.Log($"[{MonsterName}] Harvest selesai.");
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Override di child class buat isi APA YANG TERJADI saat harvest selesai
    /// (mis. kasih resource ke player, spawn item, tambah stok, dsb). Sengaja
    /// dikosongkan dulu di base class sesuai permintaan -- "nanti saja buat apa
    /// yang terjadi saat di-harvest".
    /// </summary>
    protected virtual void OnMonsterHarvested() { }

    /// <summary>
    /// Reset growth monster ke MINIMAL growth untuk state Growing, dipanggil otomatis
    /// begitu harvest selesai. PLACEHOLDER -- ganti isinya (atau override di child class)
    /// supaya beneran mengeset growth value/timer kalian + CurrentGrowthState = GrowthState.Growing,
    /// sesuai cara growth kalian disimpan di MonsterBase.Growth.cs.
    /// </summary>
    protected virtual void ResetGrowthForHarvest()
    {
        Debug.LogWarning($"[{MonsterName}] ResetGrowthForHarvest() belum diimplementasi -- " +
                          "isi method ini di MonsterBase.Growth.cs supaya growth beneran " +
                          "kembali ke minimal Growing setelah harvest.");
    }

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Mulai proses harvest monster ini.
    /// </summary>
    /// <param name="durationOverride">
    /// Durasi harvest FINAL (detik) yang sudah dihitung pemanggil (biasanya
    /// Employee.CalculateHarvestDuration). Kalau null, fallback ke HarvestDuration bawaan,
    /// supaya TryHarvest() tetap aman dipanggil langsung tanpa lewat Employee.
    /// </param>
    /// <returns>
    /// True kalau proses harvest berhasil DIMULAI sekarang. Bukan berarti sudah selesai --
    /// selesainya dilaporkan lewat OnHarvestFinished, kecuali durationOverride &lt;= 0 (instan).
    /// </returns>
    public bool TryHarvest(float? durationOverride = null)
    {
        if (!CanBeHarvested)
        {
            Debug.Log($"[{MonsterName}] Harvest gagal : growth belum Overgrowth, atau sedang harvest lain.");
            return false;
        }

        harvestDurationTimer = Mathf.Max(0f, durationOverride ?? harvestDuration);

        Debug.Log($"[{MonsterName}] Mulai harvest, durasi : {harvestDurationTimer}s");

        // Durasi 0 = harvest instan, langsung selesai di frame yang sama.
        if (harvestDurationTimer <= 0f)
            CompleteHarvest();

        return true;
    }
}