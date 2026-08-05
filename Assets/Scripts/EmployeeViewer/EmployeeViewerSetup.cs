using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class EmployeeViewerSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupEmployeeViewerScene();
    }

    public static void SetupEmployeeViewerScene()
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
            mainCam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
        }
        else
        {
            mainCam.orthographic = true;
            mainCam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
        }

        // 2. Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 3. Find or Create Manager
        EmployeeViewerManager manager = FindFirstObjectByType<EmployeeViewerManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("EmployeeViewerManager");
            manager = managerObj.AddComponent<EmployeeViewerManager>();
        }

        // 4. Ensure Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("EmployeeViewerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 100f;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 5. Build Left Employee List Panel
        Transform existingLeftPanel = canvas.transform.Find("EmployeeListPanel");
        if (existingLeftPanel != null) DestroyImmediate(existingLeftPanel.gameObject);

        GameObject leftPanelObj = new GameObject("EmployeeListPanel", typeof(RectTransform), typeof(Image));
        leftPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform leftRt = leftPanelObj.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0.28f, 1f);
        leftRt.pivot = new Vector2(0f, 0.5f);
        leftRt.offsetMin = new Vector2(20, 20);
        leftRt.offsetMax = new Vector2(0, -20);

        Image leftImg = leftPanelObj.GetComponent<Image>();
        leftImg.color = new Color(0.07f, 0.09f, 0.13f, 0.95f);

        // Header Title in Left Panel
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(leftPanelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -15);
        titleRt.sizeDelta = new Vector2(-20, 40);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "EMPLOYEE DIRECTORY";
        titleTmp.fontSize = 22;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.8f, 0.9f, 1f);
        titleTmp.fontStyle = FontStyles.Bold;

        // Container Scroll Rect for vertical employee list
        GameObject scrollObj = new GameObject("ListScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(leftPanelObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(15, 15);
        scrollRt.offsetMax = new Vector2(-15, -65);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        // Viewport inside Scroll Rect
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRt;

        // Card Container inside Viewport
        GameObject containerObj = new GameObject("CardContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        containerObj.transform.SetParent(viewportObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 1);
        containerRt.anchorMax = new Vector2(1, 1);
        containerRt.pivot = new Vector2(0f, 1f);
        containerRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = containerObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 12;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = containerObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = containerRt;

        // 6. Build Card UI Prefab Template
        GameObject cardPrefab = CreateEmployeeCardPrefab();

        // 7. Build Right Info & Preview Panel UI
        Transform existingRightPanel = canvas.transform.Find("EmployeeDetailPanel");
        if (existingRightPanel != null) DestroyImmediate(existingRightPanel.gameObject);

        GameObject rightPanelObj = new GameObject("EmployeeDetailPanel", typeof(RectTransform), typeof(Image));
        rightPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform rightRt = rightPanelObj.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.68f, 0.15f);
        rightRt.anchorMax = new Vector2(0.96f, 0.85f);
        rightRt.pivot = new Vector2(0.5f, 0.5f);
        rightRt.anchoredPosition = Vector2.zero;

        Image rightImg = rightPanelObj.GetComponent<Image>();
        rightImg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);

        // Employee Name Text Header
        GameObject nameObj = new GameObject("SelectedNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(rightPanelObj.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -20);
        nameRt.sizeDelta = new Vector2(-40, 45);

        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "EMPLOYEE NAME";
        nameTmp.fontSize = 26;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;

        // Employee Role Text
        GameObject roleObj = new GameObject("SelectedRoleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        roleObj.transform.SetParent(rightPanelObj.transform, false);
        RectTransform roleRt = roleObj.GetComponent<RectTransform>();
        roleRt.anchorMin = new Vector2(0, 1);
        roleRt.anchorMax = new Vector2(1, 1);
        roleRt.pivot = new Vector2(0.5f, 1);
        roleRt.anchoredPosition = new Vector2(0, -70);
        roleRt.sizeDelta = new Vector2(-40, 35);

        TextMeshProUGUI roleTmp = roleObj.GetComponent<TextMeshProUGUI>();
        roleTmp.text = "Role: Specialist";
        roleTmp.fontSize = 18;
        roleTmp.alignment = TextAlignmentOptions.Center;
        roleTmp.color = new Color(0.4f, 0.8f, 1f);
        roleTmp.fontStyle = FontStyles.Italic;

        // Employee Details Text (Positioned at bottom of right panel)
        GameObject detailsObj = new GameObject("SelectedDetailsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        detailsObj.transform.SetParent(rightPanelObj.transform, false);
        RectTransform detailsRt = detailsObj.GetComponent<RectTransform>();
        detailsRt.anchorMin = new Vector2(0, 0);
        detailsRt.anchorMax = new Vector2(1, 0.35f);
        detailsRt.offsetMin = new Vector2(25, 25);
        detailsRt.offsetMax = new Vector2(-25, 0);

        TextMeshProUGUI detailsTmp = detailsObj.GetComponent<TextMeshProUGUI>();
        detailsTmp.text = "Select an employee from the left panel to inspect details and appearance.";
        detailsTmp.fontSize = 16;
        detailsTmp.alignment = TextAlignmentOptions.TopLeft;
        detailsTmp.color = new Color(0.85f, 0.88f, 0.95f);

        // 8. Create World Preview Anchor inside the Right Detail Panel
        GameObject previewAnchorObj = GameObject.Find("PreviewAnchor");
        if (previewAnchorObj == null)
        {
            previewAnchorObj = new GameObject("PreviewAnchor");
        }
        previewAnchorObj.transform.position = new Vector3(6.4f, -0.2f, 0f);

        // 9. Load Prefabs and Assign References
        List<GameObject> employeePrefabs = new List<GameObject>();
#if UNITY_EDITOR
        string[] employeeNames = { "EmployeeBotanist", "EmployeeResearcher", "EmployeeSecurity", "EmployeeMedic", "EmployeeEngineer" };
        foreach (string empName in employeeNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{empName} t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject ep = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (ep != null) employeePrefabs.Add(ep);
            }
        }
#endif

        SetFieldValue(manager, "cardContainer", containerRt);
        SetFieldValue(manager, "cardPrefab", cardPrefab);
        SetFieldValue(manager, "selectedNameText", nameTmp);
        SetFieldValue(manager, "selectedRoleText", roleTmp);
        SetFieldValue(manager, "selectedDetailsText", detailsTmp);
        SetFieldValue(manager, "previewAnchor", previewAnchorObj.transform);
        SetFieldValue(manager, "employeePrefabs", employeePrefabs);

        Debug.Log("[EmployeeViewerSetup] Employee Viewer scene successfully configured programmatically!");
    }

    private static GameObject CreateEmployeeCardPrefab()
    {
        Transform existingTemp = GameObject.Find("Canvas")?.transform.Find("EmployeeListCardPrefab_Temp");
        if (existingTemp != null) DestroyImmediate(existingTemp.gameObject);

        GameObject card = new GameObject("EmployeeListCardPrefab_Temp", typeof(RectTransform), typeof(Image), typeof(Button));
        card.SetActive(false);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(0, 80);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 80;
        le.minHeight = 80;

        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);

        // Highlight Border Image
        GameObject outlineObj = new GameObject("OutlineHighlight", typeof(RectTransform), typeof(Image));
        outlineObj.transform.SetParent(card.transform, false);
        RectTransform outlineRt = outlineObj.GetComponent<RectTransform>();
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.sizeDelta = new Vector2(6, 6);

        Image outlineImg = outlineObj.GetComponent<Image>();
        outlineImg.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        outlineImg.enabled = false;
        outlineImg.raycastTarget = false;

        // Contents container with VerticalLayoutGroup
        GameObject contentObj = new GameObject("Contents", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(card.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(20, 10);
        contentRt.offsetMax = new Vector2(-20, -10);

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Name Text
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "Employee Name";
        nameTmp.fontSize = 20;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.TopLeft;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin = 12;
        nameTmp.fontSizeMax = 22;
        nameTmp.enableWordWrapping = false;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.raycastTarget = false;

        // Role Text
        GameObject roleObj = new GameObject("RoleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        roleObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI roleTmp = roleObj.GetComponent<TextMeshProUGUI>();
        roleTmp.text = "Role";
        roleTmp.fontSize = 15;
        roleTmp.color = new Color(0.8f, 0.9f, 1f);
        roleTmp.alignment = TextAlignmentOptions.TopLeft;
        roleTmp.enableAutoSizing = true;
        roleTmp.fontSizeMin = 10;
        roleTmp.fontSizeMax = 16;
        roleTmp.enableWordWrapping = false;
        roleTmp.overflowMode = TextOverflowModes.Ellipsis;
        roleTmp.raycastTarget = false;

        // Component mapping
        EmployeeListCardUI cardUI = card.AddComponent<EmployeeListCardUI>();
        SetFieldValue(cardUI, "nameText", nameTmp);
        SetFieldValue(cardUI, "roleText", roleTmp);
        SetFieldValue(cardUI, "cardBackground", cardImg);
        SetFieldValue(cardUI, "outlineHighlight", outlineImg);
        SetFieldValue(cardUI, "mainButton", card.GetComponent<Button>());

        return card;
    }

    private static void SetFieldValue(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            var prop = obj.GetType().GetProperty(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }
    }
}
