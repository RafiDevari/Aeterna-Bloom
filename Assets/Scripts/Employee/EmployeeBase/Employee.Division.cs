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

    /// <summary>
    /// Memeriksa apakah keahlian employee (Division) cocok dengan divisi tempat ia ditugaskan (AssignedDivision).
    /// Jika cocok (misal Botanist ditugaskan di DivisionBotanist, Researcher di DivisionResearcher), mendapat bonus movement speed +1.
    /// </summary>
    public bool IsAssignedToMatchingDivision()
    {
        return assignedDivision != null && assignedDivision.RoomDivisionType == division;
    }

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
    /// Suruh employee ini balik ke room divisi tempat dia ditugaskan (AssignedDivision).
    /// Mengarahkan employee ke lantai/walkway terdekat dari ruangan divisi.
    /// Jika clearExistingTasks = true, semua task yang sedang berjalan akan langsung dibatalkan (cancel).
    /// </summary>
    public void BackToDivision(bool clearExistingTasks = false)
    {
        if (assignedDivision == null)
        {
            Debug.Log($"[Employee] {employeeName} belum punya AssignedDivision, tetap di tempat.");
            return;
        }

        if (clearExistingTasks)
        {
            ClearTasksAndInterrupt();
        }

        DivisionRoom targetDivision = assignedDivision;

        EnqueueTask(new MoveToTask(
            () => targetDivision.GetNearestWalkablePosition(targetDivision.transform.position),
            () => assignedDivision == targetDivision));

        Debug.Log($"[Employee] {employeeName} akan kembali ke divisi : {targetDivision.RoomName}");
    }
}