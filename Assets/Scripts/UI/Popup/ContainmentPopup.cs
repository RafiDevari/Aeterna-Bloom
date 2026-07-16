using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup yang muncul saat ContainmentUnit (yang punya monster) ditekan.
/// Berisi pilihan: Nutrisi, Research, Harvest (kondisional), Info, Close.
///
/// PENTING: script ini harus ada di GameObject yang SELALU AKTIF (misalnya
/// langsung di Canvas, atau GameObject controller kosong terpisah) -- BUKAN
/// di GameObject visual popup (popupRoot) yang di-toggle aktif/nonaktif.
/// Lihat komentar di PopupBase.cs untuk alasannya.
/// </summary>
public class ContainmentPopup : PopupBase
{
    public static ContainmentPopup Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button nutrisiButton;
    [SerializeField] private Button researchButton;
    [SerializeField] private Button harvestButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button closeButton;

    private ContainmentUnit targetUnit;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        nutrisiButton.onClick.AddListener(OnNutrisiClicked);
        researchButton.onClick.AddListener(OnResearchClicked);
        harvestButton.onClick.AddListener(OnHarvestClicked);
        infoButton.onClick.AddListener(OnInfoClicked);
        closeButton.onClick.AddListener(Close);

        ContainmentUnit.OnAnyUnitClicked += HandleUnitClicked;
    }

    private void OnDestroy()
    {
        ContainmentUnit.OnAnyUnitClicked -= HandleUnitClicked;
    }

    private void HandleUnitClicked(ContainmentUnit unit)
    {
        Open(unit);
    }

    public void Open(ContainmentUnit unit)
    {
        targetUnit = unit;

        // Tombol Harvest cuma muncul kalau growth monster-nya sudah lewat 100% (Overgrowth).
        // Lihat MonsterBase.IsOvergrown (MonsterBase.Harvest.cs).
        bool showHarvest = unit != null && unit.HasMonster && unit.Monster.IsOvergrown;
        harvestButton.gameObject.SetActive(showHarvest);

        base.Open();
    }

    private void OnNutrisiClicked()
    {
        Debug.Log($"[ContainmentUnitPopup] NutrisiPopup. ");

        // Buka sub-popup Nutrisi. Tidak perlu panggil Close() di sini —
        // PopupManager otomatis menutup popup ini begitu NutrisiPopup dibuka.
        if (NutrisiPopup.Instance != null)
        {
            Debug.Log($"[ContainmentUnitPopup] Membuka NutrisiPopup untuk unit: {targetUnit?.UnitName}");
            NutrisiPopup.Instance.Open(targetUnit);
        }
        else
            Debug.LogError("[ContainmentUnitPopup] NutrisiPopup.Instance belum ada. Pastikan component NutrisiPopup sudah ditambahkan ke scene.");
    }

    private void OnResearchClicked()
    {
        Debug.Log($"[ContainmentUnitPopup] Research ditekan untuk unit: {targetUnit?.UnitName}");

        // Sama seperti Nutrisi: tidak perlu panggil Close() di sini kalau PopupManager
        // otomatis menutup popup ini begitu EmployeeSelectPopup dibuka.
        if (EmployeeSelectPopup.Instance != null)
        {
            ContainmentUnit unit = targetUnit; // capture, targetUnit di-reset saat popup ini close

            EmployeeSelectPopup.Instance.Open(employee => employee.GoResearch(unit), typeof(DivisionResearcher));
        }
        else
            Debug.LogError("[ContainmentUnitPopup] EmployeeSelectPopup.Instance belum ada. Pastikan component EmployeeSelectPopup sudah ditambahkan ke scene.");
    }

    private void OnHarvestClicked()
    {
        Debug.Log($"[ContainmentUnitPopup] Harvest ditekan untuk unit: {targetUnit?.UnitName}");

        // Sama seperti Research/Nutrisi: tidak perlu panggil Close() di sini kalau PopupManager
        // otomatis menutup popup ini begitu EmployeeSelectPopup dibuka.
        if (EmployeeSelectPopup.Instance != null)
        {
            ContainmentUnit unit = targetUnit; // capture, targetUnit di-reset saat popup ini close

            EmployeeSelectPopup.Instance.Open(employee => employee.GoHarvest(unit), typeof(DivisionBotanist));
        }
        else
            Debug.LogError("[ContainmentUnitPopup] EmployeeSelectPopup.Instance belum ada. Pastikan component EmployeeSelectPopup sudah ditambahkan ke scene.");
    }

    private void OnInfoClicked()
    {
        Debug.Log($"[ContainmentUnitPopup] Info ditekan untuk unit: {targetUnit?.UnitName}");

        // Sama seperti Nutrisi: tidak perlu panggil Close() di sini kalau PopupManager
        // otomatis menutup popup ini begitu MonsterInfoPopup dibuka.
        if (MonsterInfoPopup.Instance != null)
        {
            MonsterInfoPopup.Instance.Open(targetUnit);
        }
        else
            Debug.LogError("[ContainmentUnitPopup] MonsterInfoPopup.Instance belum ada. Pastikan component MonsterInfoPopup sudah ditambahkan ke scene.");
    }

    protected override void OnClosed()
    {
        targetUnit = null;
    }
}