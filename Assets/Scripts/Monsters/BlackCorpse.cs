using UnityEngine;
using System.Collections;

/// <summary>
/// Black Corpse - Tanaman baru dengan mekanik Slime, Mayat Employee, dan reaksi Nutrisi khusus.
/// 
/// MEKANIK:
/// 1. Mood Drop ke 0:
///    - Spawn Slime di lokasi tanaman ini berada (Slime.SpawnAt(transform.position)).
///    - Kembalikan Mood ke 5.
/// 2. Deteksi Mayat Employee:
///    - Ketika ada employee yang mati, jika dalam 60 detik mayatnya masih ada (tidak dimakan Slime / belum dihapus), Mood drop 1.
/// 3. Mutated State:
///    - Ketika tanaman ini mutated, spawn 1 slime secara acak di fasilitas menggunakan Slime.Spawn() (saat pertama mutasi dan setiap 30 detik).
/// 4. Feeder Mood Check:
///    - Jika employee yang memberi makan sedang dalam mood < 3, bunuh employee tersebut (feeder.Die()) dan spawn Slime di lokasi tanaman.
/// 5. Reaksi Nutrisi:
///    - Natrium   : growth speed + 1.5 kali.
///    - Kalium    : growth speed 1.5 kali.
///    - Fosfor    : growth speed 1.5 kali.
///    - Magnesium : growth speed 1.5 kali.
/// </summary>
public class BlackCorpse : MonsterBase
{
    [Header("Black Corpse Settings")]
    [SerializeField] private float growthSpeedMultiplier = 1f;
    [SerializeField] private float growthBoostTimer = 0f;
    [SerializeField] private float defaultBoostDuration = 15f;

    private float mutatedSlimeTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        MonsterName = "Black Corpse";
    }

    private void OnEnable()
    {
        Employee.OnEmployeeDied += HandleEmployeeDied;
    }

    private void OnDisable()
    {
        Employee.OnEmployeeDied -= HandleEmployeeDied;
    }

    protected override void OnMonsterUpdate()
    {
        base.OnMonsterUpdate();

        if (growthBoostTimer > 0f)
        {
            growthBoostTimer -= Time.deltaTime;
            if (growthBoostTimer <= 0f)
            {
                growthSpeedMultiplier = 1f;
                Debug.Log($"[{MonsterName}] Growth boost expired. Growth speed multiplier reset to 1x.");
            }
        }
    }

    protected override float GetGrowthSpeedMultiplier()
    {
        float ownBoost = growthBoostTimer > 0f ? growthSpeedMultiplier : 1f;
        return ownBoost * base.GetGrowthSpeedMultiplier();
    }

    protected override void OnMoodChange(int oldMood, int newMood)
    {
        base.OnMoodChange(oldMood, newMood);

        if (newMood == 0)
        {
            Debug.LogWarning($"[{MonsterName}] Mood mencapai 0! Spawning Slime di lokasi ini dan mengembalikan Mood ke 5.");
            Slime.SpawnAt(transform.position);
            SetMood(5);
        }
    }

    private void HandleEmployeeDied(Employee deadEmp)
    {
        if (deadEmp != null)
        {
            StartCoroutine(TrackCorpseRoutine(deadEmp));
        }
    }

    private IEnumerator TrackCorpseRoutine(Employee deadEmp)
    {
        yield return new WaitForSeconds(60f);

        // Jika setelah 60 detik mayatnya masih ada (tidak dimakan Slime / belum destroyed & status masih Dead)
        if (deadEmp != null && deadEmp.gameObject != null && deadEmp.CurrentState == EmployeeState.Dead)
        {
            ModifyMood(-1);
            Debug.LogWarning($"[{MonsterName}] Mayat {deadEmp.EmployeeName} masih ada setelah 60 detik! Mood berkurang 1. Mood saat ini: {Mood}");
        }
    }

    protected override void OnMutatedUpdate()
    {
        base.OnMutatedUpdate();

        mutatedSlimeTimer += Time.deltaTime;
        if (mutatedSlimeTimer >= 30f)
        {
            mutatedSlimeTimer = 0f;
            Debug.LogWarning($"[{MonsterName}] Status Mutated: Spawns 1 Slime di lokasi random via Slime.Spawn() (tiap 30s).");
            Slime.Spawn();
        }
    }

    protected override void OnGrowthStateChange(GrowthState oldState, GrowthState newState)
    {
        base.OnGrowthStateChange(oldState, newState);

        if (newState == GrowthState.Mutated && oldState != GrowthState.Mutated)
        {
            mutatedSlimeTimer = 0f;
            Debug.LogWarning($"[{MonsterName}] Tanaman mutated! Spawn 1 slime di lokasi random via Slime.Spawn().");
            Slime.Spawn();
        }
    }

    protected override void OnFeedStarted(FoodType food, Employee feeder)
    {
        base.OnFeedStarted(food, feeder);

        if (feeder != null && feeder.Mood < 3)
        {
            Debug.LogWarning($"[{MonsterName}] Employee {feeder.EmployeeName} memberi makan dengan Mood < 3 ({feeder.Mood})! Employee dibunuh dan Slime di-spawn.");
            feeder.Die();
            Slime.SpawnAt(transform.position);
        }
    }

    protected override void OnMonsterFed(FoodType food, Employee feeder)
    {
        base.OnMonsterFed(food, feeder);

        switch (food)
        {
            case FoodType.Natrium:
                float currentBoost = growthBoostTimer > 0f ? growthSpeedMultiplier : 1f;
                growthSpeedMultiplier = currentBoost + 1.5f;
                growthBoostTimer = defaultBoostDuration;
                Debug.Log($"[{MonsterName}] Diberi Natrium: growth speed + 1.5x (menjadi {growthSpeedMultiplier}x) selama {defaultBoostDuration}s.");
                break;

            case FoodType.Kalium:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = defaultBoostDuration;
                Debug.Log($"[{MonsterName}] Diberi Kalium: growth speed 1.5x selama {defaultBoostDuration}s.");
                break;

            case FoodType.Fosfor:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = defaultBoostDuration;
                Debug.Log($"[{MonsterName}] Diberi Fosfor: growth speed 1.5x selama {defaultBoostDuration}s.");
                break;

            case FoodType.Magnesium:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = defaultBoostDuration;
                Debug.Log($"[{MonsterName}] Diberi Magnesium: growth speed 1.5x selama {defaultBoostDuration}s.");
                break;
        }
    }
}
