using UnityEngine;

/// <summary>
/// Task untuk merawat (take care) employee yang terhipnotis.
/// Healer akan berjalan mendekat, menidurkan target, melakukan proses healing,
/// dan mengembalikan target ke divisinya jika sukses.
/// </summary>
public class TakeCareTask : EmployeeTask
{
    private readonly Employee targetEmployee;
    private Employee healerEmployee;
    private System.Action onComplete;
    private System.Action onFail;
    private bool isWaitingForFinish;
    private const float healDuration = 5f;
    private Coroutine followCoroutine;

    public TakeCareTask(Employee targetEmployee)
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

        // Berlangganan status target untuk mendeteksi kematian sebelum healer sampai
        targetEmployee.OnStateChanged += HandleTargetStateChanged;

        Debug.Log($"[TakeCare] {healerEmployee.EmployeeName} mulai berjalan menuju {targetEmployee.EmployeeName} untuk merawat.");

        // Mulai coroutine untuk mengikuti target secara dinamis agar mereka bertemu dari jarak dekat
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

            // Jika sudah sangat dekat (jarak < 1.0f), langsung temui / obati
            float distance = Vector3.Distance(healerEmployee.transform.position, targetEmployee.transform.position);
            if (distance < 1.0f)
            {
                ArrivedAtTarget();
                yield break;
            }

            // Jika target berpindah tempat lebih dari 0.5 unit dari target posisi sebelumnya, update path
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

        // Validasi ulang target
        if (targetEmployee == null || targetEmployee.CurrentState == EmployeeState.Dead)
        {
            HandleEarlyDeath();
            return;
        }

        Debug.Log($"[TakeCare] {healerEmployee.EmployeeName} telah sampai. {targetEmployee.EmployeeName} tertidur.");

        // Target tertidur, batalkan gerakan & task mereka
        targetEmployee.SetState(EmployeeState.Sleeping);
        targetEmployee.ClearTasksAndInterrupt();

        // Healer mulai proses healing
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
            // Jika healer bukan divisi medic, HP berkurang 50% max HP
            if (healerEmployee.Division != EmployeeDivision.Medic)
            {
                int damage = Mathf.RoundToInt(targetEmployee.MaxHp * 0.5f);
                targetEmployee.ModifyHp(-damage);
                Debug.LogWarning($"[TakeCare] Dirawat oleh non-Medic! {targetEmployee.EmployeeName} terluka: -{damage} HP.");
            }
            else
            {
                // Jika healer adalah Medic, HP pulih penuh, mood kembali normal (3)
                targetEmployee.SetHp(targetEmployee.MaxHp);
                targetEmployee.SetMood(3);
                Debug.Log($"[TakeCare] Dirawat oleh Medic! HP & Mood {targetEmployee.EmployeeName} dipulihkan.");
            }

            // Target kembali ke divisi (jika masih hidup setelah penalti)
            if (targetEmployee.CurrentState != EmployeeState.Dead)
            {
                targetEmployee.SetState(EmployeeState.Idle);
                targetEmployee.BackToDivision();
            }
        }

        // Healer kembali ke divisi
        healerEmployee.SetState(EmployeeState.Idle);
        healerEmployee.BackToDivision();

        onComplete?.Invoke();
    }

    private void HealingInterrupted()
    {
        if (!isWaitingForFinish) return;
        isWaitingForFinish = false;

        StopFollowCoroutine();
        targetEmployee.OnStateChanged -= HandleTargetStateChanged;

        // Bangunkan target jika masih tertidur
        if (targetEmployee != null && targetEmployee.CurrentState == EmployeeState.Sleeping)
        {
            targetEmployee.SetState(EmployeeState.Hypnotized);
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

        Debug.LogWarning($"[TakeCare] Target mati sebelum healer sampai! Membatalkan tugas {healerEmployee.EmployeeName} dan mengurangi mood sebanyak 2.");

        var failCallback = onFail;
        onFail = null;
        failCallback?.Invoke();

        if (healerEmployee != null)
        {
            healerEmployee.ClearTasksAndInterrupt();
            healerEmployee.ModifyMood(-2);
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
