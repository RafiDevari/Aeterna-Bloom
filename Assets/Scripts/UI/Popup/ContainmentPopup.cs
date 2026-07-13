using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup yang muncul saat ContainmentUnit (yang punya monster) ditekan.
/// Berisi 4 pilihan: Nutrisi, Research, Info, Close.
///
/// PENTING: script ini harus ada di GameObject yang SELALU AKTIF (misalnya
/// langsung di Canvas, atau GameObject controller kosong terpisah) -- BUKAN
/// di GameObject visual popup (popupRoot) yang di-toggle aktif/nonaktif.
/// Lihat komentar di PopupBase.cs untuk alasannya.
///
/// Untuk sekarang setiap pilihan cuma nge-print ke console lalu popup
/// otomatis close.
/// </summary>
public class ContainmentPopup : PopupBase
{
    public static ContainmentPopup Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button nutrisiButton;
    [SerializeField] private Button researchButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button closeButton;

    private ContainmentUnit targetUnit;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        nutrisiButton.onClick.AddListener(OnNutrisiClicked);
        researchButton.onClick.AddListener(OnResearchClicked);
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
 
            EmployeeSelectPopup.Instance.Open(employee => employee.GoResearch(unit));
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