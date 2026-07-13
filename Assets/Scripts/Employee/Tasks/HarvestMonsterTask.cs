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

        this.onComplete = onComplete;
        isWaitingForHarvestToFinish = true;

        // NOTE: sama seperti ResearchMonsterTask, pastikan EmployeeState punya value
        // Harvesting. Kalau belum ada, tambahkan dulu di enum EmployeeState, atau ganti
        // baris ini ke state lain yang paling sesuai buat sementara.
        employee.SetState(EmployeeState.Harvesting);
        targetMonster.OnHarvestFinished += HandleHarvestFinished;
    }

    private void HandleHarvestFinished()
    {
        if (!isWaitingForHarvestToFinish)
            return;

        isWaitingForHarvestToFinish = false;
        targetMonster.OnHarvestFinished -= HandleHarvestFinished;
        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForHarvestToFinish)
            return;

        isWaitingForHarvestToFinish = false;
        targetMonster.OnHarvestFinished -= HandleHarvestFinished;
    }
}