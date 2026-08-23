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
/// dikonfigurasi lewat Inspector/EmployeeAssignment. Begitu scene mulai (Start), semua entry di-instantiate dari
/// prefab-nya, ditaruh di posisi room ini, dan otomatis di-assign tempat kerjanya ke divisi ini (AssignDivision),
/// sementara keahliannya (EmployeeDivision) tetap sesuai dengan keahlian bawaan prefab-nya.
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

        [Tooltip("Keahlian/Divisi spesifik milik employee ini.")]
        public EmployeeDivision division;

        [Tooltip("Warna baju/suit employee.")]
        public Color suitColor;

        [Tooltip("Warna rambut employee.")]
        public Color hairColor;

        [Tooltip("Mood awal/tersimpan employee (0-5).")]
        public int mood;
    }

    [Header("Employee Spawn")]
    [Tooltip("Daftar employee \"tersimpan\" yang akan di-spawn oleh divisi ini begitu scene mulai. " +
             "Tiap entry di-instantiate dari employeePrefab, posisi awal di room ini, dan otomatis " +
             "di-assign stasiun kerjanya ke divisi ini.")]
    [SerializeField] private List<EmployeeSpawnData> employeesToSpawn = new List<EmployeeSpawnData>();

    [Tooltip("Offset Y posisi spawn employee agar kakinya pas di atas lantai dan tidak keluar/tenggelam (misal 0.5f).")]
    [SerializeField] private float spawnOffsetY = 0.5f;

    [Header("Division Add Objects")]
    [Tooltip("Daftar 5 objek dekorasi yang disembunyikan secara default. Untuk setiap 1 employee yang diassign, 1 objek akan ditampilkan.")]
    [SerializeField] private List<GameObject> addObjects = new List<GameObject>();

    private readonly List<Employee> assignedEmployees = new List<Employee>();

    /// <summary>Semua employee yang sedang ditugaskan ke divisi ini (lihat Employee.AssignedDivision).</summary>
    public IReadOnlyList<Employee> AssignedEmployees => assignedEmployees;

    /// <summary>
    /// Tipe divisi yang dinaungi oleh room ini (Botanist, Researcher, Security, Medic, Engineer, Clerk).
    /// </summary>
    protected abstract EmployeeDivision EmployeeDivisionType { get; }

    /// <summary>Public accessor untuk tipe divisi room ini.</summary>
    public EmployeeDivision RoomDivisionType => EmployeeDivisionType;

    protected override void Start()
    {
        base.Start();

        AutoFindAddObjectsIfEmpty();
        UpdateAddVisuals();

        // Don't auto-spawn employees in RoomCreator or EmployeeAssignment scenes
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "RoomCreator" || sceneName == "EmployeeAssignment")
        {
            return;
        }

        SpawnEmployees();
    }

    private void AutoFindAddObjectsIfEmpty()
    {
        if (addObjects == null)
            addObjects = new List<GameObject>();

        if (addObjects.Count == 0)
        {
            for (int i = 1; i <= 5; i++)
            {
                Transform child = transform.Find($"Adds ({i})");
                if (child != null)
                {
                    addObjects.Add(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Update visibilitas objek dekorasi divisi berdasarkan jumlah employee yang diassign.
    /// </summary>
    public void UpdateAddVisuals()
    {
        AutoFindAddObjectsIfEmpty();

        int assignedCount = GetAssignedCount();

        for (int i = 0; i < addObjects.Count; i++)
        {
            if (addObjects[i] != null)
            {
                addObjects[i].SetActive(i < assignedCount);
            }
        }
    }

    public int GetAssignedCount()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "RoomCreator" || sceneName == "EmployeeAssignment")
        {
            return GetEmployeesToSpawnCount();
        }

        if (assignedEmployees != null && assignedEmployees.Count > 0)
        {
            return assignedEmployees.Count;
        }

        return GetEmployeesToSpawnCount();
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

            Vector3 spawnPos = GetNearestWalkablePosition(transform.position) + new Vector3(0f, spawnOffsetY, 0f);
            Employee employee = Instantiate(data.employeePrefab, spawnPos, Quaternion.identity);

            if (!string.IsNullOrEmpty(data.employeeName))
                employee.EmployeeName = data.employeeName;

            employee.RefreshAppearanceFromInventory();

            if (data.suitColor.a > 0f || data.hairColor.a > 0f)
            {
                if (employee.Appearance != null)
                {
                    if (data.suitColor.a > 0f && employee.Appearance.SuitColor == Color.white)
                        employee.Appearance.SuitColor = data.suitColor;
                    if (data.hairColor.a > 0f && employee.Appearance.HairColor == Color.white)
                        employee.Appearance.HairColor = data.hairColor;
                }
            }

            // Set division variable from JSON / spawn data
            employee.SetDivision(data.division);

            if (data.mood > 0)
            {
                employee.SetMood(data.mood);
            }

            // Maintain native EmployeeDivision from prefab (do not overwrite with room's division)
            employee.AssignDivision(this);

            Debug.Log($"[{RoomName}] Spawn employee : {employee.EmployeeName} ({employee.Division}).");
        }
    }

    /// <summary>
    /// Mendapatkan jumlah entry employee yang tersimpan di employeesToSpawn.
    /// </summary>
    public int GetEmployeesToSpawnCount() => employeesToSpawn != null ? employeesToSpawn.Count : 0;

    /// <summary>
    /// Hook virtual yang dipanggil ketika employee berhasil di-assign ke divisi ini.
    /// </summary>
    protected virtual void OnEmployeeAssigned(Employee employee)
    {
        UpdateAddVisuals();
    }

    /// <summary>
    /// Hook virtual yang dipanggil ketika employee keluar/di-unassign dari divisi ini.
    /// </summary>
    protected virtual void OnEmployeeUnassigned(Employee employee)
    {
        UpdateAddVisuals();
    }

    /// <summary>
    /// Virtual method untuk meng-update tampilan visual room yang bergantung pada state/assignment.
    /// </summary>
    public virtual void UpdateVisuals()
    {
        UpdateAddVisuals();
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
        OnEmployeeAssigned(employee);
        UpdateVisuals();
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
        OnEmployeeUnassigned(employee);
        UpdateVisuals();
    }
}