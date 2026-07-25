using UnityEngine;
using System.Collections.Generic;

public class SunflowerMonster : MonsterBase
{
    [Header("Feeding Growth Speed Boost")]
    [SerializeField] private float growthSpeedMultiplier = 1f;
    [SerializeField] private float growthBoostTimer = 0f;
    private float myPassiveGrowthTimer;
    private bool isMoodZeroEffectActive = false;
    private bool hasMoodReachedZero = false;
    private float potassiumTimer = 0f;

    // Track monsters that had their suitable temperature decreased by us
    private List<MonsterBase> affectedMonsters = new List<MonsterBase>();

    // Track active employees interacting with this monster
    private Dictionary<Employee, float> employeeInteractionTimers = new Dictionary<Employee, float>();

    private float currentTempModifier = 0f;

    protected override void Awake()
    {
        base.Awake();
        MonsterName = "Sunflower";
        // Requirement: suitable temperature is 20, with 3 tolerance
        suitableTemperature = 20f;
        allowedDifference = 3f;
    }

    private void UpdateTemperatureModifier(float newModifier)
    {
        float change = newModifier - currentTempModifier;
        if (Mathf.Approximately(change, 0f)) return;

        if (Context != null && Context.Facility != null)
        {
            Context.ChangeFacilityTemperature(change);
            foreach (var room in Context.Facility.Rooms)
            {
                if (room != null)
                {
                    room.Temperature += change;
                }
            }
            Debug.Log($"[{MonsterName}] Suhu global facility disesuaikan sebesar {(change > 0 ? "+" : "")}{change:F1}°C karena perubahan mood. Total tambahan: {newModifier:F1}°C.");
        }
        currentTempModifier = newModifier;
    }

    protected virtual void Start()
    {
        UpdateTemperatureModifier(5f - Mood);
    }

    private void OnDisable()
    {
        // Revert temporary mood 0 effect
        if (isMoodZeroEffectActive)
        {
            RemoveMoodZeroEffect();
        }

        // Revert global temperature modifier
        UpdateTemperatureModifier(0f);
    }

