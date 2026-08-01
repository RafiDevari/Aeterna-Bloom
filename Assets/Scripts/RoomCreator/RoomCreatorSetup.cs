using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
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

        // Container for Cards (Horizontal Layout)
        GameObject containerObj = new GameObject("CardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerObj.transform.SetParent(invPanelObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 0);
        containerRt.anchorMax = new Vector2(1, 1);
        containerRt.offsetMin = new Vector2(20, 10);
        containerRt.offsetMax = new Vector2(-20, -30);

        HorizontalLayoutGroup hlg = containerObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 30;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

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
        confirmRt.sizeDelta = new Vector2(360, 110);

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
        confirmHlg.spacing = 20;
        confirmHlg.childControlWidth = false;
        confirmHlg.childControlHeight = false;

        // Checklist Button (✔)
        GameObject checkBtnObj = CreateButton("ChecklistBtn", btnContainer.transform, "✔ Checklist", new Color(0.15f, 0.65f, 0.25f), new Vector2(140, 50));
        Button checklistBtn = checkBtnObj.GetComponent<Button>();

        // Cancel Button (✖)
        GameObject cancelBtnObj = CreateButton("CancelBtn", btnContainer.transform, "✖ Cancel", new Color(0.8f, 0.2f, 0.2f), new Vector2(140, 50));
        Button cancelBtn = cancelBtnObj.GetComponent<Button>();

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

        // 8. Find Prefabs from Assets/Prefabs/Rooms/
        GameObject hallPrefab = LoadRoomPrefab("Prefab_HallRoom");
        GameObject mainPrefab = LoadRoomPrefab("Prefab_MainRoom");
        GameObject botanistPrefab = LoadRoomPrefab("Prefab_DivisionBotanist");

        // 9. Assign references to RoomCreatorManager
        SetFieldValue(manager, "cardContainer", containerObj.transform);
        SetFieldValue(manager, "cardPrefab", cardPrefab);
        SetFieldValue(manager, "confirmationPanel", confirmPanelObj);
        SetFieldValue(manager, "checklistButton", checklistBtn);
        SetFieldValue(manager, "cancelButton", cancelBtn);
        SetFieldValue(manager, "statusMessageText", warnTmp);
        SetFieldValue(manager, "hallRoomPrefab", hallPrefab);
        SetFieldValue(manager, "mainHallPrefab", mainPrefab);
        SetFieldValue(manager, "botanistRoomPrefab", botanistPrefab);

        // 10. Configure inventory items (4 Hall Room, 2 Main Hall, 1 Botanist Room)
        var items = new System.Collections.Generic.List<RoomInventoryItemData>
        {
            new RoomInventoryItemData("Hall Room", 4, hallPrefab),
            new RoomInventoryItemData("Main Hall", 2, mainPrefab),
            new RoomInventoryItemData("Botanist Room", 1, botanistPrefab)
        };
        SetFieldValue(manager, "inventoryItems", items);

        manager.EnsureDefaultInventory();
        manager.RefreshInventoryUI();

        Debug.Log("[RoomCreatorSetup] Scene roomCreator berhasil dikonfigurasi dengan 3 Kotak Room dan Panel Konfirmasi!");
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

    private static void SetFieldValue(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
