using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sub-popup Nutrisi, muncul saat tombol "Nutrisi" di ContainmentUnitPopup
/// ditekan.
/// </summary>
public class NutrisiPopup : PopupBase
{
    public static NutrisiPopup Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button natriumButton;
    [SerializeField] private Button fosforButton;
    [SerializeField] private Button kaliumButton;
    [SerializeField] private Button magnesiumButton;

    private ContainmentUnit targetUnit;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        natriumButton.onClick.AddListener(() => SelectNutrition(FoodType.Natrium));
        fosforButton.onClick.AddListener(() => SelectNutrition(FoodType.Fosfor));
        kaliumButton.onClick.AddListener(() => SelectNutrition(FoodType.Kalium));
        magnesiumButton.onClick.AddListener(() => SelectNutrition(FoodType.Magnesium));
    }

    public void Open(ContainmentUnit unit)
    {
        targetUnit = unit;
        base.Open();
    }

    private void SelectNutrition(FoodType food)
    {
        Debug.Log($"[NutrisiPopup] Nutrisi dipilih: {food} untuk unit: {targetUnit?.UnitName}");

        if (EmployeeSelectPopup.Instance != null)
        {
            // PopupManager akan otomatis menutup popup ini saat popup baru dibuka.
            EmployeeSelectPopup.Instance.Open(targetUnit, food);
        }
    }

    protected override void OnClosed()
    {
        targetUnit = null;
    }
}