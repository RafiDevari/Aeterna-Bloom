using UnityEngine;

//==============================================================
// Task: berjalan ke sebuah posisi.
// Posisi & validitas dievaluasi LAZY (saat task benar-benar mulai),
// supaya kalau target sudah tidak valid (misal room dihancurkan),
// task gagal dengan bersih alih-alih exception / posisi salah.
//==============================================================
public class MoveToTask : EmployeeTask
{
    private readonly System.Func<Vector3> getDestination;
    private readonly System.Func<bool> isValid;

    public MoveToTask(System.Func<Vector3> getDestination, System.Func<bool> isValid = null)
    {
        this.getDestination = getDestination;
        this.isValid = isValid;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (isValid != null && !isValid())
        {
            onFail?.Invoke();
            return;
        }

        employee.MoveTo(getDestination(), onComplete, onFail);
    }

    public void Cancel()
    {
        // Tidak ada resource untuk dibersihkan; employee yang berhenti
        // ditangani lewat Employee.ClearTasksAndInterrupt().
    }
}