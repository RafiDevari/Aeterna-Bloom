using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Inisialisasi otomatis scene roomCreator.
/// Membuat Canvas, UI Bar (3 Kotak Room: Hall Room (4), Main Hall (2), Botanist Room (1)),
/// Panel Konfirmasi (Checklist & Cancel), serta menghubungkan Prefab Room dari project.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class RoomCreatorSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupRoomCreatorScene();
    }

    public static void SetupRoomCreatorScene()
    {
#if UNITY_EDITOR
        EnsureBuildSettingsInEditor();
#endif
        // 1. Ensure Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6f;
            camObj.transform.position = new Vector3(0, 0, -10);
            mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        }
        else
        {
            mainCam.orthographic = true;
            mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        }

        if (mainCam.GetComponent<RoomCreatorCameraController>() == null)
        {
            mainCam.gameObject.AddComponent<RoomCreatorCameraController>();
        }

        // 2. Ensure EventSystem
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 3. Find or Create Manager
        RoomCreatorManager manager = FindFirstObjectByType<RoomCreatorManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("RoomCreatorManager");
            manager = managerObj.AddComponent<RoomCreatorManager>();
        }

        // 4. Ensure Canvas UI
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RoomCreatorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 5. Build Inventory Panel (Bottom Bar with 3 Room Boxes)
        Transform existingPanel = canvas.transform.Find("InventoryPanel");
        if (existingPanel != null) DestroyImmediate(existingPanel.gameObject);

        GameObject invPanelObj = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        invPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform invRt = invPanelObj.GetComponent<RectTransform>();
        invRt.anchorMin = new Vector2(0f, 0f);
        invRt.anchorMax = new Vector2(1f, 0f);
        invRt.pivot = new Vector2(0.5f, 0f);
        invRt.anchoredPosition = new Vector2(0, 20);
        invRt.sizeDelta = new Vector2(-40, 120);

        Image invImg = invPanelObj.GetComponent<Image>();
        invImg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        // Header Title in Inventory Panel
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(invPanelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -5);
        titleRt.sizeDelta = new Vector2(0, 25);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "INVENTARIS ROOM (DRAG KOTAK UNTUK MERAKIT LAYOUT)";
        titleTmp.fontSize = 16;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.7f, 0.85f, 1f);

        // Container Scroll Rect for scrollability when there are many rooms
        GameObject scrollObj = new GameObject("CardScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(invPanelObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(20, 10);
        scrollRt.offsetMax = new Vector2(-20, -35);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.vertical = false;
        scrollRect.horizontal = true;

        // Viewport inside Scroll Rect (using RectMask2D for clipping)
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRt;

        // Container for Cards (Horizontal Layout + ContentSizeFitter)
        GameObject containerObj = new GameObject("CardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        containerObj.transform.SetParent(viewportObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 0.5f);
        containerRt.anchorMax = new Vector2(0, 0.5f);
        containerRt.pivot = new Vector2(0, 0.5f);
        containerRt.anchoredPosition = Vector2.zero;
        containerRt.sizeDelta = new Vector2(0, 80);

        HorizontalLayoutGroup hlg = containerObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 20;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = containerObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = containerRt;

        // 6. Build Card Prefab
        GameObject cardPrefab = CreateCardPrefab();

        // 7. Build Confirmation Panel (Checklist ✔ and Cancel ✖ Buttons)
        Transform existingConfirm = canvas.transform.Find("ConfirmationPanel");
        if (existingConfirm != null) DestroyImmediate(existingConfirm.gameObject);

        GameObject confirmPanelObj = new GameObject("ConfirmationPanel", typeof(RectTransform), typeof(Image));
        confirmPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform confirmRt = confirmPanelObj.GetComponent<RectTransform>();
        confirmRt.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRt.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRt.pivot = new Vector2(0.5f, 0.5f);
        confirmRt.anchoredPosition = new Vector2(0, -180);
        confirmRt.sizeDelta = new Vector2(510, 110);

        Image confirmImg = confirmPanelObj.GetComponent<Image>();
        confirmImg.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);

        // Button Container
        GameObject btnContainer = new GameObject("BtnContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnContainer.transform.SetParent(confirmPanelObj.transform, false);
        RectTransform btnContainerRt = btnContainer.GetComponent<RectTransform>();
        btnContainerRt.anchorMin = new Vector2(0, 0.35f);
        btnContainerRt.anchorMax = new Vector2(1, 1f);
        btnContainerRt.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup confirmHlg = btnContainer.GetComponent<HorizontalLayoutGroup>();
        confirmHlg.childAlignment = TextAnchor.MiddleCenter;
        confirmHlg.spacing = 15;
        confirmHlg.childControlWidth = false;
        confirmHlg.childControlHeight = false;

        // Checklist Button (✔)
        GameObject checkBtnObj = CreateButton("ChecklistBtn", btnContainer.transform, "✔ Checklist", new Color(0.15f, 0.65f, 0.25f), new Vector2(130, 48));
        Button checklistBtn = checkBtnObj.GetComponent<Button>();

        // Cancel Button (✖)
        GameObject cancelBtnObj = CreateButton("CancelBtn", btnContainer.transform, "✖ Cancel", new Color(0.8f, 0.2f, 0.2f), new Vector2(130, 48));
        Button cancelBtn = cancelBtnObj.GetComponent<Button>();

        // Delete Room Button (🗑️)
        GameObject deleteBtnObj = CreateButton("DeleteBtn", btnContainer.transform, "🗑️ Hapus Room", new Color(0.9f, 0.45f, 0.1f), new Vector2(140, 48));
        Button deleteBtn = deleteBtnObj.GetComponent<Button>();

        // Warning Text in Confirmation Panel
        GameObject warnTextObj = new GameObject("WarningText", typeof(RectTransform), typeof(TextMeshProUGUI));
        warnTextObj.transform.SetParent(confirmPanelObj.transform, false);
        RectTransform warnRt = warnTextObj.GetComponent<RectTransform>();
        warnRt.anchorMin = new Vector2(0, 0);
        warnRt.anchorMax = new Vector2(1, 0.35f);
        warnRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI warnTmp = warnTextObj.GetComponent<TextMeshProUGUI>();
        warnTmp.text = "";
        warnTmp.fontSize = 14;
        warnTmp.alignment = TextAlignmentOptions.Center;
        warnTmp.color = new Color(1f, 0.4f, 0.4f);

        // 8. Build Top Right Action Buttons (Reset & Start/Mulai)
        Transform existingReset = canvas.transform.Find("ResetBtn");
        if (existingReset != null) DestroyImmediate(existingReset.gameObject);

        Transform existingSave = canvas.transform.Find("SaveLayoutBtn");
        if (existingSave != null) DestroyImmediate(existingSave.gameObject);

        Transform existingTest = canvas.transform.Find("TestPlayBtn");
        if (existingTest != null) DestroyImmediate(existingTest.gameObject);

        GameObject resetBtnObj = CreateButton("ResetBtn", canvas.transform, "🔄 Reset", new Color(0.85f, 0.35f, 0.15f), new Vector2(130, 50));
        RectTransform resetRt = resetBtnObj.GetComponent<RectTransform>();
        resetRt.anchorMin = new Vector2(1f, 1f);
        resetRt.anchorMax = new Vector2(1f, 1f);
        resetRt.pivot = new Vector2(1f, 1f);
        resetRt.anchoredPosition = new Vector2(-200, -20);
        Button resetBtn = resetBtnObj.GetComponent<Button>();

        GameObject testBtnObj = CreateButton("TestPlayBtn", canvas.transform, "▶ Mulai", new Color(0.15f, 0.65f, 0.25f), new Vector2(160, 50));
        RectTransform testRt = testBtnObj.GetComponent<RectTransform>();
        testRt.anchorMin = new Vector2(1f, 1f);
        testRt.anchorMax = new Vector2(1f, 1f);
        testRt.pivot = new Vector2(1f, 1f);
        testRt.anchoredPosition = new Vector2(-20, -20);
        Button testBtn = testBtnObj.GetComponent<Button>();

        // Global Warning/Notification Text for Room Creator (Top Center)
        Transform existingGlobalStatus = canvas.transform.Find("GlobalStatusText");
        if (existingGlobalStatus != null) DestroyImmediate(existingGlobalStatus.gameObject);

        GameObject globalStatusObj = new GameObject("GlobalStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        globalStatusObj.transform.SetParent(canvas.transform, false);
        RectTransform gsRt = globalStatusObj.GetComponent<RectTransform>();
        gsRt.anchorMin = new Vector2(0.5f, 1f);
        gsRt.anchorMax = new Vector2(0.5f, 1f);
        gsRt.pivot = new Vector2(0.5f, 1f);
        gsRt.anchoredPosition = new Vector2(0, -25);
        gsRt.sizeDelta = new Vector2(1000, 45);

        TextMeshProUGUI gsTmp = globalStatusObj.GetComponent<TextMeshProUGUI>();
        gsTmp.text = "";
        gsTmp.fontSize = 18;
        gsTmp.alignment = TextAlignmentOptions.Center;
        gsTmp.color = new Color(1f, 0.35f, 0.35f);
        gsTmp.fontStyle = FontStyles.Bold;

        // 9. Find Prefabs dynamically from Assets/Prefabs/Rooms/
        System.Collections.Generic.List<GameObject> allRoomPrefabs = FindAllRoomPrefabs();

        // 10. Assign references to RoomCreatorManager
        SetFieldValue(manager, "cardContainer", containerObj.transform);
        SetFieldValue(manager, "cardPrefab", cardPrefab);
        SetFieldValue(manager, "confirmationPanel", confirmPanelObj);
        SetFieldValue(manager, "checklistButton", checklistBtn);
        SetFieldValue(manager, "cancelButton", cancelBtn);
        SetFieldValue(manager, "deleteButton", deleteBtn);
        SetFieldValue(manager, "saveButton", null);
        SetFieldValue(manager, "testPlayButton", testBtn);
        SetFieldValue(manager, "resetButton", resetBtn);
        SetFieldValue(manager, "statusMessageText", warnTmp);
        SetFieldValue(manager, "globalStatusText", gsTmp);
        SetFieldValue(manager, "roomPrefabs", allRoomPrefabs);

        manager.EnsureDefaultInventory();
        manager.RefreshInventoryUI();

        // Initialize/Refresh plant inventory panel on the left
        RefreshLeftContainmentPanel(null);

        Debug.Log("[RoomCreatorSetup] Scene roomCreator berhasil dikonfigurasi dengan Kotak Room dan Panel Konfirmasi!");
    }

    private static GameObject CreateCardPrefab()
    {
        GameObject cardObj = new GameObject("RoomCardPrefab", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(RoomInventoryCardUI));
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 65);

        Image img = cardObj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.32f, 0.95f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(cardObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "Room (0)";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return cardObj;
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Color btnColor, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = btnColor;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return btnObj;
    }

    private static GameObject LoadRoomPrefab(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>($"Rooms/{prefabName}");
        if (prefab != null) return prefab;

        prefab = Resources.Load<GameObject>(prefabName);
        if (prefab != null) return prefab;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif
        return null;
    }

    private static System.Collections.Generic.List<GameObject> FindAllRoomPrefabs()
    {
        var prefabs = new System.Collections.Generic.List<GameObject>();
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<Room>() != null)
            {
                prefabs.Add(prefab);
            }
        }
#endif
        return prefabs;
    }

    private static void SetFieldValue(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

#if UNITY_EDITOR
    private static void EnsureBuildSettingsInEditor()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool modified = false;

        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/RoomCreator.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/EmployeeAssignment.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Gameplay1.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Tutorial.unity");

        if (modified)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[RoomCreatorSetup] Automatically registered required scenes to Build Settings.");
        }
    }

    private static bool AddBuildSceneIfMissing(System.Collections.Generic.List<EditorBuildSettingsScene> list, string path)
    {
        if (!System.IO.File.Exists(path)) return false;
        foreach (var s in list)
        {
            if (s.path == path) return false;
        }
        list.Add(new EditorBuildSettingsScene(path, true));
        return true;
    }
