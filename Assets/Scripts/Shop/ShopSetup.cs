using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ShopSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupShopScene();
    }

    public static void SetupShopScene()
    {
        // 1. Setup Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            mainCam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
        }
        else
        {
            mainCam.orthographic = true;
            mainCam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
        }

        // 2. Setup EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 3. Setup Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Remove old UI root if existing to rebuild cleanly
        Transform oldRoot = canvas.transform.Find("ShopUIRoot");
        if (oldRoot != null)
        {
            DestroyImmediate(oldRoot.gameObject);
        }

        // Create Main UI Root
        GameObject uiRoot = new GameObject("ShopUIRoot", typeof(RectTransform));
        uiRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = uiRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // --- BACKGROUND ---
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(uiRoot.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);

        // --- HEADER BAR ---
        GameObject headerObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(uiRoot.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.91f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        Image headerImg = headerObj.GetComponent<Image>();
        headerImg.color = new Color(0.1f, 0.13f, 0.18f, 0.95f);

        // Header Title
        GameObject headerTitleObj = CreateTextObject("ShopTitle", headerObj.transform, "ITEM SHOP", 32, TextAlignmentOptions.Left);
        RectTransform headerTitleRt = headerTitleObj.GetComponent<RectTransform>();
        headerTitleRt.anchorMin = new Vector2(0.03f, 0f);
        headerTitleRt.anchorMax = new Vector2(0.3f, 1f);
        headerTitleRt.offsetMin = Vector2.zero;
        headerTitleRt.offsetMax = Vector2.zero;

        // Money Display Text
        GameObject moneyObj = CreateTextObject("MoneyText", headerObj.transform, "Gold: <color=#FFD700>1000</color>", 26, TextAlignmentOptions.Right);
        RectTransform moneyRt = moneyObj.GetComponent<RectTransform>();
        moneyRt.anchorMin = new Vector2(0.65f, 0f);
        moneyRt.anchorMax = new Vector2(0.85f, 1f);
        moneyRt.offsetMin = Vector2.zero;
        moneyRt.offsetMax = Vector2.zero;

        // Debug Add Money Button
        GameObject addMoneyBtnObj = CreateButton("AddMoneyButton", headerObj.transform, "+500 Gold", 18, new Color(0.2f, 0.5f, 0.3f));
        RectTransform addMoneyRt = addMoneyBtnObj.GetComponent<RectTransform>();
        addMoneyRt.anchorMin = new Vector2(0.87f, 0.15f);
        addMoneyRt.anchorMax = new Vector2(0.97f, 0.85f);
        addMoneyRt.offsetMin = Vector2.zero;
        addMoneyRt.offsetMax = Vector2.zero;

        // --- CATEGORY BAR (CENTER TOP) ---
        GameObject categoryBarObj = new GameObject("CategoryBar", typeof(RectTransform));
        categoryBarObj.transform.SetParent(uiRoot.transform, false);
        RectTransform catBarRt = categoryBarObj.GetComponent<RectTransform>();
        catBarRt.anchorMin = new Vector2(0.25f, 0.80f);
        catBarRt.anchorMax = new Vector2(0.75f, 0.89f);
        catBarRt.offsetMin = Vector2.zero;
        catBarRt.offsetMax = Vector2.zero;

        // Category Buttons (Seed, Room, Accessory)
        GameObject seedBtnObj = CreateButton("SeedCategoryButton", categoryBarObj.transform, "SEED", 22, new Color(0.2f, 0.6f, 1f));
        RectTransform seedRt = seedBtnObj.GetComponent<RectTransform>();
        seedRt.anchorMin = new Vector2(0.0f, 0.1f);
        seedRt.anchorMax = new Vector2(0.31f, 0.9f);
        seedRt.offsetMin = Vector2.zero;
        seedRt.offsetMax = Vector2.zero;

        GameObject roomBtnObj = CreateButton("RoomCategoryButton", categoryBarObj.transform, "ROOM", 22, new Color(0.2f, 0.25f, 0.35f));
        RectTransform roomRt = roomBtnObj.GetComponent<RectTransform>();
        roomRt.anchorMin = new Vector2(0.34f, 0.1f);
        roomRt.anchorMax = new Vector2(0.65f, 0.9f);
        roomRt.offsetMin = Vector2.zero;
        roomRt.offsetMax = Vector2.zero;

        GameObject accBtnObj = CreateButton("AccessoryCategoryButton", categoryBarObj.transform, "ACCESSORY", 22, new Color(0.2f, 0.25f, 0.35f));
        RectTransform accRt = accBtnObj.GetComponent<RectTransform>();
        accRt.anchorMin = new Vector2(0.68f, 0.1f);
        accRt.anchorMax = new Vector2(1.0f, 0.9f);
        accRt.offsetMin = Vector2.zero;
        accRt.offsetMax = Vector2.zero;

        // --- CAROUSEL MAIN CONTENT AREA ---
        GameObject carouselAreaObj = new GameObject("CarouselArea", typeof(RectTransform));
        carouselAreaObj.transform.SetParent(uiRoot.transform, false);
        RectTransform carouselAreaRt = carouselAreaObj.GetComponent<RectTransform>();
        carouselAreaRt.anchorMin = new Vector2(0.1f, 0.10f);
        carouselAreaRt.anchorMax = new Vector2(0.9f, 0.78f);
        carouselAreaRt.offsetMin = Vector2.zero;
        carouselAreaRt.offsetMax = Vector2.zero;

        // Navigation Buttons (Prev < and Next >)
        GameObject prevBtnObj = CreateButton("PrevButton", carouselAreaObj.transform, "<", 36, new Color(0.18f, 0.22f, 0.3f));
        RectTransform prevRt = prevBtnObj.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0.08f, 0.42f);
        prevRt.anchorMax = new Vector2(0.18f, 0.58f);
        prevRt.offsetMin = Vector2.zero;
        prevRt.offsetMax = Vector2.zero;

        GameObject nextBtnObj = CreateButton("NextButton", carouselAreaObj.transform, ">", 36, new Color(0.18f, 0.22f, 0.3f));
        RectTransform nextRt = nextBtnObj.GetComponent<RectTransform>();
        nextRt.anchorMin = new Vector2(0.82f, 0.42f);
        nextRt.anchorMax = new Vector2(0.92f, 0.58f);
        nextRt.offsetMin = Vector2.zero;
        nextRt.offsetMax = Vector2.zero;

        // Main Item Card Frame (Center)
        GameObject cardFrameObj = new GameObject("ItemCardFrame", typeof(RectTransform), typeof(Image));
        cardFrameObj.transform.SetParent(carouselAreaObj.transform, false);
        RectTransform cardFrameRt = cardFrameObj.GetComponent<RectTransform>();
        cardFrameRt.anchorMin = new Vector2(0.25f, 0.08f);
        cardFrameRt.anchorMax = new Vector2(0.75f, 0.92f);
        cardFrameRt.offsetMin = Vector2.zero;
        cardFrameRt.offsetMax = Vector2.zero;
        Image cardFrameImg = cardFrameObj.GetComponent<Image>();
        cardFrameImg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
        cardFrameImg.raycastTarget = true;
        ShopCarouselSwipeHandler swipeComp = cardFrameObj.AddComponent<ShopCarouselSwipeHandler>();

        // 1) TITLE ABOVE IMAGE
        GameObject titleObj = CreateTextObject("ItemTitleText", cardFrameObj.transform, "Item Title", 30, TextAlignmentOptions.Center);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.85f);
        titleRt.anchorMax = new Vector2(0.95f, 0.97f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        // 2) IMAGE IN CENTER
        GameObject itemImgObj = new GameObject("ItemImage", typeof(RectTransform), typeof(Image));
        itemImgObj.transform.SetParent(cardFrameObj.transform, false);
        RectTransform itemImgRt = itemImgObj.GetComponent<RectTransform>();
        itemImgRt.anchorMin = new Vector2(0.2f, 0.40f);
        itemImgRt.anchorMax = new Vector2(0.8f, 0.82f);
        itemImgRt.offsetMin = Vector2.zero;
        itemImgRt.offsetMax = Vector2.zero;
        Image itemImg = itemImgObj.GetComponent<Image>();
        itemImg.preserveAspect = true;

        // 3) PRICE UNDER IMAGE
        GameObject priceObj = CreateTextObject("ItemPriceText", cardFrameObj.transform, "100 Gold", 26, TextAlignmentOptions.Center);
        RectTransform priceRt = priceObj.GetComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0.05f, 0.29f);
        priceRt.anchorMax = new Vector2(0.95f, 0.38f);
        priceRt.offsetMin = Vector2.zero;
        priceRt.offsetMax = Vector2.zero;
        TextMeshProUGUI priceTmp = priceObj.GetComponent<TextMeshProUGUI>();
        priceTmp.color = new Color(1f, 0.85f, 0.2f);

        // Item Description
        GameObject descObj = CreateTextObject("ItemDescriptionText", cardFrameObj.transform, "Item description goes here...", 18, TextAlignmentOptions.Center);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.05f, 0.18f);
        descRt.anchorMax = new Vector2(0.95f, 0.28f);
        descRt.offsetMin = Vector2.zero;
        descRt.offsetMax = Vector2.zero;
        descObj.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);

        // Buy Button
        GameObject buyBtnObj = CreateButton("BuyButton", cardFrameObj.transform, "BUY NOW", 22, new Color(0.15f, 0.65f, 0.3f));
        RectTransform buyRt = buyBtnObj.GetComponent<RectTransform>();
        buyRt.anchorMin = new Vector2(0.25f, 0.04f);
        buyRt.anchorMax = new Vector2(0.75f, 0.16f);
        buyRt.offsetMin = Vector2.zero;
        buyRt.offsetMax = Vector2.zero;

        // Page Indicator Text (Below card)
        GameObject pageIndObj = CreateTextObject("PageIndicatorText", carouselAreaObj.transform, "1 / 3", 20, TextAlignmentOptions.Center);
        RectTransform pageIndRt = pageIndObj.GetComponent<RectTransform>();
        pageIndRt.anchorMin = new Vector2(0.4f, 0.0f);
        pageIndRt.anchorMax = new Vector2(0.6f, 0.06f);
        pageIndRt.offsetMin = Vector2.zero;
        pageIndRt.offsetMax = Vector2.zero;

        // --- TOAST FEEDBACK MESSAGE ---
        GameObject toastObj = CreateTextObject("ToastMessageText", uiRoot.transform, "Feedback Toast", 22, TextAlignmentOptions.Center);
        RectTransform toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.2f, 0.02f);
        toastRt.anchorMax = new Vector2(0.8f, 0.08f);
        toastRt.offsetMin = Vector2.zero;
        toastRt.offsetMax = Vector2.zero;
        toastObj.SetActive(false);

        // --- CONFIRMATION POPUP DIALOG ---
        GameObject popupPanelObj = new GameObject("ConfirmationPopupPanel", typeof(RectTransform), typeof(Image));
        popupPanelObj.transform.SetParent(uiRoot.transform, false);
        RectTransform popupPanelRt = popupPanelObj.GetComponent<RectTransform>();
        popupPanelRt.anchorMin = Vector2.zero;
        popupPanelRt.anchorMax = Vector2.one;
        popupPanelRt.offsetMin = Vector2.zero;
        popupPanelRt.offsetMax = Vector2.zero;
        Image popupPanelBg = popupPanelObj.GetComponent<Image>();
        popupPanelBg.color = new Color(0f, 0f, 0f, 0.75f);

        // Popup Container Box
        GameObject popupBoxObj = new GameObject("PopupContainerBox", typeof(RectTransform), typeof(Image));
        popupBoxObj.transform.SetParent(popupPanelObj.transform, false);
        RectTransform popupBoxRt = popupBoxObj.GetComponent<RectTransform>();
        popupBoxRt.anchorMin = new Vector2(0.32f, 0.28f);
        popupBoxRt.anchorMax = new Vector2(0.68f, 0.72f);
        popupBoxRt.offsetMin = Vector2.zero;
        popupBoxRt.offsetMax = Vector2.zero;
        Image popupBoxImg = popupBoxObj.GetComponent<Image>();
        popupBoxImg.color = new Color(0.14f, 0.17f, 0.24f, 1f);

        // Popup Header Title
        GameObject popTitleObj = CreateTextObject("PopupTitleText", popupBoxObj.transform, "CONFIRM PURCHASE", 26, TextAlignmentOptions.Center);
        RectTransform popTitleRt = popTitleObj.GetComponent<RectTransform>();
        popTitleRt.anchorMin = new Vector2(0.05f, 0.85f);
        popTitleRt.anchorMax = new Vector2(0.95f, 0.96f);
        popTitleRt.offsetMin = Vector2.zero;
        popTitleRt.offsetMax = Vector2.zero;

        // Popup Message Prompt
        GameObject popPromptObj = CreateTextObject("PromptMessageText", popupBoxObj.transform, "Are you sure you want to buy\n<color=#FFD700>Item Name</color>?", 22, TextAlignmentOptions.Center);
        RectTransform popPromptRt = popPromptObj.GetComponent<RectTransform>();
        popPromptRt.anchorMin = new Vector2(0.05f, 0.58f);
        popPromptRt.anchorMax = new Vector2(0.95f, 0.83f);
        popPromptRt.offsetMin = Vector2.zero;
        popPromptRt.offsetMax = Vector2.zero;

        // Popup Image Preview
        GameObject popPreviewImgObj = new GameObject("ItemPreviewImage", typeof(RectTransform), typeof(Image));
        popPreviewImgObj.transform.SetParent(popupBoxObj.transform, false);
        RectTransform popPreviewRt = popPreviewImgObj.GetComponent<RectTransform>();
        popPreviewRt.anchorMin = new Vector2(0.35f, 0.32f);
        popPreviewRt.anchorMax = new Vector2(0.65f, 0.56f);
        popPreviewRt.offsetMin = Vector2.zero;
        popPreviewRt.offsetMax = Vector2.zero;
        Image popPreviewImg = popPreviewImgObj.GetComponent<Image>();
        popPreviewImg.preserveAspect = true;

        // Popup Price Text
        GameObject popPriceObj = CreateTextObject("PopupPriceText", popupBoxObj.transform, "100 Gold", 22, TextAlignmentOptions.Center);
        RectTransform popPriceRt = popPriceObj.GetComponent<RectTransform>();
        popPriceRt.anchorMin = new Vector2(0.05f, 0.22f);
        popPriceRt.anchorMax = new Vector2(0.95f, 0.31f);
        popPriceRt.offsetMin = Vector2.zero;
        popPriceRt.offsetMax = Vector2.zero;
        popPriceObj.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.2f);

        // Confirm Button
        GameObject confirmBtnObj = CreateButton("ConfirmButton", popupBoxObj.transform, "CONFIRM", 20, new Color(0.15f, 0.65f, 0.3f));
        RectTransform confirmRt = confirmBtnObj.GetComponent<RectTransform>();
        confirmRt.anchorMin = new Vector2(0.1f, 0.05f);
        confirmRt.anchorMax = new Vector2(0.46f, 0.19f);
        confirmRt.offsetMin = Vector2.zero;
        confirmRt.offsetMax = Vector2.zero;

        // Cancel Button
        GameObject cancelBtnObj = CreateButton("CancelButton", popupBoxObj.transform, "CANCEL", 20, new Color(0.65f, 0.2f, 0.2f));
        RectTransform cancelRt = cancelBtnObj.GetComponent<RectTransform>();
        cancelRt.anchorMin = new Vector2(0.54f, 0.05f);
        cancelRt.anchorMax = new Vector2(0.9f, 0.19f);
        cancelRt.offsetMin = Vector2.zero;
        cancelRt.offsetMax = Vector2.zero;

        // Attach & Setup ShopConfirmationPopup
        ShopConfirmationPopup popupComp = popupPanelObj.GetComponent<ShopConfirmationPopup>();
        if (popupComp == null) popupComp = popupPanelObj.AddComponent<ShopConfirmationPopup>();

