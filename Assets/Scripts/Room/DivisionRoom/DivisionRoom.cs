using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class untuk semua Room Divisi (tempat kerja/stasiun employee per jenis spesialisasi).
///
/// Employee di-assign ke satu DivisionRoom lewat Employee.AssignDivision(), dan bisa balik
/// ke sini lewat Employee.BackToDivision() (mis. begitu selesai job research/harvest, lihat
/// Employee.cs).
///
/// SPAWN: tiap DivisionRoom punya daftar employee "tersimpan" (employeesToSpawn) yang
/// dikonfigurasi lewat Inspector. Begitu scene mulai (Start), semua entry di-instantiate dari
/// prefab-nya, ditaruh di posisi room ini, otomatis di-assign ke divisi ini (AssignDivision),
/// dan keahliannya (EmployeeDivision) otomatis dipaksa ikut tipe room ini lewat
/// EmployeeDivisionType (lihat DivisionBotanist/DivisionResearcher) -- jadi employee manapun
/// yang ditaruh di list DivisionBotanist otomatis jadi EmployeeDivision.Botanist, dst, tanpa
/// perlu di-set manual satu-satu di tiap prefab.
///
/// Child class spesifik : DivisionBotanist, DivisionResearcher (nanti bisa nambah lagi, mis.
/// DivisionClerk dsb).
/// </summary>
public abstract class DivisionRoom : Room
{
    [System.Serializable]
    public struct EmployeeSpawnData
    {
        [Tooltip("Opsional -- kalau diisi, dipakai buat overwrite EmployeeName hasil instantiate. Kosongkan buat pakai nama bawaan prefab.")]
        public string employeeName;

        [Tooltip("Prefab Employee (harus punya component Employee) yang mau di-spawn.")]
        public Employee employeePrefab;
    }

    [Header("Employee Spawn")]
    [Tooltip("Daftar employee \"tersimpan\" yang akan di-spawn oleh divisi ini begitu scene mulai. " +
             "Tiap entry di-instantiate dari employeePrefab, posisi awal di room ini, otomatis " +
             "di-assign ke divisi ini, dan keahliannya dipaksa ikut EmployeeDivisionType room ini.")]
    [SerializeField] private List<EmployeeSpawnData> employeesToSpawn = new List<EmployeeSpawnData>();

    private readonly List<Employee> assignedEmployees = new List<Employee>();

    /// <summary>Semua employee yang sedang ditugaskan ke divisi ini (lihat Employee.AssignedDivision).</summary>
    public IReadOnlyList<Employee> AssignedEmployees => assignedEmployees;

    /// <summary>
    /// Tipe keahlian yang dipaksakan ke semua employee yang di-spawn dari divisi ini.
    /// Diisi oleh child class spesifik (DivisionBotanist -> Botanist, DivisionResearcher -> Researcher).
    /// </summary>
    protected abstract EmployeeDivision EmployeeDivisionType { get; }

    protected override void Start()
    {
        base.Start();

        // Don't auto-spawn employees in RoomCreator or EmployeeAssignment scenes
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "RoomCreator" || sceneName == "EmployeeAssignment")
        {
            return;
        }

        SpawnEmployees();
    }

    private bool hasSpawnedEmployees = false;

    /// <summary>
    /// Instantiate semua entry di employeesToSpawn, taruh di posisi room ini, assign ke divisi
    /// ini, dan paksa EmployeeDivision-nya ikut EmployeeDivisionType. Dipanggil otomatis di
    /// Start() -- boleh juga dipanggil manual (mis. buat spawn tambahan di runtime lewat kode lain).
    /// </summary>
    public void SpawnEmployees()
    {
        if (hasSpawnedEmployees) return;
        hasSpawnedEmployees = true;

        foreach (var data in employeesToSpawn)
        {
            if (data.employeePrefab == null)
            {
                Debug.LogWarning($"[{RoomName}] Ada entry employeesToSpawn dengan prefab kosong, dilewati.");
                continue;
            }

            Employee employee = Instantiate(data.employeePrefab, transform.position, Quaternion.identity);

            if (!string.IsNullOrEmpty(data.employeeName))
                employee.EmployeeName = data.employeeName;

            employee.SetDivision(EmployeeDivisionType);
            employee.AssignDivision(this);

            Debug.Log($"[{RoomName}] Spawn employee : {employee.EmployeeName} ({EmployeeDivisionType}).");
        }
    }

    /// <summary>
    /// Dipanggil dari Employee.AssignDivision() -- jangan panggil ini langsung dari luar,
    /// panggil Employee.AssignDivision(divisionRoom) supaya sisi Employee-nya ikut ke-update.
    /// </summary>
    public void AssignEmployee(Employee employee)
    {
        if (employee == null || assignedEmployees.Contains(employee))
            return;

        assignedEmployees.Add(employee);

        Debug.Log($"[{RoomName}] {employee.EmployeeName} bergabung ke divisi ini.");
    }

    /// <summary>
    /// Dipanggil dari Employee.AssignDivision()/OnDestroy -- jangan panggil ini langsung dari
    /// luar, panggil lewat sisi Employee supaya konsisten.
    /// </summary>
    public void UnassignEmployee(Employee employee)
    {
        if (employee == null)
            return;

        assignedEmployees.Remove(employee);

        Debug.Log($"[{RoomName}] {employee.EmployeeName} keluar dari divisi ini.");
    }
}