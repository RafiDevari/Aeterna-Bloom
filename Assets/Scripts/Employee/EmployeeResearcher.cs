using UnityEngine;

/// <summary>
/// Employee spesialis Research. Efisien untuk research, tapi kalau terpaksa
/// memberi makan atau harvest (bukan bidangnya), durasinya jadi berkali-kali
/// lipat lebih lama.
/// </summary>
public class EmployeeResearcher : Employee
{
    [Header("Researcher Penalty")]
    [Tooltip("Multiplier durasi saat researcher terpaksa feed / harvest (bukan bidangnya).")]
    [SerializeField] private float offFieldMultiplier = 5f;

    protected override float CalculateFeedDuration(MonsterBase target)
    {
        return base.CalculateFeedDuration(target) * offFieldMultiplier;
    }

    protected override float CalculateHarvestDuration(MonsterBase target)
    {
        return base.CalculateHarvestDuration(target) * offFieldMultiplier;
    }
}