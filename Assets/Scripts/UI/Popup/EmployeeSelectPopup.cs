using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sub-popup pemilihan Employee. Menampilkan 1 tombol untuk tiap Employee yang
/// terdaftar di Facility, dengan nama employee sebagai teks tombolnya.
///
/// GENERIC lewat callback: dipanggil dengan Action&lt;Employee&gt; yang berisi apa
/// yang mau dilakukan begitu employee dipilih (mis. GoFeed, GoResearch, atau job
/// lain di masa depan). Popup ini sendiri TIDAK tahu-menahu soal feed/research --
/// itu keputusan si pemanggil (NutrisiPopup, ContainmentPopup, dst), popup ini
/// cuma bertugas "kasih daftar employee, laporkan siapa yang dipilih".
///
/// Contoh pakai dari NutrisiPopup (feed) :
///   EmployeeSelectPopup.Instance.Open(employee => employee.GoFeed(targetUnit, selectedFood));
///
/// Contoh pakai dari ContainmentPopup (research) :
///   EmployeeSelectPopup.Instance.Open(employee => employee.GoResearch(targetUnit));
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

    private System.Action<Employee> onEmployeeSelected;

    private readonly List<GameObject> spawnedButtons = new();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    /// <summary>
    /// Buka popup pilih employee. Begitu salah satu employee di-klik, callback
    /// dipanggil dengan employee itu, lalu popup otomatis Close().
    /// </summary>
    public void Open(System.Action<Employee> onEmployeeSelected)
    {
        this.onEmployeeSelected = onEmployeeSelected;

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
        // Simpan dulu & clear field sebelum invoke, biar OnClosed() (dipanggil
        // dari Close()) tidak ikut menghapus callback yang belum sempat jalan.
        var callback = onEmployeeSelected;

        Close();

        callback?.Invoke(employee);
    }

    protected override void OnClosed()
    {
        ClearButtons();
        onEmployeeSelected = null;
    }
}