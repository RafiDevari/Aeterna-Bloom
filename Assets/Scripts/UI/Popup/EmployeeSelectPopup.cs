using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sub-popup pemilihan Employee, di-PAGE per DivisionRoom. Satu halaman = satu INSTANCE
/// DivisionRoom (bukan per tipe) -- kalau ada 2 DivisionBotanist di scene, keduanya tetap
/// dapat halaman masing-masing, isinya cuma employee yang AssignedDivision-nya persis room
/// itu (lihat DivisionRoom.AssignedEmployees).
///
/// Tombol Next/Back geser antar halaman (wrap-around: dari halaman terakhir Next balik lagi
/// ke halaman pertama, dan sebaliknya).
///
/// GENERIC lewat callback: dipanggil dengan Action&lt;Employee&gt; yang berisi apa yang mau
/// dilakukan begitu employee dipilih (mis. GoFeed, GoResearch, GoHarvest, atau job lain di
/// masa depan). Popup ini sendiri TIDAK tahu-menahu soal feed/research -- itu keputusan si
/// pemanggil (NutrisiPopup, ContainmentPopup, dst), popup ini cuma bertugas "kasih daftar
/// employee per divisi, laporkan siapa yang dipilih".
///
/// Contoh pakai dari NutrisiPopup (feed) :
///   EmployeeSelectPopup.Instance.Open(employee => employee.GoFeed(targetUnit, selectedFood));
///
/// Contoh pakai dari ContainmentPopup (research/harvest) :
///   EmployeeSelectPopup.Instance.Open(employee => employee.GoResearch(targetUnit));
///   EmployeeSelectPopup.Instance.Open(employee => employee.GoHarvest(targetUnit));
///
/// Sama seperti popup lain: taruh script ini di GameObject yang SELALU AKTIF,
/// bukan di GameObject visual (popupRoot).
/// </summary>
public class EmployeeSelectPopup : PopupBase
{
    public static EmployeeSelectPopup Instance { get; private set; }

    [Header("Room Paging")]
    [Tooltip("Nama room halaman yang sedang ditampilkan, mis. \"Divisi Botanist\".")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;

    [Header("Employee Buttons")]
    [Tooltip("Parent yang menampung tombol employee hasil generate. Sebaiknya punya Vertical Layout Group + Content Size Fitter.")]
    [SerializeField] private Transform buttonContainer;

    [Tooltip("Prefab tombol employee. Harus punya component Button + Text/TMP_Text di child-nya.")]
    [SerializeField] private Button employeeButtonPrefab;

    [Header("Empty State (opsional)")]
    [Tooltip("Opsional -- GameObject (mis. teks \"Tidak ada employee di divisi ini\") yang diaktifkan kalau halaman saat ini kosong.")]
    [SerializeField] private GameObject emptyStateLabel;

    private System.Action<Employee> onEmployeeSelected;

    private readonly List<GameObject> spawnedButtons = new();
    private readonly List<DivisionRoom> pages = new();

    private int currentPageIndex;
    private System.Type priorityDivisionType;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNextPage);

        if (backButton != null)
            backButton.onClick.AddListener(GoToPreviousPage);
    }

    /// <summary>
    /// Buka popup pilih employee. Begitu salah satu employee di-klik, callback
    /// dipanggil dengan employee itu, lalu popup otomatis Close().
    /// </summary>
    public void Open(System.Action<Employee> onEmployeeSelected, System.Type priorityDivisionType = null)
    {
        this.onEmployeeSelected = onEmployeeSelected;
        this.priorityDivisionType = priorityDivisionType;
        BuildPages();
        currentPageIndex = 0;
        RefreshCurrentPage();
        base.Open();
    }

    //────────────────────────────────────────────────────────
    // Paging
    //────────────────────────────────────────────────────────

    private void BuildPages()
    {
        pages.Clear();
        if (Facility.Instance == null)
            return;

        // Tiap INSTANCE DivisionRoom jadi 1 halaman sendiri -- kalau ada beberapa room
        // dengan tipe yang sama, semuanya tetap ditampilkan terpisah, bukan digabung.
        IEnumerable<DivisionRoom> rooms = Facility.Instance.Rooms.OfType<DivisionRoom>();

        // Kalau si pemanggil kasih tipe prioritas (mis. Research -> DivisionResearcher),
        // room dengan tipe itu ditaruh duluan. OrderBy stabil, jadi urutan asli antar
        // room dengan prioritas sama (baik yang prioritas maupun yang bukan) tetap terjaga.
        if (priorityDivisionType != null)
        {
            rooms = rooms.OrderBy(room => room.GetType() == priorityDivisionType ? 0 : 1);
        }

        pages.AddRange(rooms);
    }   

    private void RefreshCurrentPage()
    {
        ClearButtons();

        if (pages.Count == 0)
        {
            if (roomNameText != null)
                roomNameText.text = "Tidak ada divisi.";

            SetPagingInteractable(false);
            SetEmptyState(true);
            return;
        }

        currentPageIndex = ((currentPageIndex % pages.Count) + pages.Count) % pages.Count;

        DivisionRoom room = pages[currentPageIndex];

        if (roomNameText != null)
            roomNameText.text = room.RoomName;

        // Next/Back cuma perlu aktif kalau ada lebih dari 1 halaman buat di-geser.
        SetPagingInteractable(pages.Count > 1);

        BuildEmployeeButtons(room);
    }

    private void GoToNextPage()
    {
        if (pages.Count == 0)
            return;

        currentPageIndex++;
        RefreshCurrentPage();
    }

    private void GoToPreviousPage()
    {
        if (pages.Count == 0)
            return;

        currentPageIndex--;
        RefreshCurrentPage();
    }

    private void SetPagingInteractable(bool interactable)
    {
        if (nextButton != null)
            nextButton.interactable = interactable;

        if (backButton != null)
            backButton.interactable = interactable;
    }

    private void SetEmptyState(bool show)
    {
        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(show);
    }

    //────────────────────────────────────────────────────────
    // Employee Buttons
    //────────────────────────────────────────────────────────

    private void BuildEmployeeButtons(DivisionRoom room)
    {
        bool hasAnyEmployee = false;

        foreach (Employee employee in room.AssignedEmployees)
        {
            if (employee == null)
                continue;

            hasAnyEmployee = true;

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

        SetEmptyState(!hasAnyEmployee);
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
        priorityDivisionType = null; // <-- tambahan
        pages.Clear();
        currentPageIndex = 0;
    }
}