#endif

    public static void RefreshLeftContainmentPanel(HashSet<string> placedPlantInstanceIds)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Find or create LeftContainmentPanel
        Transform existingPanel = canvas.transform.Find("LeftContainmentPanel");
        if (existingPanel != null) DestroyImmediate(existingPanel.gameObject);

        // Load Plant Inventory JSON
        string jsonText = "";
#if UNITY_EDITOR
        string editorPath = Path.Combine(Application.dataPath, "Resources", "plant_inventory.json");
        if (File.Exists(editorPath))
        {
            jsonText = File.ReadAllText(editorPath);
        }
#endif
        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, "plant_inventory.json");
            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
            }
            else
            {
                TextAsset asset = Resources.Load<TextAsset>("plant_inventory");
                if (asset != null)
                {
                    jsonText = asset.text;
                }
            }
        }

        if (string.IsNullOrEmpty(jsonText)) return;

        PlantInventoryData inventoryData = null;
        try
        {
            inventoryData = JsonUtility.FromJson<PlantInventoryData>(jsonText);
        }
        catch (System.Exception) {}

        if (inventoryData == null || inventoryData.plants == null || inventoryData.plants.Count == 0) return;

        // Build Left Panel UI (Glassmorphic dark design)
        GameObject leftPanel = new GameObject("LeftContainmentPanel", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = leftPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0.5f);
        panelRt.anchorMax = new Vector2(0f, 0.5f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.anchoredPosition = new Vector2(20, 0);
        panelRt.sizeDelta = new Vector2(260, 600);

        Image panelImg = leftPanel.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

        // Panel Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(leftPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -15);
        titleRt.sizeDelta = new Vector2(0, 30);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "PLANT INVENTORY";
        titleTmp.fontSize = 18;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.7f, 0.85f, 1f);
        titleTmp.fontStyle = FontStyles.Bold;

        // Scroll Container for Left Panel
        GameObject scrollObj = new GameObject("PlantScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(leftPanel.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(15, 15);
        scrollRt.offsetMax = new Vector2(-15, -55);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRt;

        // Vertical List Container inside Viewport
        GameObject listContainer = new GameObject("ListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listContainer.transform.SetParent(viewportObj.transform, false);
        RectTransform listRt = listContainer.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 1f);
        listRt.anchorMax = new Vector2(0.5f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.anchoredPosition = Vector2.zero;
        listRt.sizeDelta = new Vector2(230, 0);

        VerticalLayoutGroup vlg = listContainer.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 15;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = listContainer.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = listRt;

        // Spawn Card UI for each plant
        RoomCreatorManager manager = FindFirstObjectByType<RoomCreatorManager>();

        foreach (var plant in inventoryData.plants)
        {
            if (plant == null || string.IsNullOrEmpty(plant.plantId)) continue;

            bool isPlaced = placedPlantInstanceIds != null && placedPlantInstanceIds.Contains(plant.plantInstanceId);

            GameObject cardObj = new GameObject($"PlantCard_{plant.plantInstanceId}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObj.transform.SetParent(listContainer.transform, false);
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(230, 80);

            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = isPlaced ? new Color(0.12f, 0.14f, 0.18f, 0.5f) : new Color(0.18f, 0.22f, 0.32f, 0.95f);

            // Plant Name text
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(10, 0);
            nameRt.offsetMax = new Vector2(-10, -5);

            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.text = plant.plantId;
            nameTmp.fontSize = 18;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = isPlaced ? Color.gray : Color.white;
            nameTmp.fontStyle = FontStyles.Bold;

            // Plant Instance ID / Status text
            GameObject subObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(cardObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0, 0);
            subRt.anchorMax = new Vector2(1, 0.5f);
            subRt.offsetMin = new Vector2(10, 5);
            subRt.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = isPlaced ? $"Status: PLACED ({plant.growth * 100f:F0}%)" : $"ID: {plant.plantInstanceId} ({plant.growth * 100f:F0}%)";
            subTmp.fontSize = 14;
            subTmp.alignment = TextAlignmentOptions.Left;
            subTmp.color = isPlaced ? new Color(0.5f, 0.8f, 0.5f) : new Color(0.7f, 0.8f, 0.9f);

            // Add Drag Script
            if (!isPlaced)
            {
                PlantInventoryCardUI cardUI = cardObj.AddComponent<PlantInventoryCardUI>();
                cardUI.Setup(plant.plantInstanceId, plant.plantId, manager);
            }
            else
            {
                CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0.5f;
                    cg.blocksRaycasts = false;
                }
            }
        }
    }
}

[System.Serializable]
public class PlantInventoryItemData
{
    public string plantInstanceId;
    public string plantId;
    public float growth;
    public List<string> completedResearchIds;
}

[System.Serializable]
public class PlantInventoryData
{
    public List<PlantInventoryItemData> plants;
}

public class PlantInventoryCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private string plantInstanceId;
    private string plantId;
    private RoomCreatorManager manager;
    private ScrollRect parentScrollRect;
    private bool isDraggingSelf = false;

    public void Setup(string instanceId, string id, RoomCreatorManager managerRef)
    {
        plantInstanceId = instanceId;
        plantId = id;
        manager = managerRef;
        if (parentScrollRect == null)
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }
    }

    public void OnPointerDown(PointerEventData eventData) {}

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null && Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            isDraggingSelf = false;
            parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        isDraggingSelf = true;
        if (manager != null)
        {
            manager.StartDraggingContainmentUnit(plantInstanceId, plantId, eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingSelf)
        {
            if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
        }
        else if (manager != null)
        {
            manager.UpdateDraggingRoom(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingSelf)
        {
            if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
        }
        else if (manager != null)
        {
            manager.DropDraggingRoom(eventData);
        }
    }
}
