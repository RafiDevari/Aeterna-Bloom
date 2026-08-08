using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Room Divisi Botanist -- tempat kerja & spawn point employee ber-Division = EmployeeDivision.Botanist.
/// Spesialisasi : Feed & Harvest.
/// 
/// Memiliki 5 objek dekorasi (addObjects / "Adds (1)" - "Adds (5)").
/// Sesuai aturan: Secara default semua objek disembunyikan (0 assigned = 0 visible).
/// Setiap 1 employee yang di-assign ke divisi Botanist ini akan menampilkan 1 objek tambahan.
/// </summary>
public class DivisionBotanist : DivisionRoom
{
    [Header("Botanist Add Objects")]
    [Tooltip("Daftar 5 objek dekorasi yang disembunyikan secara default. Untuk setiap 1 employee yang diassign, 1 objek akan ditampilkan.")]
    [SerializeField] private List<GameObject> addObjects = new List<GameObject>();

    protected override EmployeeDivision EmployeeDivisionType => EmployeeDivision.Botanist;

    protected override void Start()
    {
        base.Start();
        AutoFindAddObjectsIfEmpty();
        UpdateBotanistAddVisuals();
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
    /// Update visibilitas 5 objek dekorasi Botanist berdasarkan jumlah employee yang diassign.
    /// </summary>
    public void UpdateBotanistAddVisuals()
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

    private int GetAssignedCount()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "RoomCreator" || sceneName == "EmployeeAssignment")
        {
            return GetEmployeesToSpawnCount();
        }

        if (AssignedEmployees != null && AssignedEmployees.Count > 0)
        {
            return AssignedEmployees.Count;
        }

        return GetEmployeesToSpawnCount();
    }

    protected override void OnEmployeeAssigned(Employee employee)
    {
        base.OnEmployeeAssigned(employee);
        UpdateBotanistAddVisuals();
    }

    protected override void OnEmployeeUnassigned(Employee employee)
    {
        base.OnEmployeeUnassigned(employee);
        UpdateBotanistAddVisuals();
    }

    public override void UpdateVisuals()
    {
        base.UpdateVisuals();
        UpdateBotanistAddVisuals();
    }
}