using UnityEngine;

/// <summary>
/// Task untuk merawat / mengobati Employee yang sedang sakit (IsSick).
/// Healer akan berjalan mendekati target dan melakukan tindakan pemulihan (timed action).
///
/// PERATURAN MEKANIK:
/// - Jika healer adalah Medic (Division == EmployeeDivision.Medic): status IsSick target akan disembuhkan (CureVirus) dan HP dipulihkan.
/// - Jika healer BUKAN Medic: HP target bertambah, TAPI status IsSick TIDAK akan hilang.
/// </summary>
public class HealSickTask : EmployeeTask
{
    private readonly Employee targetEmployee;
    private Employee healerEmployee;
    private System.Action onComplete;
    private System.Action onFail;
    private bool isWaitingForFinish;
    private const float healDuration = 4f;
    private Coroutine followCoroutine;

    public HealSickTask(Employee targetEmployee)
    {
        this.targetEmployee = targetEmployee;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (targetEmployee == null || targetEmployee.CurrentState == EmployeeState.Dead)
        {
            onFail?.Invoke();
            return;
        }

        this.healerEmployee = employee;
        this.onComplete = onComplete;
        this.onFail = onFail;
        this.isWaitingForFinish = true;

        targetEmployee.OnStateChanged += HandleTargetStateChanged;

        Debug.Log($"[HealSick] {healerEmployee.EmployeeName} berjalan menuju {targetEmployee.EmployeeName} untuk mengobati.");

        followCoroutine = healerEmployee.StartCoroutine(FollowTargetRoutine());
    }

    private System.Collections.IEnumerator FollowTargetRoutine()
    {
        Vector3 lastTargetPos = targetEmployee.transform.position;
        healerEmployee.MoveTo(lastTargetPos);

        while (isWaitingForFinish)
        {
            yield return new WaitForSeconds(0.25f);

            if (targetEmployee == null || targetEmployee.CurrentState == EmployeeState.Dead)
            {
                HandleEarlyDeath();
                yield break;
            }

            float distance = Vector3.Distance(healerEmployee.transform.position, targetEmployee.transform.position);
            if (distance < 1.0f)
            {
                ArrivedAtTarget();
                yield break;
            }

            if (Vector3.Distance(targetEmployee.transform.position, lastTargetPos) > 0.5f)
            {
                lastTargetPos = targetEmployee.transform.position;
                healerEmployee.MoveTo(lastTargetPos);
            }
        }
    }

    private void ArrivedAtTarget()
    {
        if (!isWaitingForFinish) return;

        StopFollowCoroutine();

        if (targetEmployee == null || targetEmployee.CurrentState == EmployeeState.Dead)
        {
            HandleEarlyDeath();
            return;
        }

        Debug.Log($"[HealSick] {healerEmployee.EmployeeName} telah sampai di posisi {targetEmployee.EmployeeName}. Memulai proses pengobatan...");

        // Target tertidur, batalkan gerakan & task mereka saat pengobatan dimulai
        targetEmployee.SetState(EmployeeState.Sleeping);
        targetEmployee.ClearTasksAndInterrupt();

        healerEmployee.SetState(EmployeeState.Healing);
        healerEmployee.StartTimedAction(healDuration, HealingCompleted, HealingInterrupted);
    }

    private void HealingCompleted()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();
        targetEmployee.OnStateChanged -= HandleTargetStateChanged;

        if (targetEmployee != null && targetEmployee.CurrentState != EmployeeState.Dead)
        {
            bool isMedic = healerEmployee.IsImmuneToVirus || healerEmployee.Division == EmployeeDivision.Medic || healerEmployee is EmployeeMedic;

            if (isMedic)
            {
                targetEmployee.CureVirus();
                targetEmployee.SetHp(targetEmployee.MaxHp);
                Debug.Log($"[HealSick] Obati oleh Medic! {targetEmployee.EmployeeName} sembuh total dari virus & HP pulih penuh.");
            }
            else
            {
                // Non-medic: heal HP, tapi IsSick TIDAK hilang!
                targetEmployee.ModifyHp(30);
                Debug.LogWarning($"[HealSick] Obati oleh non-Medic! {targetEmployee.EmployeeName} mendapat +30 HP, TAPI status SICK TIDAK HILANG.");
            }
        }

        if (healerEmployee != null)
        {
            healerEmployee.SetState(EmployeeState.Idle);
            healerEmployee.BackToDivision();
        }

        onComplete?.Invoke();
    }

    private void HealingInterrupted()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();
        targetEmployee.OnStateChanged -= HandleTargetStateChanged;

        // Bangunkan target jika pengobatan terganggu
        if (targetEmployee != null && targetEmployee.CurrentState == EmployeeState.Sleeping)
        {
            targetEmployee.SetState(EmployeeState.Idle);
        }

        var failCallback = onFail;
        onFail = null;
        failCallback?.Invoke();

        if (healerEmployee != null)
        {
            healerEmployee.SetState(EmployeeState.Idle);
            healerEmployee.BackToDivision();
        }
    }

    private void HandleTargetStateChanged(EmployeeState newState)
    {
        if (!isWaitingForFinish) return;

        if (newState == EmployeeState.Dead)
        {
            HandleEarlyDeath();
        }
    }

    private void HandleEarlyDeath()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();
        targetEmployee.OnStateChanged -= HandleTargetStateChanged;

        Debug.LogWarning($"[HealSick] Target meninggal sebelum pengobatan selesai.");

        var failCallback = onFail;
        onFail = null;
        failCallback?.Invoke();

        if (healerEmployee != null)
        {
            healerEmployee.ClearTasksAndInterrupt();
            healerEmployee.SetState(EmployeeState.Idle);
            healerEmployee.BackToDivision();
        }
    }

    private void StopFollowCoroutine()
    {
        if (followCoroutine != null && healerEmployee != null)
        {
            healerEmployee.StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }

    public void Cancel()
    {
        if (!isWaitingForFinish) return;

        HealingInterrupted();
    }
}
