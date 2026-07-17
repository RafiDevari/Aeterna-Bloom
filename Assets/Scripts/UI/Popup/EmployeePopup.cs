using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup sederhana yang muncul saat Employee di-klik (kanan).
/// Menampilkan nama + divisi employee. Berisi pilihan: Close.
///
/// PENTING: sama seperti ContainmentPopup, script ini harus ada di
/// GameObject yang SELALU AKTIF (misalnya langsung di Canvas), BUKAN
/// di GameObject visual popup (popupRoot) yang di-toggle aktif/nonaktif.
/// Lihat komentar di PopupBase.cs untuk alasannya.
/// </summary>
public class EmployeePopup : PopupBase
{
    public static EmployeePopup Instance { get; private set; }

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text divisionText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private Employee targetEmployee;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        closeButton.onClick.AddListener(Close);

        Employee.OnAnyEmployeeRightClicked += HandleEmployeeRightClicked;
    }

    private void OnDestroy()
    {
        Employee.OnAnyEmployeeRightClicked -= HandleEmployeeRightClicked;
    }

    private void HandleEmployeeRightClicked(Employee employee)
    {
        Open(employee);
    }

    public void Open(Employee employee)
    {
        targetEmployee = employee;

        if (nameText != null)
            nameText.text = employee != null ? employee.EmployeeName : "-";

        if (divisionText != null)
            divisionText.text = employee != null ? employee.Division.ToString() : "-";

        base.Open();
    }

    protected override void OnClosed()
    {
        targetEmployee = null;
    }
}