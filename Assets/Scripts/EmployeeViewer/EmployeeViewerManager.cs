using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manager for the Employee Viewer scene.
/// Displays an employee list on the left side and previews the selected employee's model and details on the right side.
/// </summary>
public class EmployeeViewerManager : MonoBehaviour
{
    public static EmployeeViewerManager Instance { get; private set; }

    [Header("Employee Prefabs")]
    [SerializeField] private List<GameObject> employeePrefabs = new List<GameObject>();

    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedRoleText;
    [SerializeField] private TextMeshProUGUI selectedDetailsText;

    [Header("Preview Settings")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Vector3 previewScale = new Vector3(10f, 20f, 1f);

    private Dictionary<string, GameObject> employeePrefabMap = new Dictionary<string, GameObject>();
    private List<EmployeeListCardUI> cardUIList = new List<EmployeeListCardUI>();
    private EmployeeListCardUI selectedCard;
    private GameObject currentPreviewInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePrefabMap();
    }

    private void Start()
    {
        InitializePrefabMap();
        LoadAndDisplayEmployeeList();
    }

    private void InitializePrefabMap()
    {
        employeePrefabMap.Clear();
        foreach (var prefab in employeePrefabs)
        {
            if (prefab == null) continue;
            if (!employeePrefabMap.ContainsKey(prefab.name))
            {
                employeePrefabMap.Add(prefab.name, prefab);
            }
        }
    }

    /// <summary>
    /// Loads employee data from EmployeeInventorySaveSystem and populates the left panel list.
    /// </summary>
    public void LoadAndDisplayEmployeeList()
    {
        // Clear previous cards
        foreach (var card in cardUIList)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        cardUIList.Clear();

        EmployeeInventoryData invData = null;
        EmployeeInventorySaveSystem invSystem = EmployeeInventorySaveSystem.Instance;
        if (invSystem == null) invSystem = FindFirstObjectByType<EmployeeInventorySaveSystem>();

        if (invSystem != null)
        {
            invData = invSystem.LoadInventory();
        }
        else
        {
            // If no system instance exists in scene, create one to handle loading employee_inventory.json
            GameObject go = new GameObject("EmployeeInventorySaveSystem");
            invSystem = go.AddComponent<EmployeeInventorySaveSystem>();
            invData = invSystem.LoadInventory();
        }

        if (cardContainer == null || cardPrefab == null)
        {
            Debug.LogWarning("[EmployeeViewerManager] Card container or card prefab is missing!");
            return;
        }

        foreach (var empData in invData.employees)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            cardObj.SetActive(true);

            EmployeeListCardUI cardUI = cardObj.GetComponent<EmployeeListCardUI>();
            if (cardUI == null)
            {
                cardUI = cardObj.AddComponent<EmployeeListCardUI>();
            }

            cardUI.Setup(empData, this);
            cardUIList.Add(cardUI);
        }

        // Auto-select the first employee by default if available
        if (cardUIList.Count > 0)
        {
            SelectEmployeeCard(cardUIList[0]);
        }
    }

    /// <summary>
    /// Handles selection of an employee card.
    /// </summary>
    public void SelectEmployeeCard(EmployeeListCardUI cardUI)
    {
        if (cardUI == null || cardUI.ItemData == null) return;

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = cardUI;
        selectedCard.SetSelected(true);

        DisplayEmployeePreview(cardUI.ItemData);
    }

    /// <summary>
    /// Spawns and displays the visual representation of the employee in the scene preview.
    /// </summary>
    private void DisplayEmployeePreview(EmployeeInventoryItemSaveData empData)
    {
        // Destroy existing preview instance
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        Vector3 spawnPos = previewAnchor != null ? previewAnchor.position : new Vector3(6.4f, -0.2f, 0f);

        if (employeePrefabMap.TryGetValue(empData.employeePrefabName, out GameObject prefab))
        {
            currentPreviewInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
            currentPreviewInstance.name = $"Preview_{empData.employeeName}";
            currentPreviewInstance.transform.localScale = previewScale;

            EmployeeAppearance appearance = currentPreviewInstance.GetComponent<EmployeeAppearance>();
            if (appearance != null)
            {
                Color suit = empData.suitColor.a > 0f ? empData.suitColor : Color.white;
                Color hair = empData.hairColor.a > 0f ? empData.hairColor : Color.white;
                appearance.SetAppearanceColors(suit, hair);
            }

            // Preserving relative sorting orders while moving the character layer in front of the UI panel (order 0)
            SpriteRenderer[] spriteRenderers = currentPreviewInstance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                sr.sortingOrder += 50;
            }

            // Ensure preview employee components are in passive/idle state
            Employee empComp = currentPreviewInstance.GetComponent<Employee>();
            if (empComp != null)
            {
                empComp.enabled = false; // Disable AI movement/tasks in viewer mode
            }

            // Ensure collider is disabled to prevent physics interactions
            Collider2D col = currentPreviewInstance.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            Rigidbody2D rb = currentPreviewInstance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
            }
        }
        else
        {
            Debug.LogWarning($"[EmployeeViewerManager] Prefab '{empData.employeePrefabName}' not found in employeePrefabMap!");
        }

        // Update Info UI
        string roleName = empData.employeePrefabName.Replace("Employee", "");
        if (selectedNameText != null) selectedNameText.text = empData.employeeName;
        if (selectedRoleText != null) selectedRoleText.text = $"Role: {roleName}";
        if (selectedDetailsText != null)
        {
            selectedDetailsText.text = GetRoleDescription(roleName);
        }
    }

    private string GetRoleDescription(string role)
    {
        switch (role)
        {
            case "Botanist":
                return "Specialist in plant cultivation, harvesting, and flora research.";
            case "Researcher":
                return "Conducts laboratory analysis, data processing, and technology upgrades.";
            case "Security":
                return "Maintains facility safety, suppresses containment breaches, and patrols corridors.";
            case "Medic":
                return "Provides medical care, restores employee health, and manages bio-hazards.";
            case "Engineer":
                return "Handles facility repairs, electrical grid maintenance, and machinery upgrades.";
            default:
                return "Facility staff member.";
        }
    }
}
