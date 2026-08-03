using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmployeeInventoryCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image outlineHighlight;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button unassignButton;

    private EmployeeInventoryItemSaveData itemData;
    private EmployeeAssignmentManager manager;
    private bool isSelected = false;

    public EmployeeInventoryItemSaveData ItemData => itemData;

    public void Setup(EmployeeInventoryItemSaveData data, EmployeeAssignmentManager managerRef)
    {
        itemData = data;
        manager = managerRef;

        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(OnCardClicked);
        }

        if (unassignButton != null)
        {
            unassignButton.onClick.RemoveAllListeners();
            unassignButton.onClick.AddListener(OnUnassignClicked);
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

        // Parse clean role name (e.g., "EmployeeBotanist" -> "Botanist")
        string cleanRole = itemData.employeePrefabName.Replace("Employee", "");
        if (roleText != null)
        {
            roleText.text = cleanRole;
        }

        // Set card colors depending on the role to make it look gorgeous
        if (cardBackground != null)
        {
            switch (cleanRole)
            {
                case "Botanist":
                    cardBackground.color = new Color(0.12f, 0.35f, 0.2f, 0.9f); // Greenish
                    break;
                case "Researcher":
                    cardBackground.color = new Color(0.12f, 0.25f, 0.45f, 0.9f); // Bluish
                    break;
                case "Security":
                    cardBackground.color = new Color(0.45f, 0.15f, 0.15f, 0.9f); // Redish
                    break;
                case "Medic":
                    cardBackground.color = new Color(0.35f, 0.15f, 0.4f, 0.9f); // Purpleish
                    break;
                case "Engineer":
                    cardBackground.color = new Color(0.4f, 0.3f, 0.1f, 0.9f); // Yellow/Orange
                    break;
                default:
                    cardBackground.color = new Color(0.2f, 0.22f, 0.28f, 0.9f); // Default gray
                    break;
            }
        }

        // Check if assigned in manager
        string assignedRoomName = manager != null ? manager.GetAssignedRoomForEmployee(itemData.employeeName) : null;
        bool isAssigned = !string.IsNullOrEmpty(assignedRoomName);

        if (statusText != null)
        {
            if (isAssigned)
            {
                statusText.text = $"Room: {assignedRoomName}";
                statusText.color = new Color(0.4f, 1f, 0.5f); // Neon green
            }
            else
            {
                statusText.text = "Unassigned";
                statusText.color = new Color(0.7f, 0.7f, 0.7f); // Gray
            }
        }

        if (unassignButton != null)
        {
            unassignButton.gameObject.SetActive(isAssigned);
        }

        // Selection outline highlight
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

    private void OnUnassignClicked()
    {
        if (manager != null && itemData != null)
        {
            manager.UnassignEmployee(itemData.employeeName);
        }
    }
}
