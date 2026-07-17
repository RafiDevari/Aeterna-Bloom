using UnityEngine;

//==============================================================
// Task: Memperbaiki listrik di ElectricityRoom (dengan durasi).
//==============================================================
public class FixElectricityTask : EmployeeTask
{
    private readonly ElectricityRoom electricityRoom;

    private Employee employee;
    private Coroutine runningRoutine;
    private bool isWaitingForFixToFinish;

    public FixElectricityTask(ElectricityRoom electricityRoom)
    {
        this.electricityRoom = electricityRoom;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (electricityRoom == null || !Facility.Instance.IsBlackout)
        {
            Debug.Log($"[Employee] {employee.EmployeeName} gagal memperbaiki listrik: room null atau listrik stabil.");
            onFail?.Invoke();
            return;
        }

        if (electricityRoom.IsFixing)
        {
            Debug.Log($"[Employee] {employee.EmployeeName} batal memperbaiki listrik: sudah ada employee lain yang memperbaikinya.");
            onFail?.Invoke();
            return;
        }

        this.employee = employee;
        electricityRoom.IsFixing = true;

        float duration = employee.CalculateFixElectricityDuration(electricityRoom);

        // Simpan durasi final ke progress bar
        employee.SetActionDuration(duration);

        employee.SetState(EmployeeState.FixingElectricity);
        isWaitingForFixToFinish = true;

        runningRoutine = employee.StartCoroutine(FixRoutine(duration, onComplete));
    }

    private System.Collections.IEnumerator FixRoutine(float duration, System.Action onComplete)
    {
        yield return new WaitForSeconds(duration);

        if (isWaitingForFixToFinish)
        {
            isWaitingForFixToFinish = false;
            electricityRoom.IsFixing = false;

            // Memulihkan listrik global di Facility
            Facility.Instance.ResolveBlackout();

            employee.SetState(EmployeeState.Idle);

            Debug.Log($"[Employee] {employee.EmployeeName} selesai memperbaiki listrik di {electricityRoom.RoomName} (durasi : {duration}s).");

            onComplete?.Invoke();
        }
    }

    public void Cancel()
    {
        if (!isWaitingForFixToFinish)
            return;

        isWaitingForFixToFinish = false;

        if (runningRoutine != null && employee != null)
        {
            employee.StopCoroutine(runningRoutine);
        }

        if (electricityRoom != null)
        {
            electricityRoom.IsFixing = false;
        }

        employee?.SetState(EmployeeState.Idle);
    }
}
