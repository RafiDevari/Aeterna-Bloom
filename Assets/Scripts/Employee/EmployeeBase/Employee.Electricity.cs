using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus sistem perbaikan listrik (Fix Electricity).
/// </summary>
public partial class Employee
{
    /// <summary>
    /// Menghitung durasi perbaikan listrik FINAL (detik) untuk target ElectricityRoom,
    /// dari sudut pandang employee ini.
    /// </summary>
    public virtual float CalculateFixElectricityDuration(ElectricityRoom targetRoom)
    {
        float baseDuration = targetRoom.FixDuration;

        // Engineer adalah spesialis perbaikan listrik. Divisi lain kena penalti.
        return division == EmployeeDivision.Engineer
            ? baseDuration
            : baseDuration * offDivisionMultiplier;
    }

    /// <summary>
    /// Perintah lengkap: jalan ke ElectricityRoom target, lalu perbaiki listrik begitu sampai.
    /// </summary>
    public void GoFixElectricity(ElectricityRoom targetRoom)
    {
        if (targetRoom == null || !Facility.Instance.IsBlackout)
        {
            Debug.Log($"[Employee] {employeeName} batal memperbaiki listrik: target room null atau listrik stabil.");
            return;
        }

        ClearTasksAndInterrupt();

        EnqueueTask(new MoveToTask(
            () => targetRoom.transform.position,
            () => targetRoom != null && Facility.Instance.IsBlackout));

        EnqueueTask(new FixElectricityTask(targetRoom));

        BackToDivision();

        Debug.Log($"[Employee] {employeeName} menerima job: perbaiki listrik di {targetRoom.RoomName}.");
    }
}
