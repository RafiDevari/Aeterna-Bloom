using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus sistem sterilisasi ruangan (Sterilize Room).
/// </summary>
public partial class Employee
{
    /// <summary>
    /// Menghitung durasi sterilisasi ruangan FINAL (detik) untuk target Room,
    /// dari sudut pandang employee ini.
    /// </summary>
    public virtual float CalculateSterilizeDuration(Room targetRoom)
    {
        float baseDuration = 30f; // Durasi sterilize dasar 30 detik

        // Security adalah spesialis sterilisasi. Divisi lain kena penalti.
        float duration = division == EmployeeDivision.Security
            ? baseDuration
            : baseDuration * offDivisionMultiplier;

        return duration * GetDivisionAssignmentWorkMultiplier();
    }

    /// <summary>
    /// Perintah lengkap: jalan ke Room target, lalu sterilkan ruangan begitu sampai.
    /// </summary>
    public void GoSterilize(Room targetRoom)
    {
        if (targetRoom == null)
        {
            Debug.LogWarning($"[Employee] {employeeName} batal mensterilisasi: target room null.");
            return;
        }

        ClearTasksAndInterrupt();

        // 1. Bergerak ke target ruangan
        EnqueueTask(new MoveToTask(
            () => targetRoom.transform.position,
            () => targetRoom != null));

        // 2. Mulai proses sterilisasi setelah sampai
        EnqueueTask(new SterilizeTask(targetRoom));

        // 3. Kembali ke divisi setelah selesai
        BackToDivision();

        Debug.Log($"[Employee] {employeeName} menerima job: sterilisasi ruangan {targetRoom.RoomName}.");
    }
}