    protected override void OnMonsterUpdate()
    {
        base.OnMonsterUpdate();

        // 1. Potassium Feeding Timer
        potassiumTimer += Time.deltaTime;
        if (potassiumTimer >= 60f)
        {
            ModifyMood(-1);
            potassiumTimer = 0f;
            Debug.Log($"[{MonsterName}] Tidak diberi makan Kalium dalam 1 menit, Mood berkurang 1.");
        }

        // 2. Growth Boost Timer
        if (growthBoostTimer > 0f)
        {
            growthBoostTimer -= Time.deltaTime;
            if (growthBoostTimer <= 0f)
            {
                growthSpeedMultiplier = 1f;
                Debug.Log($"[{MonsterName}] Growth speed boost expired. Multiplier reset to 1x.");
            }
        }

        // 3. Periodic Employee Interaction Damage
        // Requirement: ketika ada employee yang melakukan feeding, harvest, atau research ke tanaman ini, tiap 5 detik mereka melakukannya, berikan damage sebanyak 1.
        // Special requirement: jika harvest & selain botanist, damage-nya dikali 3.
        if (Facility.Instance != null && Facility.Instance.Employees != null)
        {
            var activeEmployees = new List<Employee>();
            foreach (var emp in Facility.Instance.Employees)
            {
                if (emp == null || emp.CurrentState == EmployeeState.Dead)
                    continue;

                bool isInteracting = (emp.CurrentState == EmployeeState.Feeding || 
                                      emp.CurrentState == EmployeeState.Researching || 
                                      emp.CurrentState == EmployeeState.Harvesting) 
                                      && Vector3.Distance(transform.position, emp.transform.position) < 1.5f;

                if (isInteracting)
                {
                    activeEmployees.Add(emp);
                }
            }

            foreach (var emp in activeEmployees)
            {
                if (!employeeInteractionTimers.ContainsKey(emp))
                {
                    employeeInteractionTimers[emp] = 0f;
                }

                employeeInteractionTimers[emp] += Time.deltaTime;
                if (employeeInteractionTimers[emp] >= 5f)
                {
                    // Calculate damage: if harvesting and not botanist, damage is 3, otherwise 1.
                    int dmg = 1;
                    if (emp.CurrentState == EmployeeState.Harvesting && emp.Division != EmployeeDivision.Botanist)
                    {
                        dmg = 3;
                    }

                    emp.ModifyHp(-dmg);
                    Debug.Log($"[{MonsterName}] Memberikan {dmg} damage ke {emp.EmployeeName} (State: {emp.CurrentState}) karena berinteraksi selama 5 detik.");
                    employeeInteractionTimers[emp] -= 5f;
                }
            }

            // Cleanup removed employees
            var keysToRemove = new List<Employee>();
            foreach (var key in employeeInteractionTimers.Keys)
            {
                if (!activeEmployees.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                employeeInteractionTimers.Remove(key);
            }
        }
    }

    protected override void OnMoodChange(int oldMood, int newMood)
    {
        base.OnMoodChange(oldMood, newMood);

        // Update temperature modifier dynamically
        UpdateTemperatureModifier(5f - newMood);

        // Requirement: ketika mood tanaman ini 0, tingkatkan suhu fasility sebanyak 10, lalu turunkan suitable temperature untuk tanaman yang satu room dengannya sebanyak 10 derajat selama mood nya 0.
        if (newMood == 0 && !isMoodZeroEffectActive)
        {
            ApplyMoodZeroEffect();
        }
        else if (newMood > 0 && isMoodZeroEffectActive)
        {
            RemoveMoodZeroEffect();
        }
    }

    protected override bool CheckCustomResearchCondition(ResearchEntry entry)
    {
        if (entry.id == "mood0")
        {
            return hasMoodReachedZero;
        }
        return base.CheckCustomResearchCondition(entry);
    }

    private void ApplyMoodZeroEffect()
    {
        isMoodZeroEffectActive = true;
        hasMoodReachedZero = true;
        Debug.LogWarning($"[{MonsterName}] Mood mencapai 0! Mengaktifkan efek temperatur.");

        // 1. Increase facility temperature by 10
        if (Context != null && Context.Facility != null)
        {
            Context.ChangeFacilityTemperature(10f);
            foreach (var room in Context.Facility.Rooms)
            {
                if (room != null)
                {
                    room.Temperature += 10f;
                }
            }
        }

        // 2. Lower suitable temperature of other monsters in the same room by 10
        if (Context != null && Context.CurrentRoom is ContainmentRoom containmentRoom)
        {
            foreach (var unit in containmentRoom.ContainmentUnits)
            {
                if (unit != null && unit.HasMonster && unit.Monster != this)
                {
                    unit.Monster.ModifySuitableTemperature(-10f);
                    affectedMonsters.Add(unit.Monster);
                }
            }
        }
    }

    private void RemoveMoodZeroEffect()
    {
        isMoodZeroEffectActive = false;
        Debug.Log($"[{MonsterName}] Mood naik di atas 0. Menghapus efek temperatur.");

        // 1. Revert facility temperature
        if (Context != null && Context.Facility != null)
        {
            Context.ChangeFacilityTemperature(-10f);
            foreach (var room in Context.Facility.Rooms)
            {
                if (room != null)
                {
                    room.Temperature -= 10f;
                }
            }
        }

        // 2. Revert other monsters' suitable temperature
        foreach (var monster in affectedMonsters)
        {
            if (monster != null)
            {
                monster.ModifySuitableTemperature(10f);
            }
        }
        affectedMonsters.Clear();
    }

    protected override void TickPassiveGrowth()
    {
        if (Every(ref myPassiveGrowthTimer, passiveGrowthInterval))
        {
            ModifyGrowth(passiveGrowthAmount * growthSpeedMultiplier);
        }
    }

    protected override void OnMonsterFed(FoodType food)
    {
        base.OnMonsterFed(food);

        // Requirement: memberi makan natrium akan meningkatkan growth speed 1.5X dan mood + 1, fosfor dan magnesium hanya growth speed 1.25X. sedangkan kalium meningkatkan growth sebanyak 2X.
        switch (food)
        {
            case FoodType.Natrium:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = 15f;
                ModifyMood(1);
                Debug.Log($"[{MonsterName}] Diberi makan Natrium: growth speed 1.5x, mood +1.");
                break;

            case FoodType.Fosfor:
            case FoodType.Magnesium:
                growthSpeedMultiplier = 1.25f;
                growthBoostTimer = 15f;
                Debug.Log($"[{MonsterName}] Diberi makan {food}: growth speed 1.25x.");
                break;

            case FoodType.Kalium:
                growthSpeedMultiplier = 2f;
                growthBoostTimer = 15f;
                potassiumTimer = 0f; // Reset potassium feeding starvation timer
                Debug.Log($"[{MonsterName}] Diberi makan Kalium: growth speed 2x.");
                break;
        }
    }

    protected override void OnMonsterHarvested()
    {
        base.OnMonsterHarvested();

        // Requirement: ketika di harvest, jika yang mengharvest adalah selain botanist, tingkatkan damage nya sebanyak 3 kali.
        Employee harvester = currentHarvester;
        if (harvester == null)
        {
            harvester = FindFallbackHarvester();
        }

        if (harvester != null)
        {
            int baseDamage = 10;
            int finalDamage = (harvester.Division != EmployeeDivision.Botanist) ? (baseDamage * 3) : baseDamage;
            harvester.ModifyHp(-finalDamage);
            Debug.Log($"[{MonsterName}] Di-harvest! Harvester {harvester.EmployeeName} menerima {finalDamage} HP damage.");
        }
    }

    private Employee FindFallbackHarvester()
    {
        if (Context == null || Context.CurrentRoom == null || Facility.Instance == null || Facility.Instance.Employees == null)
            return null;

        float minDistance = float.MaxValue;
        Employee closest = null;
        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp == null || emp.CurrentState == EmployeeState.Dead)
                continue;

            if (Context.CurrentRoom.Contains(emp.transform.position))
            {
                float dist = Vector3.Distance(transform.position, emp.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = emp;
                }
            }
        }
        return closest;
    }
}


