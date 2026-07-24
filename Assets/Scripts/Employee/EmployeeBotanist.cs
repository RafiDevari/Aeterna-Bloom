using UnityEngine;

/// <summary>
/// Employee spesialis perawatan monster (feed & harvest). Efisien untuk itu,
/// tapi kalau terpaksa melakukan research (bukan bidangnya), durasinya jadi
/// berkali-kali lipat lebih lama.
/// </summary>
public class EmployeeBotanist : Employee
{
    [Header("Botanist Penalty")]
    [Tooltip("Multiplier durasi saat botanist terpaksa research (bukan bidangnya).")]
    [SerializeField] private float offFieldMultiplier = 6f;

    protected override float CalculateResearchDuration(MonsterBase target)
    {
        return base.CalculateResearchDuration(target) * offFieldMultiplier;
    }
}