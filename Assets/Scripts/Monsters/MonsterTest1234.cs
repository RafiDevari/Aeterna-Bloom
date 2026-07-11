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

    private void TriggerMoodZeroEffect()
    {
        hasTriggeredAtMoodZero = true;

        Debug.Log($"[{MonsterName}] Mood mencapai 0.");

        Context.ChangeRoomTemperature(roomTempIncrease);

        Context.SummonRandomEmployee();
    }
}