using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sub-popup Nutrisi, muncul saat tombol "Nutrisi" di ContainmentUnitPopup
/// ditekan. Pilihan: Natrium, Phosfor, Kalium, Magnesium.
///
/// Sama seperti ContainmentUnitPopup: taruh script ini di GameObject yang
/// SELALU AKTIF (misalnya "PopupController"), bukan di GameObject visualnya.
///
/// Untuk sekarang tiap pilihan cuma print ke console lalu popup close.
/// </summary>
public class NutrisiPopup : PopupBase
{
    public static NutrisiPopup Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button natriumButton;
    [SerializeField] private Button phosforButton;
    [SerializeField] private Button kaliumButton;
    [SerializeField] private Button magnesiumButton;

    private ContainmentUnit targetUnit;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        natriumButton.onClick.AddListener(() => SelectNutrition("Natrium"));
        phosforButton.onClick.AddListener(() => SelectNutrition("Phosfor"));
        kaliumButton.onClick.AddListener(() => SelectNutrition("Kalium"));
        magnesiumButton.onClick.AddListener(() => SelectNutrition("Magnesium"));
    }

    public void Open(ContainmentUnit unit)
    {
        targetUnit = unit;
        base.Open();
    }

    private void SelectNutrition(string nutritionName)
    {
        Debug.Log($"[NutrisiPopup] Nutrisi dipilih: {nutritionName} untuk unit: {targetUnit?.UnitName}");

        if (EmployeeSelectPopup.Instance != null)
        {
            // RequestOpen di dalam Open() akan otomatis menutup popup ini
            // (lihat PopupManager), jadi tidak perlu panggil Close() manual.
            EmployeeSelectPopup.Instance.Open(targetUnit, nutritionName);
        }
    }

    protected override void OnClosed()
    {
        targetUnit = null;
    }
}