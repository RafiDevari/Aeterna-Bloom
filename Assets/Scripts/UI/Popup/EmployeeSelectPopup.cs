using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sub-popup pemilihan Employee, muncul setelah salah satu nutrisi dipilih
/// di NutrisiPopup. Menampilkan 1 tombol untuk tiap Employee yang terdaftar
/// di Facility, dengan nama employee sebagai teks tombolnya.
///
/// Sama seperti popup lain: taruh script ini di GameObject yang SELALU AKTIF,
/// bukan di GameObject visual (popupRoot).
/// </summary>
public class EmployeeSelectPopup : PopupBase
{
    public static EmployeeSelectPopup Instance { get; private set; }

    [Header("Employee Buttons")]
    [Tooltip("Parent yang menampung tombol employee hasil generate. Sebaiknya punya Vertical Layout Group + Content Size Fitter.")]
    [SerializeField] private Transform buttonContainer;

    [Tooltip("Prefab tombol employee. Harus punya component Button + Text/TMP_Text di child-nya.")]
    [SerializeField] private Button employeeButtonPrefab;

    private ContainmentUnit targetUnit;
    private string nutritionName;

    private readonly List<GameObject> spawnedButtons = new();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public void Open(ContainmentUnit unit, string nutrition)
    {
        targetUnit = unit;
        nutritionName = nutrition;

        BuildEmployeeButtons();

        base.Open();
    }

    private void BuildEmployeeButtons()
    {
        ClearButtons();

        if (Facility.Instance == null)
            return;

        foreach (Employee employee in Facility.Instance.Employees)
        {
            if (employee == null)
                continue;

            Button btn = Instantiate(employeeButtonPrefab, buttonContainer);
            btn.gameObject.SetActive(true);

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                label.text = employee.EmployeeName;
            }

            Employee capturedEmployee = employee; // hindari closure bug di foreach
            btn.onClick.AddListener(() => OnEmployeeClicked(capturedEmployee));

            spawnedButtons.Add(btn.gameObject);
        }
    }

    private void ClearButtons()
    {
        foreach (GameObject go in spawnedButtons)
        {
            if (go != null)
                Destroy(go);
        }

        spawnedButtons.Clear();
    }

    private void OnEmployeeClicked(Employee employee)
    {
        if (targetUnit == null || !targetUnit.HasMonster)
        {
            Close();
            return;
        }

        employee.FeedMonster(targetUnit.Monster);

        Debug.Log( $"Employee {employee.EmployeeName} berjalan menuju {targetUnit.Monster.MonsterName} untuk memberi nutrisi {nutritionName}.");

        Close();
    }

    protected override void OnClosed()
    {
        ClearButtons();
        targetUnit = null;
        nutritionName = null;
    }
}