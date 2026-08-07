using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI element representing an employee entry in the left panel of EmployeeViewer.
/// </summary>
public class EmployeeListCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image outlineHighlight;
    [SerializeField] private Button mainButton;

    private EmployeeInventoryItemSaveData itemData;
    private EmployeeViewerManager manager;
    private bool isSelected = false;

    public EmployeeInventoryItemSaveData ItemData => itemData;

    public void Setup(EmployeeInventoryItemSaveData data, EmployeeViewerManager managerRef)
    {
        itemData = data;
        manager = managerRef;

        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(OnCardClicked);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (itemData == null) return;

        if (nameText != null)
        {
            nameText.text = itemData.employeeName;
        }

        string cleanRole = itemData.employeePrefabName.Replace("Employee", "");
        if (roleText != null)
        {
            roleText.text = cleanRole;
        }

        if (cardBackground != null)
        {
            switch (cleanRole)
            {
                case "Botanist":
                    cardBackground.color = new Color(0.12f, 0.35f, 0.2f, 0.9f);
                    break;
                case "Researcher":
                    cardBackground.color = new Color(0.12f, 0.25f, 0.45f, 0.9f);
                    break;
                case "Security":
                    cardBackground.color = new Color(0.45f, 0.15f, 0.15f, 0.9f);
                    break;
                case "Medic":
                    cardBackground.color = new Color(0.35f, 0.15f, 0.4f, 0.9f);
                    break;
                case "Engineer":
                    cardBackground.color = new Color(0.4f, 0.3f, 0.1f, 0.9f);
                    break;
                default:
                    cardBackground.color = new Color(0.2f, 0.22f, 0.28f, 0.9f);
                    break;
            }
        }

        if (outlineHighlight != null)
        {
            outlineHighlight.enabled = isSelected;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (outlineHighlight != null)
        {
            outlineHighlight.enabled = selected;
        }
    }

    private void OnCardClicked()
    {
        if (manager != null)
        {
            manager.SelectEmployeeCard(this);
        }
    }
}
