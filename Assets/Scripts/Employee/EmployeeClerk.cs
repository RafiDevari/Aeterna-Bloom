using UnityEngine;

/// <summary>
/// Employee spesialis Clerk.
/// Memiliki movement speed 6f dan multiplier durasi 2x untuk semua tugas.
/// </summary>
public class EmployeeClerk : Employee
{
    [Header("Clerk Penalty")]
    [Tooltip("Multiplier durasi saat clerk melakukan tugas (bukan bidangnya).")]
    [SerializeField] private float offFieldMultiplier = 2f;

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 6f;
    }

    protected override float CalculateFeedDuration(MonsterBase target)
    {
        return target.FeedDuration * offFieldMultiplier;
    }

    protected override float CalculateHarvestDuration(MonsterBase target)
    {
        return target.HarvestDuration * offFieldMultiplier;
    }

    protected override float CalculateResearchDuration(MonsterBase target)
    {
        return target.ResearchDuration * offFieldMultiplier;
    }

    protected internal override float CalculateTakeStockDuration(StockRoom stockRoom, FoodType food, int amount)
    {
        return stockRoom.TakeStockDuration * offFieldMultiplier;
    }

    public override float CalculateFixElectricityDuration(ElectricityRoom targetRoom)
    {
        return targetRoom.FixDuration * offFieldMultiplier;
    }
}
