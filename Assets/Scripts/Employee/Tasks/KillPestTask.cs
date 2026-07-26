using UnityEngine;

/// <summary>
/// Task untuk menugaskan Employee membunuh Pest (misal Tikus).
/// Employee akan berjalan mendekati pest hingga posisi dekat, lalu membunuhnya.
///
/// PERATURAN MEKANIK:
/// - Jika yang disuruh adalah Security (Division == EmployeeDivision.Security): Pembasmian sukses tanpa penalti.
/// - Jika yang disuruh BUKAN Security: Employee tersebut mendapat penalti Mood -1 dan HP -20!
/// </summary>
public class KillPestTask : EmployeeTask
{
    private readonly Pest targetPest;
    private Employee assignedEmployee;
    private System.Action onComplete;
    private System.Action onFail;
    private bool isWaitingForFinish;
    private Coroutine followCoroutine;

    public KillPestTask(Pest targetPest)
    {
        this.targetPest = targetPest;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (targetPest == null || targetPest.IsDead || employee == null || employee.CurrentState == EmployeeState.Dead)
        {
            onFail?.Invoke();
            return;
        }

        this.assignedEmployee = employee;
        this.onComplete = onComplete;
        this.onFail = onFail;
        this.isWaitingForFinish = true;

        Debug.Log($"[KillPest] {assignedEmployee.EmployeeName} berjalan menuju pest untuk membunuhnya.");

        followCoroutine = assignedEmployee.StartCoroutine(FollowTargetRoutine());
    }

    private System.Collections.IEnumerator FollowTargetRoutine()
    {
        Vector3 lastTargetPos = targetPest.transform.position;
        assignedEmployee.MoveTo(lastTargetPos);

        while (isWaitingForFinish)
        {
            yield return new WaitForSeconds(0.25f);

            if (targetPest == null || targetPest.IsDead || assignedEmployee == null || assignedEmployee.CurrentState == EmployeeState.Dead)
            {
                HandleEarlyTargetDeath();
                yield break;
            }

            float distance = Vector3.Distance(assignedEmployee.transform.position, targetPest.transform.position);
            if (distance < 1.0f)
            {
                ArrivedAtTarget();
                yield break;
            }

            if (Vector3.Distance(targetPest.transform.position, lastTargetPos) > 0.5f)
            {
                lastTargetPos = targetPest.transform.position;
                assignedEmployee.MoveTo(lastTargetPos);
            }
        }
    }

    private void ArrivedAtTarget()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();

        if (targetPest != null && !targetPest.IsDead)
        {
            // Bunuh pest
            targetPest.Kill();

            // Cek apakah pembasmi adalah Security
            bool isSecurity = assignedEmployee.Division == EmployeeDivision.Security || assignedEmployee is EmployeeSecurity;

            if (!isSecurity)
            {
                // Penalti non-Security: Mood -1, HP -20
                assignedEmployee.ModifyMood(-1);
                assignedEmployee.ModifyHp(-20);
                Debug.LogWarning($"[KillPest] {assignedEmployee.EmployeeName} (bukan Security) membunuh pest! Penalti diterapkan: Mood -1, HP -20.");
            }
            else
            {
                Debug.Log($"[KillPest] Security {assignedEmployee.EmployeeName} berhasil membunuh pest tanpa penalti.");
            }
        }

        if (assignedEmployee != null)
        {
            assignedEmployee.SetState(EmployeeState.Idle);
            assignedEmployee.BackToDivision();
        }

        onComplete?.Invoke();
    }

    private void HandleEarlyTargetDeath()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();

        Debug.Log($"[KillPest] Target pest sudah mati sebelum employee sampai.");

        var failCallback = onFail;
        onFail = null;
        failCallback?.Invoke();

        if (assignedEmployee != null)
        {
            assignedEmployee.SetState(EmployeeState.Idle);
            assignedEmployee.BackToDivision();
        }
    }

    private void StopFollowCoroutine()
    {
        if (followCoroutine != null && assignedEmployee != null)
        {
            assignedEmployee.StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }

    public void Cancel()
    {
        if (!isWaitingForFinish) return;

        StopFollowCoroutine();
        isWaitingForFinish = false;

        var failCallback = onFail;
        onFail = null;
        failCallback?.Invoke();

        if (assignedEmployee != null)
        {
            assignedEmployee.SetState(EmployeeState.Idle);
            assignedEmployee.BackToDivision();
        }
    }
}
