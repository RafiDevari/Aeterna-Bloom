//==============================================================
// Task: beri makan monster target, dengan validasi ulang
// (monster/unit bisa berubah selama employee dalam perjalanan).
//==============================================================
public class FeedMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;

    private Employee employee;
    private System.Action onComplete;
    private bool isWaitingForFeedToFinish;

    public FeedMonsterTask(ContainmentUnit unit, MonsterBase targetMonster)
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

        if (!employee.FeedMonster(targetMonster))
        {
            onFail?.Invoke();
            return;
        }

        this.employee = employee;
        this.onComplete = onComplete;
        isWaitingForFeedToFinish = true;

        employee.SetState(EmployeeState.Feeding);
        targetMonster.OnFeedFinished += HandleFeedFinished;
    }

    private void HandleFeedFinished()
    {
        if (!isWaitingForFeedToFinish)
            return;

        isWaitingForFeedToFinish = false;
        targetMonster.OnFeedFinished -= HandleFeedFinished;

        // PENTING: balikin state ke Idle di sini. Tanpa ini, currentState employee
        // nyangkut selamanya di Feeding (progress bar & sistem lain yang gantung
        // pada CurrentState jadi ikut nyangkut).
        employee.SetState(EmployeeState.Idle);

        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForFeedToFinish)
            return;

        isWaitingForFeedToFinish = false;
        targetMonster.OnFeedFinished -= HandleFeedFinished;

        // Job diinterupsi di tengah jalan (mis. player klik pindah manual) --
        // tetap balikin state, jangan biarkan nyangkut di Feeding.
        employee?.SetState(EmployeeState.Idle);
    }
}