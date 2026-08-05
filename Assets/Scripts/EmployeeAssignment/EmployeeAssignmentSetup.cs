using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class EmployeeAssignmentSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupEmployeeAssignmentScene();
    }

    public static void SetupEmployeeAssignmentScene()
    {
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

        // Add RoomCreatorCameraController so the user can pan around the layout if it's large!
        if (mainCam.GetComponent<RoomCreatorCameraController>() == null)
        {
            mainCam.gameObject.AddComponent<RoomCreatorCameraController>();
        }

        // 2. Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 3. Find or Create Manager
        EmployeeAssignmentManager manager = FindFirstObjectByType<EmployeeAssignmentManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("EmployeeAssignmentManager");
            manager = managerObj.AddComponent<EmployeeAssignmentManager>();
        }

        // 4. Ensure Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("EmployeeAssignmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 5. Build Bottom Inventory Panel
        Transform existingPanel = canvas.transform.Find("InventoryPanel");
        if (existingPanel != null) DestroyImmediate(existingPanel.gameObject);

        GameObject invPanelObj = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        invPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform invRt = invPanelObj.GetComponent<RectTransform>();
        invRt.anchorMin = new Vector2(0f, 0f);
        invRt.anchorMax = new Vector2(1f, 0f);
        invRt.pivot = new Vector2(0.5f, 0f);
        invRt.anchoredPosition = new Vector2(0, 20);
        invRt.sizeDelta = new Vector2(-40, 200); // 200 height for rich card UI

        Image invImg = invPanelObj.GetComponent<Image>();
        invImg.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

        // Header Title in Inventory Panel
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(invPanelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -5);
        titleRt.sizeDelta = new Vector2(0, 30);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "EMPLOYEE ASSIGNMENT SYSTEM (SELECT EMPLOYEE, THEN CLICK A DIVISION ROOM)";
        titleTmp.fontSize = 16;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.7f, 0.85f, 1f);
        titleTmp.fontStyle = FontStyles.Bold;

        // Container Scroll Rect for scrollability
        GameObject scrollObj = new GameObject("CardScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(invPanelObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(20, 10);
        scrollRt.offsetMax = new Vector2(-35, -35);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.vertical = false;
        scrollRect.horizontal = true;

        // Viewport inside Scroll Rect (using RectMask2D for clipping without needing an Image component)
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRt;

        // Card Container inside Viewport
        GameObject containerObj = new GameObject("CardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        containerObj.transform.SetParent(viewportObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 0.5f);
        containerRt.anchorMax = new Vector2(0, 0.5f);
        containerRt.pivot = new Vector2(0, 0.5f);
        containerRt.anchoredPosition = Vector2.zero;
        containerRt.sizeDelta = new Vector2(0, 140);

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

        // 6. Build Rich Employee Card Prefab
        GameObject cardPrefab = CreateEmployeeCardPrefab();
        cardPrefab.SetActive(false);

        // 7. Action Buttons (Top Right: Save, Play Test, Reset)
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
        resetRt.anchoredPosition = new Vector2(-380, -20);
        Button resetBtn = resetBtnObj.GetComponent<Button>();

        GameObject saveBtnObj = CreateButton("SaveLayoutBtn", canvas.transform, "💾 Save", new Color(0.18f, 0.45f, 0.85f), new Vector2(170, 50));
        RectTransform saveRt = saveBtnObj.GetComponent<RectTransform>();
        saveRt.anchorMin = new Vector2(1f, 1f);
        saveRt.anchorMax = new Vector2(1f, 1f);
        saveRt.pivot = new Vector2(1f, 1f);
        saveRt.anchoredPosition = new Vector2(-200, -20);
        Button saveBtn = saveBtnObj.GetComponent<Button>();

        GameObject testBtnObj = CreateButton("TestPlayBtn", canvas.transform, "▶ Play Test", new Color(0.15f, 0.65f, 0.25f), new Vector2(160, 50));
        RectTransform testRt = testBtnObj.GetComponent<RectTransform>();
        testRt.anchorMin = new Vector2(1f, 1f);
        testRt.anchorMax = new Vector2(1f, 1f);
        testRt.pivot = new Vector2(1f, 1f);
        testRt.anchoredPosition = new Vector2(-20, -20);
        Button testBtn = testBtnObj.GetComponent<Button>();

        // Status/Warning Message Text on bottom right of the canvas
        Transform existingStatus = canvas.transform.Find("StatusText");
        if (existingStatus != null) DestroyImmediate(existingStatus.gameObject);

        GameObject statusTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusTextObj.transform.SetParent(canvas.transform, false);
        RectTransform statusRt = statusTextObj.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0, 230);
        statusRt.sizeDelta = new Vector2(-40, 30);

        TextMeshProUGUI statusTmp = statusTextObj.GetComponent<TextMeshProUGUI>();
        statusTmp.text = "Click on an employee to begin.";
        statusTmp.fontSize = 16;
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.color = Color.yellow;
        statusTmp.fontStyle = FontStyles.Italic;

        // 8. Find Prefabs from Assets/Prefabs/Rooms/
        GameObject hallPrefab = LoadPrefab("Prefab_HallRoom");
        GameObject mainPrefab = LoadPrefab("Prefab_MainRoom");
        GameObject botanistPrefab = LoadPrefab("Prefab_DivisionBotanist");
        GameObject liftPrefab = LoadPrefab("Prefab_Lift");
        GameObject containmentPrefab = LoadPrefab("Prefab_ContainmentRoom");
        // 8. Find Prefabs dynamically from Assets/Prefabs/Rooms/
        System.Collections.Generic.List<GameObject> allRoomPrefabs = FindAllRoomPrefabs();

        // 9. Find Employee Prefabs
        List<GameObject> employeePrefabs = new List<GameObject>();
        string[] employeeNames = { "EmployeeBotanist", "EmployeeResearcher", "EmployeeSecurity", "EmployeeMedic", "EmployeeEngineer" };
        foreach (var name in employeeNames)
        {
            GameObject ep = LoadPrefab(name);
            if (ep != null) employeePrefabs.Add(ep);
        }

        // 10. Assign references to EmployeeAssignmentManager
        SetFieldValue(manager, "cardContainer", containerObj.transform);
        SetFieldValue(manager, "cardPrefab", cardPrefab);
        SetFieldValue(manager, "saveButton", saveBtn);
        SetFieldValue(manager, "playTestButton", testBtn);
        SetFieldValue(manager, "resetButton", resetBtn);
        SetFieldValue(manager, "statusMessageText", statusTmp);
        SetFieldValue(manager, "roomPrefabs", allRoomPrefabs);
        SetFieldValue(manager, "employeePrefabs", employeePrefabs);

        Debug.Log("[EmployeeAssignmentSetup] Employee Assignment scene successfully configured programmatically!");
    }

    private static GameObject CreateEmployeeCardPrefab()
    {
        // Card root (has Image, Button, CanvasGroup, EmployeeInventoryCardUI)
        GameObject cardObj = new GameObject("EmployeeCardPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EmployeeInventoryCardUI));
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240, 130);

        Image img = cardObj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.32f, 0.95f);

        // Border outline highlight image (shows when card is selected)
        GameObject outlineObj = new GameObject("OutlineHighlight", typeof(RectTransform), typeof(Image));
        outlineObj.transform.SetParent(cardObj.transform, false);
        RectTransform outlineRt = outlineObj.GetComponent<RectTransform>();
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.sizeDelta = new Vector2(6, 6); // Slightly bigger for outline effect

        Image outlineImg = outlineObj.GetComponent<Image>();
        outlineImg.color = new Color(0f, 0.8f, 1f, 1f); // Glowing cyan border
        outlineImg.type = Image.Type.Sliced;
        outlineImg.enabled = false; // Hidden by default
        outlineImg.raycastTarget = false;

        // Vertical Layout Group for card contents
        GameObject contentObj = new GameObject("Contents", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(cardObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(10, 10);
        contentRt.offsetMax = new Vector2(-10, -10);

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 1. Employee Name Text
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "Employee Name";
        nameTmp.fontSize = 18;
        nameTmp.color = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.raycastTarget = false;

        // 2. Specialty/Role Text
        GameObject roleObj = new GameObject("RoleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        roleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI roleTmp = roleObj.GetComponent<TextMeshProUGUI>();
        roleTmp.text = "Role";
        roleTmp.fontSize = 14;
        roleTmp.color = new Color(0.8f, 0.9f, 1f);
        roleTmp.raycastTarget = false;

        // 3. Status text
        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI statusTmp = statusObj.GetComponent<TextMeshProUGUI>();
        statusTmp.text = "Unassigned";
        statusTmp.fontSize = 12;
        statusTmp.color = Color.gray;
        statusTmp.raycastTarget = false;

        // 4. Unassign button
        GameObject unassignBtnObj = CreateButton("UnassignBtn", cardObj.transform, "Unassign", new Color(0.8f, 0.2f, 0.2f), new Vector2(90, 25));
        RectTransform unassignRt = unassignBtnObj.GetComponent<RectTransform>();
        unassignRt.anchorMin = new Vector2(1f, 1f);
        unassignRt.anchorMax = new Vector2(1f, 1f);
        unassignRt.pivot = new Vector2(1f, 1f);
        unassignRt.anchoredPosition = new Vector2(-5, -5);
        Button unassignBtn = unassignBtnObj.GetComponent<Button>();

        // Scale text inside unassign button to fit
        TextMeshProUGUI unassignTmp = unassignBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        unassignTmp.fontSize = 12;

        // Wire references to EmployeeInventoryCardUI component
        EmployeeInventoryCardUI cardUI = cardObj.GetComponent<EmployeeInventoryCardUI>();
        SetFieldValue(cardUI, "nameText", nameTmp);
        SetFieldValue(cardUI, "roleText", roleTmp);
        SetFieldValue(cardUI, "statusText", statusTmp);
        SetFieldValue(cardUI, "cardBackground", img);
        SetFieldValue(cardUI, "outlineHighlight", outlineImg);
        SetFieldValue(cardUI, "mainButton", cardObj.GetComponent<Button>());
        SetFieldValue(cardUI, "unassignButton", unassignBtn);

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

    private static GameObject LoadPrefab(string prefabName)
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
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
