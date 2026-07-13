using UnityEngine;

/// <summary>
/// Contoh monster.
/// - Jika suhu ruangan berbeda lebih dari allowedDifference,
///   mood turun setiap moodInterval detik.
/// - Saat mood mencapai 0:
///     - Naikkan suhu ruangan
///     - Panggil 1 employee
/// - Efek hanya bisa aktif lagi setelah cooldown.
/// - Growth dipengaruhi lewat makanan (lihat OnMonsterFed), bisa naik/turun,
///   dan otomatis melewati state Benih -> Tumbuh -> Overgrowth -> Mutated
///   sesuai growThreshold / overgrowthThreshold / mutatedThreshold yang
///   diatur di Inspector (lihat MonsterBase).
/// - Research entries (array "Research Entries" di Inspector) contoh konfigurasi :
///     level 1  : id="r1_diet"       , condition=Any          , trigger=Manual
///     level 2  : id="r2_natrium"    , condition=Any          , trigger=Manual
///     level 3  : id="r3_kalium"     , condition=Any          , trigger=Manual
///     level 4  : id="r4_temp"       , condition=AboveGrowing , trigger=Manual
///     level 7  : id="r7_over"       , condition=Overgrowth   , trigger=Manual
///     level 9  : id="r9_mutasi"     , condition=Mutated      , trigger=Auto
///     level 10 : id="r10_mutasi_dp" , condition=Mutated      , trigger=Manual
///     level 11 : id="r_moodzero"    , condition=Custom       , trigger=Auto (lihat CheckCustomResearchCondition)
///     level 11 : id="r_roomtemp"    , condition=Custom       , trigger=Auto , customValue=35 (suhu ambang)
/// </summary>
public class MonsterTest1234 : MonsterBase
{
    [Header("Temperature")]
    [SerializeField] private float allowedDifference = 5f;

    [Tooltip("Interval penurunan mood jika suhu tidak sesuai.")]
    [SerializeField] private float moodInterval = 30f;

    [Header("Mood 0 Effect")]
    [SerializeField] private float roomTempIncrease = 2f;

    [SerializeField] private float triggerCooldown = 10f;

    private float temperatureTimer;
    private float cooldownTimer;

    private bool hasTriggeredAtMoodZero;

    private void Awake()
    {
        base.Awake();   
        MonsterName = "MonsterTest1234";
    }

    protected override void OnGrowingUpdate()
    {
        if (Context == null || Context.CurrentRoom == null)
            return;

        float difference = Mathf.Abs(Context.CurrentRoom.Temperature - SuitableTemperature);

        if (difference <= allowedDifference)
        {
            temperatureTimer = 0f;
        }
        else if (Every(ref temperatureTimer, moodInterval))
        {
            ModifyMood(-1);
        }
    }

    // cooldown effect mood-zero ini biar tetap jalan walau state udah pindah,
    // jadi tetap di OnMonsterUpdate (jalan di semua state)
    protected override void OnMonsterUpdate()
    {
        if (hasTriggeredAtMoodZero && Every(ref cooldownTimer, triggerCooldown))
        {
            hasTriggeredAtMoodZero = false;
            Debug.Log($"[{MonsterName}] Cooldown selesai.");
        }
    }

    protected override void OnMonsterFed(FoodType food)
    {
        if (food == FoodType.Natrium)
        {
            ModifyMood(-1);
            ModifyGrowth(0.4f);

            Debug.Log($"[{MonsterName}] Diberi Natrium : mood turun, growth +40%.");
        }
        if (food == FoodType.Kalium)
        {
            ModifyMood(-1);
            ModifyGrowth(-0.2f);

            Debug.Log($"[{MonsterName}] Diberi Kalium : mood speed turun, growth -20%%.");
        }
    }

    protected override void OnMoodChange(int oldMood, int newMood)
    {
        if (newMood == 0 && !hasTriggeredAtMoodZero)
        {
            TriggerMoodZeroEffect();
        }
    }

    /// <summary>
    /// Contoh reaksi terhadap perubahan GrowthState. Child class lain bisa
    /// override ini untuk ganti sprite, munculkan VFX, ubah perilaku, dll.
    /// </summary>
    protected override void OnGrowthStateChange(GrowthState oldState, GrowthState newState)
    {
        switch (newState)
        {
            case GrowthState.Growing:
                Debug.Log($"[{MonsterName}] Sudah Tumbuh, tidak akan bisa kembali jadi Benih lagi.");
                break;

            case GrowthState.Overgrowth:
                Debug.Log($"[{MonsterName}] Overgrowth! Growth sudah lewat 100%.");
                break;

            case GrowthState.Mutated:
                Debug.Log($"[{MonsterName}] MUTATED! Growth harus diturunkan sampai 100% lagi supaya normal.");
                break;
        }
    }

    /// <summary>
    /// Contoh syarat Custom, dispatch berdasarkan entry.id -- boleh punya banyak Custom entry
    /// sekaligus, tiap id logikanya beda-beda.
    /// </summary>
    protected override bool CheckCustomResearchCondition(ResearchEntry entry)
    {
        switch (entry.id)
        {
            // Auto-unlock begitu efek mood-zero PERNAH ke-trigger, TIDAK peduli GrowthState.
            // Karena trigger-nya Auto, begitu ini true sekali, CompleteResearch() langsung
            // jalan dan PERMANEN -- walaupun hasTriggeredAtMoodZero nanti balik false lagi
            // setelah triggerCooldown, research yang sudah selesai tidak akan ke-lock ulang.
            case "r_moodzero":
                return hasTriggeredAtMoodZero;

            // Auto-unlock begitu suhu ruangan SEKARANG >= entry.customValue (ambang diatur
            // lewat Inspector per-entry, jadi tiap entry bisa punya suhu ambang beda-beda
            // tanpa perlu tambah kode baru).
            case "r_roomtemp":
                return Context != null && Context.CurrentRoom != null
                    && Context.CurrentRoom.Temperature >= entry.customValue;

            default:
                return false;
        }
    }

    private void TriggerMoodZeroEffect()
    {
        hasTriggeredAtMoodZero = true;

        Debug.Log($"[{MonsterName}] Mood mencapai 0.");

        Context.ChangeRoomTemperature(roomTempIncrease);

        Context.SummonRandomEmployee();
    }
}