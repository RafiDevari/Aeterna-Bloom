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

        // Customization Buttons (SUIT & HAIR) on left edge of detail panel
        GameObject btnContainerObj = new GameObject("CustomizationButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        btnContainerObj.transform.SetParent(rightPanelObj.transform, false);
        RectTransform btnContainerRt = btnContainerObj.GetComponent<RectTransform>();
        btnContainerRt.anchorMin = new Vector2(0f, 0.5f);
        btnContainerRt.anchorMax = new Vector2(0f, 0.5f);
        btnContainerRt.pivot = new Vector2(1f, 0.5f);
        btnContainerRt.anchoredPosition = new Vector2(-15, 0);
        btnContainerRt.sizeDelta = new Vector2(110, 130);

        VerticalLayoutGroup btnVlg = btnContainerObj.GetComponent<VerticalLayoutGroup>();
        btnVlg.spacing = 10;
        btnVlg.childAlignment = TextAnchor.MiddleCenter;
        btnVlg.childControlWidth = true;
        btnVlg.childControlHeight = true;
        btnVlg.childForceExpandWidth = true;
        btnVlg.childForceExpandHeight = true;

        // Suit Button
        GameObject suitBtnObj = new GameObject("SuitButton", typeof(RectTransform), typeof(Image), typeof(Button));
        suitBtnObj.transform.SetParent(btnContainerObj.transform, false);
        Image suitImg = suitBtnObj.GetComponent<Image>();
        suitImg.color = new Color(0.15f, 0.35f, 0.55f, 0.95f);
        Button suitBtn = suitBtnObj.GetComponent<Button>();

        GameObject suitTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        suitTextObj.transform.SetParent(suitBtnObj.transform, false);
        RectTransform suitTextRt = suitTextObj.GetComponent<RectTransform>();
        suitTextRt.anchorMin = Vector2.zero;
        suitTextRt.anchorMax = Vector2.one;
        TextMeshProUGUI suitTmp = suitTextObj.GetComponent<TextMeshProUGUI>();
        suitTmp.text = "SUIT";
        suitTmp.fontSize = 16;
        suitTmp.fontStyle = FontStyles.Bold;
        suitTmp.alignment = TextAlignmentOptions.Center;
        suitTmp.color = Color.white;

        // Hair Button
        GameObject hairBtnObj = new GameObject("HairButton", typeof(RectTransform), typeof(Image), typeof(Button));
        hairBtnObj.transform.SetParent(btnContainerObj.transform, false);
        Image hairImg = hairBtnObj.GetComponent<Image>();
        hairImg.color = new Color(0.45f, 0.2f, 0.5f, 0.95f);
        Button hairBtn = hairBtnObj.GetComponent<Button>();

        GameObject hairTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        hairTextObj.transform.SetParent(hairBtnObj.transform, false);
        RectTransform hairTextRt = hairTextObj.GetComponent<RectTransform>();
        hairTextRt.anchorMin = Vector2.zero;
        hairTextRt.anchorMax = Vector2.one;
        TextMeshProUGUI hairTmp = hairTextObj.GetComponent<TextMeshProUGUI>();
        hairTmp.text = "HAIR";
        hairTmp.fontSize = 16;
        hairTmp.fontStyle = FontStyles.Bold;
        hairTmp.alignment = TextAlignmentOptions.Center;
        hairTmp.color = Color.white;

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

        // 8. Build Middle Field Color Palette Panel
        Transform existingPalette = canvas.transform.Find("ColorPalettePanel");
        if (existingPalette != null) DestroyImmediate(existingPalette.gameObject);

        GameObject paletteObj = new GameObject("ColorPalettePanel", typeof(RectTransform), typeof(Image));
        paletteObj.transform.SetParent(canvas.transform, false);
        RectTransform paletteRt = paletteObj.GetComponent<RectTransform>();
        paletteRt.anchorMin = new Vector2(0.33f, 0.30f);
        paletteRt.anchorMax = new Vector2(0.63f, 0.70f);
        paletteRt.pivot = new Vector2(0.5f, 0.5f);
        paletteRt.anchoredPosition = Vector2.zero;

        Image paletteImg = paletteObj.GetComponent<Image>();
        paletteImg.color = new Color(0.09f, 0.11f, 0.17f, 0.96f);

        // Palette Title Text
        GameObject paletteTitleObj = new GameObject("PaletteTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        paletteTitleObj.transform.SetParent(paletteObj.transform, false);
        RectTransform paletteTitleRt = paletteTitleObj.GetComponent<RectTransform>();
        paletteTitleRt.anchorMin = new Vector2(0, 1);
        paletteTitleRt.anchorMax = new Vector2(1, 1);
        paletteTitleRt.pivot = new Vector2(0.5f, 1);
        paletteTitleRt.anchoredPosition = new Vector2(0, -15);
        paletteTitleRt.sizeDelta = new Vector2(-20, 35);

        TextMeshProUGUI paletteTitleTmp = paletteTitleObj.GetComponent<TextMeshProUGUI>();
        paletteTitleTmp.text = "CUSTOMIZE COLOR";
        paletteTitleTmp.fontSize = 20;
        paletteTitleTmp.alignment = TextAlignmentOptions.Center;
        paletteTitleTmp.color = new Color(0.4f, 0.85f, 1f);
        paletteTitleTmp.fontStyle = FontStyles.Bold;

        // Palette Close Button (X)
        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(paletteObj.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.anchoredPosition = new Vector2(-10, -10);
        closeRt.sizeDelta = new Vector2(30, 30);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.6f, 0.15f, 0.15f, 0.9f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeTextRt = closeTextObj.GetComponent<RectTransform>();
        closeTextRt.anchorMin = Vector2.zero;
        closeTextRt.anchorMax = Vector2.one;
        TextMeshProUGUI closeTmp = closeTextObj.GetComponent<TextMeshProUGUI>();
        closeTmp.text = "X";
        closeTmp.fontSize = 16;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;

        // Swatches Horizontal Container
        GameObject swatchContainerObj = new GameObject("SwatchContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        swatchContainerObj.transform.SetParent(paletteObj.transform, false);
        RectTransform swatchContainerRt = swatchContainerObj.GetComponent<RectTransform>();
        swatchContainerRt.anchorMin = new Vector2(0.05f, 0.35f);
        swatchContainerRt.anchorMax = new Vector2(0.95f, 0.70f);
        swatchContainerRt.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup swatchHlg = swatchContainerObj.GetComponent<HorizontalLayoutGroup>();
        swatchHlg.spacing = 20;
        swatchHlg.childAlignment = TextAnchor.MiddleCenter;
        swatchHlg.childControlWidth = false;
        swatchHlg.childControlHeight = false;

        // Red Swatch Button
        Button redBtn = CreateColorSwatchButton(swatchContainerObj.transform, "RedBtn", "Merah", new Color(0.9f, 0.2f, 0.2f, 1f));
        // Green Swatch Button
        Button greenBtn = CreateColorSwatchButton(swatchContainerObj.transform, "GreenBtn", "Hijau", new Color(0.2f, 0.8f, 0.2f, 1f));
        // Black Swatch Button
        Button blackBtn = CreateColorSwatchButton(swatchContainerObj.transform, "BlackBtn", "Hitam", new Color(0.15f, 0.15f, 0.15f, 1f));

        // Save Color Button at Bottom of Palette
        GameObject saveBtnObj = new GameObject("SaveColorButton", typeof(RectTransform), typeof(Image), typeof(Button));
        saveBtnObj.transform.SetParent(paletteObj.transform, false);
        RectTransform saveRt = saveBtnObj.GetComponent<RectTransform>();
        saveRt.anchorMin = new Vector2(0.5f, 0.08f);
        saveRt.anchorMax = new Vector2(0.5f, 0.08f);
        saveRt.pivot = new Vector2(0.5f, 0f);
        saveRt.sizeDelta = new Vector2(200, 45);

        Image saveImg = saveBtnObj.GetComponent<Image>();
        saveImg.color = new Color(0.15f, 0.6f, 0.35f, 0.95f);
        Button saveBtn = saveBtnObj.GetComponent<Button>();

        GameObject saveTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        saveTextObj.transform.SetParent(saveBtnObj.transform, false);
        RectTransform saveTextRt = saveTextObj.GetComponent<RectTransform>();
        saveTextRt.anchorMin = Vector2.zero;
        saveTextRt.anchorMax = Vector2.one;
        TextMeshProUGUI saveTmp = saveTextObj.GetComponent<TextMeshProUGUI>();
        saveTmp.text = "SAVE";
        saveTmp.fontSize = 18;
        saveTmp.fontStyle = FontStyles.Bold;
        saveTmp.alignment = TextAlignmentOptions.Center;
        saveTmp.color = Color.white;

        paletteObj.SetActive(false);

        // 9. Create World Preview Anchor inside the Right Detail Panel
        GameObject previewAnchorObj = GameObject.Find("PreviewAnchor");
        if (previewAnchorObj == null)
        {
            previewAnchorObj = new GameObject("PreviewAnchor");
        }
        previewAnchorObj.transform.position = new Vector3(6.4f, -0.2f, 0f);

        // 10. Add Back Button (Top Right of Canvas)
        Transform existingBack = canvas.transform.Find("BackBtn");
        if (existingBack != null) DestroyImmediate(existingBack.gameObject);

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
        Button backBtn = backBtnObj.GetComponent<Button>();

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

        // 11. Load Prefabs and Assign References
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
        SetFieldValue(manager, "backButton", backBtn);

        SetFieldValue(manager, "suitButton", suitBtn);
        SetFieldValue(manager, "hairButton", hairBtn);
        SetFieldValue(manager, "colorPalettePanel", paletteObj);
        SetFieldValue(manager, "colorRedBtn", redBtn);
        SetFieldValue(manager, "colorGreenBtn", greenBtn);
        SetFieldValue(manager, "colorBlackBtn", blackBtn);
        SetFieldValue(manager, "saveColorBtn", saveBtn);
        SetFieldValue(manager, "closePaletteBtn", closeBtn);
        SetFieldValue(manager, "paletteTitleText", paletteTitleTmp);

        Debug.Log("[EmployeeViewerSetup] Employee Viewer scene successfully configured programmatically!");
    }

    private static Button CreateColorSwatchButton(Transform parent, string name, string label, Color color)
    {
        GameObject swatchObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        swatchObj.transform.SetParent(parent, false);
        RectTransform swatchRt = swatchObj.GetComponent<RectTransform>();
        swatchRt.sizeDelta = new Vector2(75, 75);

        Image swatchImg = swatchObj.GetComponent<Image>();
        swatchImg.color = color;
        Button button = swatchObj.GetComponent<Button>();

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(swatchObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = (color.r + color.g + color.b < 1.2f) ? Color.white : Color.black;

        return button;
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
