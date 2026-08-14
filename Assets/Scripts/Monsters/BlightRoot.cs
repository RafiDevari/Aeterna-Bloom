using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BlightRoot - Tanaman baru dengan mekanik virus dan reaksi nutrisi khusus.
/// 
/// Mekanik:
/// - Suitable Temp: 25 derajat Celsius.
/// - Mood 0: Spawn Virus di posisi acak, lalu mengembalikan Mood ke 5.
/// - Nutrisi sama berturut-turut: Mood berkurang 2.
/// - Panic State (Mood == 1): Setiap kali diberi nutrisi apapun, Mood berkurang 1 (menjadi 0).
/// - Nutrisi Kalium: Growth speed 1.5x selama 20s untuk diri sendiri & 1 tanaman lain di room yang sama, Mood -2.
/// - Nutrisi Magnesium: Mood -2. Jika dipanen dalam 30s setelah diberi Fosfor, hasil energi harvest meningkat 1.5x.
/// - Nutrisi Fosfor: Mood +2, growth berkurang 10%.
/// - Nutrisi Natrium: Growth speed 1.5x selama 10s.
/// - Mutated State: Menyebarkan virus di tempat acak setiap 1 menit (60s).
/// </summary>
public class BlightRoot : MonsterBase
{
    [Header("BlightRoot Settings")]
    [SerializeField] private float growthSpeedMultiplier = 1f;
    [SerializeField] private float growthBoostTimer = 0f;

    private FoodType lastFedFood = FoodType.None;
    private float lastFosforTime = -999f;
    private bool isMagnesiumBuffActive = false;
    private float mutatedVirusTimer = 0f;

    public FoodType LastFedFood => lastFedFood;
    public bool IsMagnesiumBuffActive => isMagnesiumBuffActive;

    protected override void Awake()
    {
        base.Awake();
        MonsterName = "BlightRoot";
        suitableTemperature = 25f;
        allowedDifference = 3f;
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
                Debug.Log($"[{MonsterName}] Growth boost expired. Reset to 1x.");
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
            Debug.LogWarning($"[{MonsterName}] Mood mencapai 0! Spawning Virus dan mengembalikan Mood ke 5.");
            SpawnVirusAtRandomLocation();
            SetMood(5);
        }
    }

    protected override void OnMonsterFed(FoodType food)
    {
        base.OnMonsterFed(food);

        // 1. Diberi nutrisi yang sama seperti sebelumnya -> Mood berkurang 2
        if (food != FoodType.None && food == lastFedFood)
        {
            ModifyMood(-2);
            Debug.Log($"[{MonsterName}] Diberikan nutrisi ({food}) berturut-turut: Mood -2.");
        }

        // 2. Panic State (Mood == 1): diberi nutrisi apapun -> Mood berkurang 1 (menjadi 0)
        if (Mood == 1)
        {
            ModifyMood(-1);
            Debug.Log($"[{MonsterName}] Diberi nutrisi saat Panic State (Mood 1): Mood -1 (menjadi 0).");
        }

        lastFedFood = food;

        // 3. Spesifik reaksi per jenis nutrisi
        switch (food)
        {
            case FoodType.Kalium:
                ModifyMood(-2);
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = 20f;
                ApplyKaliumRoomBoost();
                Debug.Log($"[{MonsterName}] Diberi Kalium: Mood -2, growth speed 1.5x selama 20s (self + 1 room plant).");
                break;

            case FoodType.Magnesium:
                ModifyMood(-2);
                isMagnesiumBuffActive = true;
                Debug.Log($"[{MonsterName}] Diberi Magnesium: Mood -2, kondisi bonus harvest energi diaktifkan.");
                break;

            case FoodType.Fosfor:
                ModifyMood(2);
                ModifyGrowth(-Growth * 0.10f);
                lastFosforTime = Time.time;
                Debug.Log($"[{MonsterName}] Diberi Fosfor: Mood +2, growth berkurang 10%. Waktu pemberian Fosfor di-catat.");
                break;

            case FoodType.Natrium:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = 10f;
                Debug.Log($"[{MonsterName}] Diberi Natrium: growth speed 1.5x selama 10s.");
                break;
        }
    }

    private void ApplyKaliumRoomBoost()
    {
        if (Context != null && Context.CurrentRoom is ContainmentRoom containmentRoom)
        {
            List<MonsterBase> otherMonsters = new List<MonsterBase>();
            foreach (var unit in containmentRoom.ContainmentUnits)
            {
                if (unit != null && unit.HasMonster && unit.Monster != this)
                {
                    otherMonsters.Add(unit.Monster);
                }
            }

            if (otherMonsters.Count > 0)
            {
                int randomIndex = Random.Range(0, otherMonsters.Count);
                MonsterBase targetPlant = otherMonsters[randomIndex];
                targetPlant.ApplyGrowthBoost(1.5f, 20f);
                Debug.Log($"[{MonsterName}] Kalium memberikan growth boost 1.5x selama 20s ke {targetPlant.MonsterName}.");
            }
        }
    }

    protected override float CalculateHarvestEnergyMultiplier()
    {
        float multiplier = base.CalculateHarvestEnergyMultiplier();

        if (isMagnesiumBuffActive && (Time.time - lastFosforTime <= 30f))
        {
            multiplier *= 1.5f;
            isMagnesiumBuffActive = false; // reset buff setelah dipakai
            Debug.Log($"[{MonsterName}] Syarat harvest Magnesium + Fosfor terpenuhi! Multiplier energi harvest: 1.5x.");
        }

        return multiplier;
    }

    protected override void OnMutatedUpdate()
    {
        base.OnMutatedUpdate();

        mutatedVirusTimer += Time.deltaTime;
        if (mutatedVirusTimer >= 60f)
        {
            mutatedVirusTimer = 0f;
            Debug.LogWarning($"[{MonsterName}] Mutated state: Spawns Virus di posisi acak setiap 1 menit.");
            SpawnVirusAtRandomLocation();
        }
    }

    public void SpawnVirusAtRandomLocation()
    {
        Vector3 spawnPos = transform.position;

        if (Context != null && Context.CurrentRoom != null)
        {
            Bounds b = Context.CurrentRoom.RoomBounds;
            float randomX = Random.Range(b.min.x, b.max.x);
            float randomY = Random.Range(b.min.y, b.max.y);
            spawnPos = new Vector3(randomX, randomY, transform.position.z);
        }
        else
        {
            float offsetX = Random.Range(-2f, 2f);
            float offsetY = Random.Range(-1f, 1f);
            spawnPos = transform.position + new Vector3(offsetX, offsetY, 0f);
        }

        Virus.SpawnAt(spawnPos);
    }
}