#if UNITY_EDITOR
        SerializedObject popSo = new SerializedObject(popupComp);
        popSo.FindProperty("popupContainer").objectReferenceValue = popupPanelObj;
        popSo.FindProperty("titleText").objectReferenceValue = popTitleObj.GetComponent<TextMeshProUGUI>();
        popSo.FindProperty("promptMessageText").objectReferenceValue = popPromptObj.GetComponent<TextMeshProUGUI>();
        popSo.FindProperty("itemPreviewImage").objectReferenceValue = popPreviewImg;
        popSo.FindProperty("priceText").objectReferenceValue = popPriceObj.GetComponent<TextMeshProUGUI>();
        popSo.FindProperty("confirmButton").objectReferenceValue = confirmBtnObj.GetComponent<Button>();
        popSo.FindProperty("cancelButton").objectReferenceValue = cancelBtnObj.GetComponent<Button>();
        popSo.ApplyModifiedProperties();
#endif
        popupPanelObj.SetActive(false);

        // --- ATTACH & SETUP SHOP MANAGER ---
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            GameObject managerObj = new GameObject("ShopManager");
            shopManager = managerObj.AddComponent<ShopManager>();
        }

#if UNITY_EDITOR
        SerializedObject mgrSo = new SerializedObject(shopManager);
        mgrSo.FindProperty("seedCategoryButton").objectReferenceValue = seedBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("roomCategoryButton").objectReferenceValue = roomBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("accessoryCategoryButton").objectReferenceValue = accBtnObj.GetComponent<Button>();

        mgrSo.FindProperty("seedButtonBg").objectReferenceValue = seedBtnObj.GetComponent<Image>();
        mgrSo.FindProperty("roomButtonBg").objectReferenceValue = roomBtnObj.GetComponent<Image>();
        mgrSo.FindProperty("accessoryButtonBg").objectReferenceValue = accBtnObj.GetComponent<Image>();

        mgrSo.FindProperty("prevButton").objectReferenceValue = prevBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("nextButton").objectReferenceValue = nextBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("pageIndicatorText").objectReferenceValue = pageIndObj.GetComponent<TextMeshProUGUI>();

        mgrSo.FindProperty("itemCardFrame").objectReferenceValue = cardFrameRt;
        mgrSo.FindProperty("swipeHandler").objectReferenceValue = swipeComp;
        mgrSo.FindProperty("itemTitleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        mgrSo.FindProperty("itemImage").objectReferenceValue = itemImg;
        mgrSo.FindProperty("itemPriceText").objectReferenceValue = priceObj.GetComponent<TextMeshProUGUI>();
        mgrSo.FindProperty("itemDescriptionText").objectReferenceValue = descObj.GetComponent<TextMeshProUGUI>();
        mgrSo.FindProperty("buyButton").objectReferenceValue = buyBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("buyButtonText").objectReferenceValue = buyBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        mgrSo.FindProperty("moneyText").objectReferenceValue = moneyObj.GetComponent<TextMeshProUGUI>();
        mgrSo.FindProperty("toastMessageText").objectReferenceValue = toastObj.GetComponent<TextMeshProUGUI>();
        mgrSo.FindProperty("addMoneyDebugButton").objectReferenceValue = addMoneyBtnObj.GetComponent<Button>();
        mgrSo.FindProperty("confirmationPopup").objectReferenceValue = popupComp;

        mgrSo.ApplyModifiedProperties();
#endif

        Debug.Log("[ShopSetup] Shop UI setup completed successfully!");
    }

    private static GameObject CreateTextObject(string name, Transform parent, string textContent, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = textContent;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false; // Prevent child text from blocking parent button clicks
        return textGo;
    }

    private static GameObject CreateButton(string name, Transform parent, string labelText, int fontSize, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true; // Ensure button image receives raycast input

        GameObject txtObj = CreateTextObject("Text", btnObj.transform, labelText, fontSize, TextAlignmentOptions.Center);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        return btnObj;
    }
}
