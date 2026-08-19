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
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedRoleText;
    [SerializeField] private TextMeshProUGUI selectedDetailsText;

    [Header("Customization UI References")]
    [SerializeField] private Button suitButton;
    [SerializeField] private Button hairButton;
    [SerializeField] private GameObject colorPalettePanel;
    [SerializeField] private Button colorRedBtn;
    [SerializeField] private Button colorGreenBtn;
    [SerializeField] private Button colorBlackBtn;
    [SerializeField] private Button saveColorBtn;
    [SerializeField] private Button closePaletteBtn;
    [SerializeField] private TextMeshProUGUI paletteTitleText;

    [Header("Preview Settings")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Vector3 previewScale = new Vector3(10f, 20f, 1f);

    public enum ColorCustomizationMode { None, Suit, Hair }
    private ColorCustomizationMode currentMode = ColorCustomizationMode.None;
    private Color activeSelectedColor = Color.white;
    private string activeSelectedSuitPath = "";
    private string activeSelectedHairPath = "";

    private AccessoryListSaveData accessoryData;
    private GameObject dynamicColorGrid;
    private GameObject dynamicAccessoryList;

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
        LoadAccessoryData();
        InitializeCustomizationUI();
        EnsureBackButton();
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToAssignment);
        }
        LoadAndDisplayEmployeeList();
    }

    private void EnsureBackButton()
    {
        if (backButton != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existingBack = canvas.transform.Find("BackBtn");
        if (existingBack != null)
        {
            backButton = existingBack.GetComponent<Button>();
            return;
        }

        GameObject backBtnObj = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(1f, 1f);
        backRt.anchorMax = new Vector2(1f, 1f);
        backRt.pivot = new Vector2(1f, 1f);
        backRt.anchoredPosition = new Vector2(-25, -25);
        backRt.sizeDelta = new Vector2(230, 50);

        Image backImg = backBtnObj.GetComponent<Image>();
        backImg.color = new Color(0.18f, 0.45f, 0.85f, 0.95f);

        GameObject backTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        backTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTxtRt = backTxtObj.GetComponent<RectTransform>();
        backTxtRt.anchorMin = Vector2.zero;
        backTxtRt.anchorMax = Vector2.one;
        TextMeshProUGUI backTmp = backTxtObj.GetComponent<TextMeshProUGUI>();
        backTmp.text = "← Back to Assignment";
        backTmp.fontSize = 16;
        backTmp.fontStyle = FontStyles.Bold;
        backTmp.alignment = TextAlignmentOptions.Center;
        backTmp.color = Color.white;

        backButton = backBtnObj.GetComponent<Button>();
    }

    private void LoadAccessoryData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("accessory_list");
        if (jsonAsset != null)
        {
            accessoryData = JsonUtility.FromJson<AccessoryListSaveData>(jsonAsset.text);
            Debug.Log("[EmployeeViewerManager] Loaded accessory_list.json");
        }
        else
        {
            Debug.LogWarning("[EmployeeViewerManager] Could not find accessory_list in Resources!");
            accessoryData = new AccessoryListSaveData();
        }
    }

    private void InitializeCustomizationUI()
    {
        if (suitButton != null)
        {
            suitButton.onClick.RemoveAllListeners();
            suitButton.onClick.AddListener(OpenPaletteForSuit);
        }

        if (hairButton != null)
        {
            hairButton.onClick.RemoveAllListeners();
            hairButton.onClick.AddListener(OpenPaletteForHair);
        }

        if (saveColorBtn != null)
        {
            saveColorBtn.onClick.RemoveAllListeners();
            saveColorBtn.onClick.AddListener(SaveColorChanges);
        }

        if (closePaletteBtn != null)
        {
            closePaletteBtn.onClick.RemoveAllListeners();
            closePaletteBtn.onClick.AddListener(ClosePalette);
        }

        // Hide legacy hardcoded color buttons
        if (colorRedBtn != null) colorRedBtn.gameObject.SetActive(false);
        if (colorGreenBtn != null) colorGreenBtn.gameObject.SetActive(false);
        if (colorBlackBtn != null) colorBlackBtn.gameObject.SetActive(false);

        BuildDynamicCustomizationUI();

        if (colorPalettePanel != null)
        {
            colorPalettePanel.SetActive(false);
        }
    }

    private void BuildDynamicCustomizationUI()
    {
        if (colorPalettePanel == null) return;

        // Create Color Grid
        dynamicColorGrid = new GameObject("DynamicColorGrid", typeof(RectTransform));
        dynamicColorGrid.transform.SetParent(colorPalettePanel.transform, false);
        
        GridLayoutGroup grid = dynamicColorGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(40, 40);
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.UpperCenter;
        
        RectTransform gridRt = dynamicColorGrid.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f);
        gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = new Vector2(0, 75); // Shifted higher up
        gridRt.sizeDelta = new Vector2(240, 140); // Proper grid height

        Color[] paletteColors = new Color[]
        {
            Color.white, Color.black, Color.gray, 
            new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.2f), new Color(0.2f, 0.4f, 0.9f),
            new Color(0.9f, 0.8f, 0.2f), new Color(0.8f, 0.3f, 0.8f), new Color(0.3f, 0.8f, 0.8f),
            new Color(0.6f, 0.3f, 0.1f), new Color(0.1f, 0.5f, 0.3f), new Color(0.9f, 0.5f, 0.2f)
        };

        foreach (Color c in paletteColors)
        {
            GameObject btnObj = new GameObject("ColorBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(dynamicColorGrid.transform, false);
            
            Image img = btnObj.GetComponent<Image>();
            img.color = c;
            
            Button btn = btnObj.GetComponent<Button>();
            Color capturedColor = c;
            btn.onClick.AddListener(() => OnColorSelected(capturedColor));
        }

        // Create Accessory List View
        dynamicAccessoryList = new GameObject("DynamicAccessoryList", typeof(RectTransform));
        dynamicAccessoryList.transform.SetParent(colorPalettePanel.transform, false);

        VerticalLayoutGroup vLayout = dynamicAccessoryList.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 6f;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = false;

        RectTransform listRt = dynamicAccessoryList.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.pivot = new Vector2(0.5f, 1f); // Top pivot so it grows downwards
        listRt.anchoredPosition = new Vector2(0, -10); // Positioned just below the color grid
        listRt.sizeDelta = new Vector2(240, 150);
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

        ClosePalette();
        DisplayEmployeePreview(cardUI.ItemData);
    }

    public void OpenPaletteForSuit()
    {
        if (selectedCard == null || selectedCard.ItemData == null) return;
        currentMode = ColorCustomizationMode.Suit;
        activeSelectedColor = selectedCard.ItemData.suitColor;
        activeSelectedSuitPath = selectedCard.ItemData.suitPath;

        if (paletteTitleText != null) paletteTitleText.text = "CUSTOMIZE SUIT COLOR";
        if (colorPalettePanel != null) colorPalettePanel.SetActive(true);

        if (accessoryData != null) PopulateAccessoryList(accessoryData.suits, true);
    }

    public void OpenPaletteForHair()
    {
        if (selectedCard == null || selectedCard.ItemData == null) return;
        currentMode = ColorCustomizationMode.Hair;
        activeSelectedColor = selectedCard.ItemData.hairColor;
        activeSelectedHairPath = selectedCard.ItemData.hairPath;

        if (paletteTitleText != null) paletteTitleText.text = "CUSTOMIZE HAIR COLOR";
        if (colorPalettePanel != null) colorPalettePanel.SetActive(true);

        if (accessoryData != null) PopulateAccessoryList(accessoryData.hairs, false);
    }

    public void OnColorSelected(Color color)
    {
        activeSelectedColor = color;

        // Apply color real-time to preview character model
        if (currentPreviewInstance != null)
        {
            EmployeeAppearance appearance = currentPreviewInstance.GetComponent<EmployeeAppearance>();
            if (appearance != null)
            {
                if (currentMode == ColorCustomizationMode.Suit)
                {
                    appearance.SetSuitColor(color);
                }
                else if (currentMode == ColorCustomizationMode.Hair)
                {
                    appearance.SetHairColor(color);
                }
            }
        }
    }

    private bool IsAccessoryUnlocked(AccessoryItemData item)
    {
        if (item == null) return false;
        if (item.isUnlocked) return true;

        GameSaveSystem saveSys = GameSaveSystem.Instance;
        if (saveSys == null) saveSys = FindFirstObjectByType<GameSaveSystem>();

        if (saveSys != null)
        {
            if (saveSys.IsItemPurchased(item.id)) return true;
            if (saveSys.IsItemPurchased("acc_" + item.id.ToLower())) return true;
            if (saveSys.IsItemPurchased("acc_" + item.id.Replace("_", "").ToLower())) return true;

            ShopDatabase shopDb = ShopDatabase.LoadFromResources("shop_items");
            if (shopDb != null && shopDb.items != null)
            {
                foreach (var shopItem in shopDb.items)
                {
                    if (string.Equals(shopItem.spritePath, item.spritePath, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (saveSys.IsItemPurchased(shopItem.id)) return true;
                    }
                }
            }
        }

        return false;
    }

    private void PopulateAccessoryList(List<AccessoryItemData> items, bool isSuit)
    {
        if (dynamicAccessoryList == null) return;

        // Clear existing items
        foreach (Transform child in dynamicAccessoryList.transform)
        {
            Destroy(child.gameObject);
        }

        if (items == null) return;

        foreach (var item in items)
        {
            if (!IsAccessoryUnlocked(item)) continue;

            GameObject btnObj = new GameObject("AccBtn_" + item.id, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(dynamicAccessoryList.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 25);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f);

            Button btn = btnObj.GetComponent<Button>();

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = item.name;
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            string capturedPath = item.spritePath;
            btn.onClick.AddListener(() => OnAccessorySelected(capturedPath, isSuit));
        }
    }

    private void OnAccessorySelected(string spritePath, bool isSuit)
    {
        if (currentPreviewInstance != null)
        {
            EmployeeAppearance appearance = currentPreviewInstance.GetComponent<EmployeeAppearance>();
            if (appearance != null)
            {
                if (isSuit)
                {
                    activeSelectedSuitPath = spritePath;
                    appearance.LoadBodyAndArmsFromResources(spritePath);
                }
                else
                {
                    activeSelectedHairPath = spritePath;
                    Sprite h = appearance.LoadSpriteFromPath(spritePath);
                    if (h != null) appearance.HairSprite = h;
                }
            }
        }
    }

    public void SaveColorChanges()
    {
        if (selectedCard == null || selectedCard.ItemData == null) return;

        var empData = selectedCard.ItemData;
        if (currentMode == ColorCustomizationMode.Suit)
        {
            empData.suitColor = activeSelectedColor;
            empData.suitPath = activeSelectedSuitPath;
        }
        else if (currentMode == ColorCustomizationMode.Hair)
        {
            empData.hairColor = activeSelectedColor;
            empData.hairPath = activeSelectedHairPath;
        }

        // Save inventory changes to disk / JSON
        EmployeeInventorySaveSystem invSystem = EmployeeInventorySaveSystem.Instance;
        if (invSystem == null) invSystem = FindFirstObjectByType<EmployeeInventorySaveSystem>();
        if (invSystem != null)
        {
            var invData = invSystem.CurrentData ?? invSystem.LoadInventory();
            invSystem.SaveInventory(invData);
        }

        Debug.Log($"[EmployeeViewerManager] Saved new {(currentMode == ColorCustomizationMode.Suit ? "suit" : "hair")} color for employee '{empData.employeeName}'.");

        ClosePalette();
    }

    public void ClosePalette()
    {
        currentMode = ColorCustomizationMode.None;
        if (colorPalettePanel != null)
        {
            colorPalettePanel.SetActive(false);
        }

        // Re-apply original saved colors if palette closed without saving
        if (selectedCard != null && selectedCard.ItemData != null && currentPreviewInstance != null)
        {
            EmployeeAppearance appearance = currentPreviewInstance.GetComponent<EmployeeAppearance>();
            if (appearance != null)
            {
                appearance.SetAppearanceColors(selectedCard.ItemData.suitColor, selectedCard.ItemData.hairColor);

                if (!string.IsNullOrEmpty(selectedCard.ItemData.suitPath))
                    appearance.LoadBodyAndArmsFromResources(selectedCard.ItemData.suitPath);

                if (!string.IsNullOrEmpty(selectedCard.ItemData.hairPath))
                {
                    Sprite h = appearance.LoadSpriteFromPath(selectedCard.ItemData.hairPath);
                    if (h != null) appearance.HairSprite = h;
                }
            }
        }
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

                if (!string.IsNullOrEmpty(empData.suitPath))
                    appearance.LoadBodyAndArmsFromResources(empData.suitPath);

                if (!string.IsNullOrEmpty(empData.hairPath))
                {
                    Sprite h = appearance.LoadSpriteFromPath(empData.hairPath);
                    if (h != null) appearance.HairSprite = h;
                }
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

    /// <summary>
    /// Navigates back to the Employee Assignment scene.
    /// </summary>
    public void BackToAssignment()
    {
#if UNITY_EDITOR
        string scenePath = "Assets/Scenes/EmployeeAssignment.unity";
        if (System.IO.File.Exists(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
            );
            return;
        }
#endif
        UnityEngine.SceneManagement.SceneManager.LoadScene("EmployeeAssignment");
    }
}
