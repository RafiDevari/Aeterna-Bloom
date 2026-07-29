using UnityEngine;
using System.Collections;

public class SterilizeTask : EmployeeTask
{
    private readonly Room targetRoom;
    private Employee employee;
    private Coroutine runningRoutine;
    private bool isWaitingToFinish;

    public SterilizeTask(Room room)
    {
        this.targetRoom = room;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (targetRoom == null)
        {
            Debug.LogWarning($"[{employee.EmployeeName}] Batal sterilisasi: Ruangan null.");
            onFail?.Invoke();
            return;
        }

        if (targetRoom.IsSterilizing)
        {
            Debug.Log($"[{employee.EmployeeName}] Ruangan sudah disterilisasi oleh pihak lain.");
            onComplete?.Invoke();
            return;
        }

        this.employee = employee;
        employee.SetState(EmployeeState.Sterilizing);
        targetRoom.IsSterilizing = true;
        isWaitingToFinish = true;

        float duration = employee.CalculateSterilizeDuration(targetRoom);
        employee.SetActionDuration(duration);

        runningRoutine = employee.StartCoroutine(SterilizeRoutine(duration, onComplete));
    }

    private IEnumerator SterilizeRoutine(float duration, System.Action onComplete)
    {
        yield return new WaitForSeconds(duration);

        if (isWaitingToFinish)
        {
            isWaitingToFinish = false;
            
            // Selesai sterilisasi: Matikan mode sterilisasi, buka kunci ruangan, dan hilangkan racun
            if (targetRoom != null)
            {
                targetRoom.IsSterilizing = false;
                targetRoom.SetLocked(false);
                targetRoom.IsPoisoned = false;
            }

            employee.SetState(EmployeeState.Idle);
            Debug.Log($"[{employee.EmployeeName}] Berhasil menyelesaikan sterilisasi di ruangan {targetRoom.RoomName}!");
            
            onComplete?.Invoke();
        }
    }

    public void Cancel()
    {
        if (!isWaitingToFinish) return;
        isWaitingToFinish = false;

        if (runningRoutine != null && employee != null)
        {
            employee.StopCoroutine(runningRoutine);
        }

        if (targetRoom != null)
        {
            targetRoom.IsSterilizing = false;
            // Jika dibatalkan tengah jalan, kita tidak buka kuncinya (biar tetap lockdown)
        }

        employee?.SetState(EmployeeState.Idle);
    }
}
