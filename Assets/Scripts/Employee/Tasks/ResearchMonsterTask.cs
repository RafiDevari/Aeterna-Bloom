//==============================================================
// Task: mulai research pada monster target, dengan validasi ulang
// (monster/unit bisa berubah selama employee dalam perjalanan).
// Punya durasi (mirror FeedMonsterTask) -- task ini menunggu
// MonsterBase.OnResearchFinished sebelum memanggil onComplete.
//==============================================================
public class ResearchMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;
    private readonly string researchId;

    private Employee employee;
    private System.Action onComplete;
    private bool isWaitingForResearchToFinish;

    /// <param name="researchId">
    /// Null/kosong -> employee akan research entry APA SAJA yang available begitu sampai
    /// (TryResearchNext). Diisi -> coba entry spesifik itu (TryResearch(id)).
    /// </param>
    public ResearchMonsterTask(ContainmentUnit unit, MonsterBase targetMonster, string researchId = null)
    {
        this.unit = unit;
        this.targetMonster = targetMonster;
        this.researchId = researchId;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (unit == null || !unit.HasMonster || unit.Monster != targetMonster)
        {
            onFail?.Invoke();
            return;
        }

        if (!employee.TryResearch(targetMonster, researchId))
        {
            onFail?.Invoke();
            return;
        }

        this.employee = employee;
        this.onComplete = onComplete;
        isWaitingForResearchToFinish = true;

        employee.SetState(EmployeeState.Researching);
        targetMonster.OnResearchFinished += HandleResearchFinished;
    }

    private void HandleResearchFinished()
    {
        if (!isWaitingForResearchToFinish)
            return;

        isWaitingForResearchToFinish = false;
        targetMonster.OnResearchFinished -= HandleResearchFinished;

        // PENTING: balikin state ke Idle di sini. Tanpa ini, currentState employee
        // nyangkut selamanya di Researching (progress bar & sistem lain yang gantung
        // pada CurrentState jadi ikut nyangkut).
        employee.SetState(EmployeeState.Idle);

        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForResearchToFinish)
            return;

        isWaitingForResearchToFinish = false;
        targetMonster.OnResearchFinished -= HandleResearchFinished;

        // Job diinterupsi di tengah jalan (mis. player klik pindah manual) --
        // tetap balikin state, jangan biarkan nyangkut di Researching.
        employee?.SetState(EmployeeState.Idle);
    }
}