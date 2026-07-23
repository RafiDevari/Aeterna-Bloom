using UnityEngine;

/// <summary>
/// Bagian MonsterBase yang mengurus sistem Harvest : durasi proses harvest (mirror
/// Feeding/Research), syarat kapan monster bisa di-harvest, reset growth, dan reward energy.
///
/// Alur : growth monster lewat 100% (Overgrowth) -> tombol Harvest muncul di UI -> employee
/// dikirim buat harvest (ada durasi, sama seperti feed/research) -> begitu selesai :
///   1. Energy dihitung dari KELEBIHAN growth di atas growThreshold :
///        energy = energyGain * (growth saat dipotong - growThreshold) * CalculateHarvestEnergyMultiplier()
///      Base class cuma menghitung angkanya -- child class yang menentukan apa yang
///      terjadi dengan energy itu lewat OnEnergyHarvested(), dan multiplier-nya sendiri
///      lewat CalculateHarvestEnergyMultiplier() (lihat contoh di MonsterTest1234).
///   2. Growth direset PERSIS ke growThreshold (balik ke awal siklus Growing, BUKAN ke Seed).
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
    protected Employee currentHarvester;

    //────────────────────────────────────────────────────────
    // Harvest Reward
    //────────────────────────────────────────────────────────

    [Header("Harvest Reward")]
    [Tooltip("Base energy yang didapat per 1.0 kelebihan growth di atas growThreshold saat harvest. " +
             "Final energy = energyGain * (growth saat harvest - growThreshold) * CalculateHarvestEnergyMultiplier().")]
    [SerializeField] protected float energyGain = 1f;

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

    /// <summary>Base energy per 1.0 kelebihan growth di atas growThreshold. Lihat CalculateHarvestEnergyMultiplier untuk multiplier tambahan.</summary>
    public float EnergyGain
    {
        get => energyGain;
        protected set => energyGain = value;
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

        // Growth SAAT DIPOTONG (sebelum direset) -- dasar perhitungan energy.
        // ASUMSI: MonsterBase.Growth.cs expose current growth lewat property/field bernama "Growth".
        // Ganti nama ini kalau ternyata beda di file kalian.
        float growthAtHarvest = Growth;
        float excessGrowth = Mathf.Max(0f, growthAtHarvest - growThreshold);
        float multiplier = CalculateHarvestEnergyMultiplier();
        float energyAmount = energyGain * excessGrowth * multiplier;

        OnMonsterHarvested();            // efek harvest lain (drop item, dsb) -- isi di child
        OnEnergyHarvested(energyAmount); // energy hasil harvest -- child WAJIB isi ini biar energy-nya benar2 diberikan

        ResetGrowthForHarvest();         // kembalikan growth persis ke growThreshold

        OnHarvestFinished?.Invoke();
        currentHarvester = null;

        Debug.Log($"[{MonsterName}] Harvest selesai. Growth {growthAtHarvest:0.##} -> {growThreshold:0.##}, " +
                  $"energy : {energyGain} x {excessGrowth:0.##} x {multiplier:0.##} = {energyAmount:0.##}");
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Override di child class buat isi APA YANG TERJADI saat harvest selesai
    /// (mis. spawn item, VFX, dsb) -- di luar reward energy (lihat OnEnergyHarvested).
    /// Sengaja dikosongkan dulu di base class.
    /// </summary>
    protected virtual void OnMonsterHarvested() { }

    /// <summary>
    /// Multiplier tambahan buat hasil energy harvest, dihitung SEPENUHNYA di child class
    /// monster (bisa berdasarkan jenis monster, mood, growth stage, RNG, dsb -- lihat
    /// contoh di MonsterTest1234). Default 1 (tidak ada modifikasi) di base class.
    /// </summary>
    protected virtual float CalculateHarvestEnergyMultiplier() => 1f;

    /// <summary>
    /// Dipanggil begitu energy hasil harvest sudah dihitung
    /// (energyGain * kelebihan growth * CalculateHarvestEnergyMultiplier()).
    /// Base class TIDAK melakukan apapun dengan angka ini -- child class WAJIB override
    /// method ini untuk benar-benar memberikan energy-nya ke sistem lain (mis. tambah ke
    /// resource pool Facility, currency player, dsb).
    /// </summary>
    protected virtual void OnEnergyHarvested(float energyAmount)
    {
        Context.ChangeEnergy(+energyAmount);
        Debug.Log($"[{MonsterName}] Harvest memberi energy : {energyAmount:0.##}");
    }

    /// <summary>
    /// Reset growth monster PERSIS ke growThreshold (awal siklus Growing), dipanggil
    /// otomatis begitu harvest selesai. Pakai ModifyGrowth() yang sudah ada supaya
    /// GrowthState & hook OnGrowthStateChange ikut ke-update otomatis lewat jalur yang sama
    /// seperti perubahan growth lainnya (mis. dari makan).
    /// </summary>
    protected virtual void ResetGrowthForHarvest()
    {
        ModifyGrowth(growThreshold - Growth);
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
    public bool TryHarvest(float? durationOverride = null, Employee harvester = null)
    {
        if (!CanBeHarvested)
        {
            Debug.Log($"[{MonsterName}] Harvest gagal : growth belum Overgrowth, atau sedang harvest lain.");
            return false;
        }

        currentHarvester = harvester;
        harvestDurationTimer = Mathf.Max(0f, durationOverride ?? harvestDuration);

        Debug.Log($"[{MonsterName}] Mulai harvest, durasi : {harvestDurationTimer}s");

        // Durasi 0 = harvest instan, langsung selesai di frame yang sama.
        if (harvestDurationTimer <= 0f)
            CompleteHarvest();

        return true;
    }
}