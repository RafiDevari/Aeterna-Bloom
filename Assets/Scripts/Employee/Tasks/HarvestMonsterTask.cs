//==============================================================
// Task: mulai harvest pada monster target, dengan validasi ulang
// (monster/unit bisa berubah selama employee dalam perjalanan).
// Mirror persis FeedMonsterTask/ResearchMonsterTask -- task ini menunggu
// MonsterBase.OnHarvestFinished sebelum memanggil onComplete.
//==============================================================
public class HarvestMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;

    private Employee employee;
    private System.Action onComplete;
    private bool isWaitingForHarvestToFinish;

    public HarvestMonsterTask(ContainmentUnit unit, MonsterBase targetMonster)
    {
        this.unit = unit;
        this.targetMonster = targetMonster;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (unit == null || !unit.HasMonster || unit.Monster != targetMonster)
        {
            onFail?.Invoke();
            return;
        }

        if (!employee.TryHarvest(targetMonster))
        {
            onFail?.Invoke();
            return;
        }

        this.employee = employee;
        this.onComplete = onComplete;
        isWaitingForHarvestToFinish = true;

        employee.SetState(EmployeeState.Harvesting);
        targetMonster.OnHarvestFinished += HandleHarvestFinished;
    }

    private void HandleHarvestFinished()
    {
        if (!isWaitingForHarvestToFinish)
            return;

        isWaitingForHarvestToFinish = false;
        targetMonster.OnHarvestFinished -= HandleHarvestFinished;

        // PENTING: balikin state ke Idle di sini. Tanpa ini, currentState employee
        // nyangkut selamanya di Harvesting (progress bar & sistem lain yang gantung
        // pada CurrentState jadi ikut nyangkut) -- sama bug yang kemarin ada di
        // FeedMonsterTask/ResearchMonsterTask.
        employee.SetState(EmployeeState.Idle);

        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForHarvestToFinish)
            return;

        isWaitingForHarvestToFinish = false;
        targetMonster.OnHarvestFinished -= HandleHarvestFinished;

        // Job diinterupsi di tengah jalan (mis. player klik pindah manual) --
        // tetap balikin state, jangan biarkan nyangkut di Harvesting.
        employee?.SetState(EmployeeState.Idle);
    }
}