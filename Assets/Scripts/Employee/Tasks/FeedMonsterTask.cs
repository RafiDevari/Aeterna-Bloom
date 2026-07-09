//==============================================================
// Task: beri makan monster target, dengan validasi ulang
// (monster/unit bisa berubah selama employee dalam perjalanan).
//==============================================================
public class FeedMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;

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
        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForFeedToFinish)
            return;

        isWaitingForFeedToFinish = false;
        targetMonster.OnFeedFinished -= HandleFeedFinished;
    }
}