using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dandelectric - A custom electricity-based plant/monster.
/// 
/// Mechanics:
/// - If Mood < 3: Increases room electricity cost by 5. Otherwise 0.
/// - Has an 'energy' variable (max 70).
/// - Every 'energyTickInterval' seconds (default 5s) if Mood < 3: increases 'energy' by 5 (capped at 70).
/// - If Mood reaches 0: triggers ElectricShock.
///   - ElectricShock chooses a random room, damages all employees in that room by 'energy' value, and resets Mood to 4.
/// - Feeding Reactions:
///   - Natrium: Mood -1, growth speed increases by 4x for 15s, energy -20.
///   - Kalium: growth speed increases by 3x for 15s.
///   - Fosfor: growth speed increases by 1.5x for 15s.
///   - Magnesium: growth speed increases by 5x for 15s, Mood +1, energy +30.
/// - Harvest Reactions:
///   - Mood -1, plant energy -20, harvester takes 25 damage.
///   - Facility energy reward remains base-equivalent.
/// </summary>
public class Dandelectric : MonsterBase
{
    [Header("Dandelectric Stats")]
    [SerializeField] private float energy = 0f;
    [SerializeField] private float energyTickInterval = 5f;

    [Header("Growth Speed Settings")]
    [SerializeField] private float growthSpeedMultiplier = 1f;
    [SerializeField] private float growthBoostTimer = 0f;

    private float energyTickTimer;
    private float myPassiveGrowthTimer;

    public float Energy => energy;

    protected override void Awake()
    {
        base.Awake();
        MonsterName = "Dandelectric";
    }

    protected override void OnMonsterUpdate()
    {
        base.OnMonsterUpdate();

        // 1. Tick energy if mood < 3
        if (Mood < 3)
        {
            if (Every(ref energyTickTimer, energyTickInterval))
            {
                energy = Mathf.Min(70f, energy + 5f);
                Debug.Log($"[{MonsterName}] Energy charged: {energy}/70 (Mood: {Mood})");
            }
        }
        else
        {
            energyTickTimer = 0f;
        }

        // 2. Tick growth boost timer
        if (growthBoostTimer > 0f)
        {
            growthBoostTimer -= Time.deltaTime;
            if (growthBoostTimer <= 0f)
            {
                growthSpeedMultiplier = 1f;
                Debug.Log($"[{MonsterName}] Growth speed boost expired. Multiplier reset to 1x.");
            }
        }

        // 3. Keep room electricity cost synchronized
        if (Context != null && Context.CurrentRoom != null)
        {
            float targetCost = Mood < 3 ? 5f : 0f;
            Context.SetMonsterElectricityCost(targetCost);
        }
    }

    private void OnDisable()
    {
        // Reset electricity cost contribution on disable/destruction
        if (Context != null)
        {
            Context.SetMonsterElectricityCost(0f);
        }
    }

    protected override void OnMoodChange(int oldMood, int newMood)
    {
        base.OnMoodChange(oldMood, newMood);

        if (newMood == 0)
        {
            ElectricShock();
        }
    }

    /// <summary>
    /// Performs an electric shock on a random room, dealing damage to all employees inside it.
    /// After shock, resets mood to 4.
    /// </summary>
    public void ElectricShock()
    {
        if (Facility.Instance == null || Facility.Instance.Rooms == null || Facility.Instance.Rooms.Count == 0)
            return;

        // Choose a random room
        int randomIndex = Random.Range(0, Facility.Instance.Rooms.Count);
        Room targetRoom = Facility.Instance.Rooms[randomIndex];
        int damage = Mathf.RoundToInt(energy);

        Debug.LogWarning($"[{MonsterName}] ELECTRIC SHOCK! Target Room: {targetRoom.RoomName}, Damage: {damage}");

        // Deal damage to all alive employees inside the chosen room
        if (Facility.Instance.Employees != null)
        {
            var employees = new List<Employee>(Facility.Instance.Employees);
            foreach (var emp in employees)
            {
                if (emp == null || emp.CurrentState == EmployeeState.Dead)
                    continue;

                if (targetRoom.Contains(emp.transform.position))
                {
                    emp.ModifyHp(-damage);
                    Debug.Log($"[{MonsterName}] Shock hit {emp.EmployeeName} in {targetRoom.RoomName} for {damage} damage.");
                }
            }
        }

        // Reset mood to 4
        SetMood(4);
    }

    /// <summary>
    /// Overriding passive growth tick to integrate growthSpeedMultiplier.
    /// </summary>
    protected override void TickPassiveGrowth()
    {
        if (Every(ref myPassiveGrowthTimer, passiveGrowthInterval))
        {
            ModifyGrowth(passiveGrowthAmount * growthSpeedMultiplier);
        }
    }

    /// <summary>
    /// Custom feeding buffs.
    /// </summary>
    protected override void OnMonsterFed(FoodType food)
    {
        base.OnMonsterFed(food);

        switch (food)
        {
            case FoodType.Natrium:
                ModifyMood(-1);
                growthSpeedMultiplier = 4f;
                growthBoostTimer = 15f;
                energy = Mathf.Max(0f, energy - 20f);
                Debug.Log($"[{MonsterName}] Fed Natrium: mood -1, growth speed 4x for 15s, energy -20. Energy: {energy}");
                break;

            case FoodType.Kalium:
                growthSpeedMultiplier = 3f;
                growthBoostTimer = 15f;
                Debug.Log($"[{MonsterName}] Fed Kalium: growth speed 3x for 15s.");
                break;

            case FoodType.Fosfor:
                growthSpeedMultiplier = 1.5f;
                growthBoostTimer = 15f;
                Debug.Log($"[{MonsterName}] Fed Fosfor: growth speed 1.5x for 15s.");
                break;

            case FoodType.Magnesium:
                ModifyMood(1);
                growthSpeedMultiplier = 5f;
                growthBoostTimer = 15f;
                energy = Mathf.Min(70f, energy + 30f);
                Debug.Log($"[{MonsterName}] Fed Magnesium: mood +1, growth speed 5x for 15s, energy +30. Energy: {energy}");
                break;
        }
    }

    /// <summary>
    /// Custom harvest logic.
    /// </summary>
    protected override void OnMonsterHarvested()
    {
        base.OnMonsterHarvested();

        // 1. Mood berkurang 1
        ModifyMood(-1);

        // 2. Energy pada tanamannya berkurang 20
        energy = Mathf.Max(0f, energy - 20f);

        // 3. Berikan damage ke employee yang mengharvestnya sebanyak 25
        Employee harvester = currentHarvester;
        if (harvester == null)
        {
            harvester = FindFallbackHarvester();
        }

        if (harvester != null)
        {
            harvester.ModifyHp(-25);
            Debug.Log($"[{MonsterName}] Harvested! Harvester {harvester.EmployeeName} damaged by 25 HP. Remaining energy: {energy}");
        }
        else
        {
            Debug.LogWarning($"[{MonsterName}] Harvested, but harvester employee could not be found to damage.");
        }
    }

    /// <summary>
    /// Helper fallback to locate the closest employee in the room to damage if currentHarvester wasn't set.
    /// </summary>
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
