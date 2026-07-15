using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus Division : keahlian (EmployeeDivision), room divisi tempat
/// ditugaskan (DivisionRoom), dan perintah balik ke divisi (BackToDivision).
/// </summary>
public partial class Employee
{
    [Header("Division")]
    [Tooltip("Keahlian employee ini. Menentukan tugas mana yang dikerjakan dengan durasi normal, " +
             "dan mana yang kena penalti (lihat offDivisionMultiplier).")]
    [SerializeField] private EmployeeDivision division;

    [Tooltip("Multiplier durasi kalau employee ini mengerjakan tugas DI LUAR keahliannya " +
             "(mis. Researcher disuruh Feed/Harvest, atau Botanist disuruh Research).")]
    [SerializeField] private float offDivisionMultiplier = 5f;

    // Divisi tempat bekerja
    private DivisionRoom assignedDivision;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    public DivisionRoom AssignedDivision => assignedDivision;

    /// <summary>Keahlian employee ini (Researcher/Botanist) -- lihat offDivisionMultiplier soal penalti di luar keahlian.</summary>
    public EmployeeDivision Division => division;

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Set keahlian (EmployeeDivision) employee ini secara langsung. Dipakai oleh DivisionRoom
    /// saat men-spawn employee (lihat DivisionRoom.SpawnEmployees), supaya keahliannya otomatis
    /// konsisten dengan tipe divisi yang men-spawn-nya, tanpa perlu diatur manual di tiap prefab.
    /// Bisa juga dipanggil manual kalau nanti butuh promosi/pindah divisi di runtime.
    /// </summary>
    public void SetDivision(EmployeeDivision newDivision)
    {
        division = newDivision;
    }

    public void AssignDivision(DivisionRoom division)
    {
        if (assignedDivision == division)
            return;

        assignedDivision?.UnassignEmployee(this);

        assignedDivision = division;

        assignedDivision?.AssignEmployee(this);

        Debug.Log($"[Employee] {employeeName} ditugaskan ke division : {assignedDivision?.RoomName}");
    }

    /// <summary>
    /// Suruh employee ini balik ke room divisi tempat dia ditugaskan (AssignedDivision),
    /// dengan menambahkan satu MoveToTask ke akhir antrean task. Dipanggil otomatis sebagai
    /// tahap akhir job Feed/Research/Harvest (lihat Employee.Feeding.cs/Research.cs/Harvest.cs)
    /// supaya employee tidak diam begitu saja di tempat begitu tugasnya selesai.
    ///
    /// No-op (tidak menambah task apapun) kalau employee belum di-assign ke divisi manapun.
    /// </summary>
    public void BackToDivision()
    {
        if (assignedDivision == null)
        {
            Debug.Log($"[Employee] {employeeName} belum punya AssignedDivision, tetap di tempat.");
            return;
        }

        DivisionRoom targetDivision = assignedDivision;

        EnqueueTask(new MoveToTask(
            () => targetDivision.transform.position,
            () => assignedDivision == targetDivision));

        Debug.Log($"[Employee] {employeeName} akan kembali ke divisi : {targetDivision.RoomName}");
    }
}