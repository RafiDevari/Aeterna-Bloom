using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Category Navigation Buttons")]
    [SerializeField] private Button seedCategoryButton;
    [SerializeField] private Button roomCategoryButton;
    [SerializeField] private Button accessoryCategoryButton;
    [SerializeField] private Button upgradeCategoryButton;
    [SerializeField] private Button hireCategoryButton;

    [SerializeField] private Image seedButtonBg;
    [SerializeField] private Image roomButtonBg;
    [SerializeField] private Image accessoryButtonBg;
    [SerializeField] private Image upgradeButtonBg;
    [SerializeField] private Image hireButtonBg;

    [Header("Carousel Elements")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("Item Card UI Elements (Title Above, Image Center, Price Under)")]
    [SerializeField] private RectTransform itemCardFrame;
    [SerializeField] private ShopCarouselSwipeHandler swipeHandler;
    [SerializeField] private TextMeshProUGUI itemTitleText;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    [Header("HUD & Feedback")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI toastMessageText;
    [SerializeField] private Button addMoneyDebugButton;

    [Header("Popup References")]
    [SerializeField] private ShopConfirmationPopup confirmationPopup;

    private ShopDatabase shopDatabase;
    private string currentCategory = "Seed";
    private List<ShopItemData> currentCategoryItems = new List<ShopItemData>();
    private int currentItemIndex = 0;
    private Coroutine toastCoroutine;
    private Coroutine cardTransitionCoroutine;
    private bool isTransitioning = false;

    // Color theme for category selection
    private Color activeCategoryColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
    private Color inactiveCategoryColor = new Color(0.2f, 0.25f, 0.35f, 0.8f);

    private void Awake()
    {
        // Subscribe to GameSaveSystem money & day events
        GameSaveSystem.OnMoneyChanged += UpdateMoneyDisplay;
        GameSaveSystem.OnDayChanged += OnDayChangedHandler;
    }

    private void OnDestroy()
    {
        GameSaveSystem.OnMoneyChanged -= UpdateMoneyDisplay;
        GameSaveSystem.OnDayChanged -= OnDayChangedHandler;

        if (swipeHandler != null)
        {
            swipeHandler.OnSwipeLeft -= TriggerNextWithAnimation;
            swipeHandler.OnSwipeRight -= TriggerPrevWithAnimation;
        }
    }

    private void OnDayChangedHandler(int newDay)
    {
        RefreshCarouselDisplay();
    }

    private void Start()
    {
        // Load Shop Data from shop_items.json
        shopDatabase = ShopDatabase.LoadFromResources("shop_items");

        // Setup Button Listeners
        if (seedCategoryButton != null) seedCategoryButton.onClick.AddListener(() => SwitchCategory("Seed"));
        if (roomCategoryButton != null) roomCategoryButton.onClick.AddListener(() => SwitchCategory("Room"));
        if (accessoryCategoryButton != null) accessoryCategoryButton.onClick.AddListener(() => SwitchCategory("Accessory"));
        if (upgradeCategoryButton != null) upgradeCategoryButton.onClick.AddListener(() => SwitchCategory("Upgrade"));
        if (hireCategoryButton != null) hireCategoryButton.onClick.AddListener(() => SwitchCategory("Hire"));

        if (prevButton != null) prevButton.onClick.AddListener(OnPrevItemClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextItemClicked);

        if (buyButton != null) buyButton.onClick.AddListener(OnBuyItemClicked);
        if (addMoneyDebugButton != null) addMoneyDebugButton.onClick.AddListener(OnAddMoneyClicked);

        // Auto-detect item card frame & swipe handler if needed
        if (itemCardFrame == null && itemTitleText != null && itemTitleText.transform.parent != null)
        {
            itemCardFrame = itemTitleText.transform.parent.GetComponent<RectTransform>();
        }

        if (swipeHandler == null && itemCardFrame != null)
        {
            swipeHandler = itemCardFrame.GetComponent<ShopCarouselSwipeHandler>();
        }

        if (swipeHandler != null)
        {
            swipeHandler.OnSwipeLeft += TriggerNextWithAnimation;
            swipeHandler.OnSwipeRight += TriggerPrevWithAnimation;
        }

        // Load initial money
        UpdateMoneyDisplay(GameSaveSystem.Instance.Money);

        // Hide Toast
        if (toastMessageText != null) toastMessageText.gameObject.SetActive(false);

        // Set initial category to Seed
        SwitchCategory("Seed");
    }

    public void SwitchCategory(string category)
    {
        currentCategory = category;
        currentCategoryItems = shopDatabase.GetItemsByCategory(currentCategory);
        currentItemIndex = 0;

        if (cardTransitionCoroutine != null)
        {
            StopCoroutine(cardTransitionCoroutine);
            cardTransitionCoroutine = null;
        }
        isTransitioning = false;

        if (swipeHandler != null)
        {
            swipeHandler.ResetPositionInstant();
            swipeHandler.CanDrag = true;
        }

        UpdateCategoryButtonVisuals();
        RefreshCarouselDisplay();
    }

    private void UpdateCategoryButtonVisuals()
    {
        if (seedButtonBg != null) seedButtonBg.color = (currentCategory == "Seed") ? activeCategoryColor : inactiveCategoryColor;
        if (roomButtonBg != null) roomButtonBg.color = (currentCategory == "Room") ? activeCategoryColor : inactiveCategoryColor;
        if (accessoryButtonBg != null) accessoryButtonBg.color = (currentCategory == "Accessory") ? activeCategoryColor : inactiveCategoryColor;
        if (upgradeButtonBg != null) upgradeButtonBg.color = (currentCategory == "Upgrade") ? activeCategoryColor : inactiveCategoryColor;
        if (hireButtonBg != null) hireButtonBg.color = (currentCategory == "Hire") ? activeCategoryColor : inactiveCategoryColor;
    }

    public int GetItemPrice(ShopItemData item)
    {
        if (item == null) return 0;

        if (string.Equals(item.id, "upgrade_electricity", System.StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(item.category, "Upgrade", System.StringComparison.OrdinalIgnoreCase) && item.id.ToLower().Contains("electricity")))
        {
            int level = (GameSaveSystem.Instance != null) ? GameSaveSystem.Instance.ElectricityLevel : 1;
            return 400 + (80 * (level - 1));
        }

        return item.price;
    }

    private void RefreshCarouselDisplay()
    {
        if (currentCategoryItems == null || currentCategoryItems.Count == 0)
        {
            if (itemTitleText != null) itemTitleText.text = "No Items Available";
            if (itemPriceText != null) itemPriceText.text = "-";
            if (itemDescriptionText != null) itemDescriptionText.text = "";
            if (itemImage != null) itemImage.gameObject.SetActive(false);
            if (pageIndicatorText != null) pageIndicatorText.text = "0 / 0";
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        if (currentItemIndex < 0) currentItemIndex = 0;
        if (currentItemIndex >= currentCategoryItems.Count) currentItemIndex = currentCategoryItems.Count - 1;

        ShopItemData item = currentCategoryItems[currentItemIndex];

        // Title & Description ABOVE / UNDER Image
        if (string.Equals(item.category, "Upgrade", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(item.id, "upgrade_electricity", System.StringComparison.OrdinalIgnoreCase) || item.id.ToLower().Contains("electricity"))
            {
                int currentLevel = (GameSaveSystem.Instance != null) ? GameSaveSystem.Instance.ElectricityLevel : 1;
                if (itemTitleText != null) itemTitleText.text = $"{item.title} (Lv. {currentLevel})";
                if (itemDescriptionText != null) itemDescriptionText.text = $"{item.description}\nCapacity: {100 + (currentLevel - 1) * 50} -> {100 + currentLevel * 50}";
            }
            else
            {
                if (itemTitleText != null) itemTitleText.text = item.title;
                if (itemDescriptionText != null) itemDescriptionText.text = item.description;
            }
        }
        else if (string.Equals(item.category, "Hire", System.StringComparison.OrdinalIgnoreCase) || item.id.ToLower().Contains("hire"))
        {
            if (itemTitleText != null) itemTitleText.text = item.title;
            if (itemDescriptionText != null) itemDescriptionText.text = $"{item.description}\n";
        }
        else
        {
            if (itemTitleText != null) itemTitleText.text = item.title;
            if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        }

        // Image in Center
        if (itemImage != null)
        {
            Sprite itemSprite = null;
            if (!string.IsNullOrEmpty(item.spritePath))
            {
                itemSprite = Resources.Load<Sprite>(item.spritePath);
#if UNITY_EDITOR
                if (itemSprite == null)
                {
                    itemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + item.spritePath + ".png");
                }
                if (itemSprite == null)
                {
                    itemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + item.spritePath + ".jpg");
                }
#endif
            }

            if (itemSprite != null)
            {
                itemImage.sprite = itemSprite;
                itemImage.color = Color.white;
                itemImage.gameObject.SetActive(true);
            }
            else
            {
                // Fallback icon placeholder visual
                itemImage.sprite = null;
                itemImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                itemImage.gameObject.SetActive(true);
            }
        }

        // Price UNDER Image
        int actualPrice = GetItemPrice(item);
        if (itemPriceText != null)
        {
            itemPriceText.text = $"{actualPrice} Gold";
        }

        // Page indicator
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentItemIndex + 1} / {currentCategoryItems.Count}";
        }

        // Navigation button interactability
        if (prevButton != null) prevButton.interactable = (currentItemIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentItemIndex < currentCategoryItems.Count - 1);

        bool isOneTime = IsOneTimePurchaseCategory(item.category);
        bool isAlreadyPurchased = isOneTime && GameSaveSystem.Instance.IsItemPurchased(item.id);
        bool isDayLocked = item.day > 0 && GameSaveSystem.Instance.Day < item.day;

        if (buyButton != null) buyButton.interactable = !isAlreadyPurchased && !isDayLocked;

        TextMeshProUGUI btnText = buyButtonText != null ? buyButtonText : (buyButton != null ? buyButton.GetComponentInChildren<TextMeshProUGUI>() : null);
        if (btnText != null)
        {
            if (isAlreadyPurchased)
            {
                btnText.text = "OWNED";
            }
            else if (isDayLocked)
            {
                btnText.text = $"LOCKED (DAY {item.day})";
            }
            else if (string.Equals(item.category, "Upgrade", System.StringComparison.OrdinalIgnoreCase))
            {
                btnText.text = "UPGRADE NOW";
            }
            else if (string.Equals(item.category, "Hire", System.StringComparison.OrdinalIgnoreCase) || item.id.ToLower().Contains("hire"))
            {
                btnText.text = "HIRE NOW";
            }
            else
            {
                btnText.text = "BUY NOW";
            }
        }
    }

    private bool IsOneTimePurchaseCategory(string category)
    {
        return string.Equals(category, "Seed", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(category, "Accessory", System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnPrevItemClicked()
    {
        TriggerPrevWithAnimation();
    }

    private void OnNextItemClicked()
    {
        TriggerNextWithAnimation();
    }

    public void TriggerNextWithAnimation()
    {
        if (isTransitioning || currentCategoryItems == null) return;

        if (currentItemIndex < currentCategoryItems.Count - 1)
        {
            if (cardTransitionCoroutine != null) StopCoroutine(cardTransitionCoroutine);
            cardTransitionCoroutine = StartCoroutine(CardSlideTransitionRoutine(1));
        }
        else if (swipeHandler != null)
        {
            swipeHandler.ResetPositionSmooth();
        }
    }

    public void TriggerPrevWithAnimation()
    {
        if (isTransitioning || currentCategoryItems == null) return;

        if (currentItemIndex > 0)
        {
            if (cardTransitionCoroutine != null) StopCoroutine(cardTransitionCoroutine);
            cardTransitionCoroutine = StartCoroutine(CardSlideTransitionRoutine(-1));
        }
        else if (swipeHandler != null)
        {
            swipeHandler.ResetPositionSmooth();
        }
    }

    private IEnumerator CardSlideTransitionRoutine(int direction)
    {
        isTransitioning = true;
        if (swipeHandler != null) swipeHandler.CanDrag = false;

        float slideOutDuration = 0.12f;
        float slideInDuration = 0.18f;

        if (itemCardFrame != null)
        {
            Vector2 startPos = itemCardFrame.anchoredPosition;
            float targetExitX = (direction == 1) ? -700f : 700f;
            float startEntryX = (direction == 1) ? 700f : -700f;

            // Slide out
            float elapsed = 0f;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideOutDuration;
                itemCardFrame.anchoredPosition = Vector2.Lerp(startPos, new Vector2(targetExitX, 0f), t * t);
                yield return null;
            }

            // Advance item index and update card contents
            currentItemIndex += direction;
            RefreshCarouselDisplay();

            // Set offscreen entry position
            itemCardFrame.anchoredPosition = new Vector2(startEntryX, 0f);
            itemCardFrame.localRotation = Quaternion.identity;

            // Slide in smoothly to center
            elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
                itemCardFrame.anchoredPosition = Vector2.Lerp(new Vector2(startEntryX, 0f), Vector2.zero, smoothT);
                yield return null;
            }

            itemCardFrame.anchoredPosition = Vector2.zero;
            itemCardFrame.localRotation = Quaternion.identity;
        }
        else
        {
            currentItemIndex += direction;
            RefreshCarouselDisplay();
        }

        if (swipeHandler != null) swipeHandler.CanDrag = true;
        isTransitioning = false;
        cardTransitionCoroutine = null;
    }

    private void OnBuyItemClicked()
    {
        if (currentCategoryItems == null || currentItemIndex < 0 || currentItemIndex >= currentCategoryItems.Count)
            return;

        ShopItemData currentItem = currentCategoryItems[currentItemIndex];

        if (currentItem.day > 0 && GameSaveSystem.Instance.Day < currentItem.day)
        {
            ShowToast($"<color=#FF5555>{currentItem.title} is locked! Unlocks at Day {currentItem.day}.</color>");
            RefreshCarouselDisplay();
            return;
        }

        if (IsOneTimePurchaseCategory(currentItem.category) && GameSaveSystem.Instance.IsItemPurchased(currentItem.id))
        {
            ShowToast($"<color=#FF5555>You already own {currentItem.title}!</color>");
            RefreshCarouselDisplay();
            return;
        }

        int actualPrice = GetItemPrice(currentItem);

        if (confirmationPopup != null)
        {
            confirmationPopup.Show(currentItem, actualPrice, () =>
            {
                ExecutePurchase(currentItem);
            });
        }
        else
        {
            // Fallback if popup is null
            ExecutePurchase(currentItem);
        }
    }

    public bool ExecutePurchase(ShopItemData item)
    {
        if (item == null) return false;

        if (item.day > 0 && GameSaveSystem.Instance.Day < item.day)
        {
            ShowToast($"<color=#FF5555>{item.title} is locked! Unlocks at Day {item.day}.</color>");
            RefreshCarouselDisplay();
            return false;
        }

        if (IsOneTimePurchaseCategory(item.category) && GameSaveSystem.Instance.IsItemPurchased(item.id))
        {
            ShowToast($"<color=#FF5555>You already own {item.title}!</color>");
            RefreshCarouselDisplay();
            return false;
        }

        int actualPrice = GetItemPrice(item);

        // Try spend money using GameSaveSystem (returns true if successful, false if insufficient funds)
        bool success = GameSaveSystem.Instance.TrySpendMoney(actualPrice);

        if (success)
        {
            if (IsOneTimePurchaseCategory(item.category))
            {
                GameSaveSystem.Instance.AddPurchasedItem(item.id);

                if (string.Equals(item.category, "Seed", System.StringComparison.OrdinalIgnoreCase))
                {
                    AddPurchasedPlantToInventory(item);
                }
            }
            else if (string.Equals(item.category, "Room", System.StringComparison.OrdinalIgnoreCase))
            {
                AddPurchasedRoomToInventory(item);
            }
            else if (string.Equals(item.category, "Upgrade", System.StringComparison.OrdinalIgnoreCase) || item.id.ToLower().Contains("upgrade"))
            {
                ExecuteUpgradePurchase(item);
            }
            else if (string.Equals(item.category, "Hire", System.StringComparison.OrdinalIgnoreCase) || item.id.ToLower().Contains("hire"))
            {
                ExecuteHirePurchase(item);
            }

            ShowToast($"<color=#55FF55>Successfully bought {item.title} for {actualPrice} Gold!</color>");
            RefreshCarouselDisplay();
        }
        else
        {
            ShowToast($"<color=#FF5555>Not enough money! Need {actualPrice} Gold (Have {GameSaveSystem.Instance.Money} Gold).</color>");
        }

        return success;
    }

    private void ExecuteHirePurchase(ShopItemData item)
    {
        if (item == null) return;

        EmployeeInventorySaveSystem invSystem = EmployeeInventorySaveSystem.Instance;
        if (invSystem == null) invSystem = FindFirstObjectByType<EmployeeInventorySaveSystem>();
        if (invSystem == null)
        {
            GameObject go = new GameObject("EmployeeInventorySaveSystem");
            invSystem = go.AddComponent<EmployeeInventorySaveSystem>();
        }

        if (invSystem != null)
        {
            var invData = invSystem.LoadInventory();
            int currentCount = (invData != null && invData.employees != null) ? invData.employees.Count : 0;

            string[] firstNames = new string[] {
                "Fiona", "Alice", "Julia", "Laura", "Nora", "Penelope", "Rachel", "Tina", "Wendy", "Yara",
                "Beatrice", "Diana", "Clara", "Elena", "Grace", "Iris", "Kira", "Luna", "Maya", "Nina",
                "Olivia", "Stella", "Violet", "Zoe", "Arthur", "Bob", "Charlie", "Daniel", "Edward", "George",
                "Ian", "Kevin", "Michael", "Oliver", "Quinn", "Steven", "Victor", "Xavier", "Zack", "Caleb",
                "Felix", "Henry", "Leo", "Oscar"
            };

            string[] lastNames = new string[] {
                "George", "Vance", "Miller", "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Davis",
                "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore",
                "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez",
                "Lewis", "Robinson", "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen",
                "Hill", "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell"
            };

            string firstName = firstNames[UnityEngine.Random.Range(0, firstNames.Length)];
            string lastName = lastNames[UnityEngine.Random.Range(0, lastNames.Length)];
            string empName = $"{firstName} {lastName}";

            EmployeeDivision targetDiv = GetDivisionFromItem(item);

            string prefabName = "Employee" + targetDiv.ToString();
            Color suitColor = GetDefaultSuitColorForDivision(targetDiv);

            // Random hair color as requested
            Color randomHairColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(0.4f, 0.95f), UnityEngine.Random.Range(0.4f, 1f));

            EmployeeInventoryItemSaveData newEmp = new EmployeeInventoryItemSaveData(
                empName,
                prefabName,
                targetDiv,
                suitColor,
                randomHairColor
            );

            invSystem.AddEmployee(newEmp);
            Debug.Log($"[ShopManager] Hired new employee '{empName}' ({targetDiv}) with random hair color ({randomHairColor})");
        }
    }

    private EmployeeDivision GetDivisionFromItem(ShopItemData item)
    {
        if (item == null) return EmployeeDivision.Botanist;

        string idStr = (item.id ?? "").ToLower();
        string titleStr = (item.title ?? "").ToLower();

        if (idStr.Contains("botanist") || titleStr.Contains("botanist")) return EmployeeDivision.Botanist;
        if (idStr.Contains("researcher") || titleStr.Contains("researcher")) return EmployeeDivision.Researcher;
        if (idStr.Contains("security") || titleStr.Contains("security")) return EmployeeDivision.Security;
        if (idStr.Contains("medic") || titleStr.Contains("medic")) return EmployeeDivision.Medic;
        if (idStr.Contains("engineer") || titleStr.Contains("engineer")) return EmployeeDivision.Engineer;
        if (idStr.Contains("clerk") || titleStr.Contains("clerk")) return EmployeeDivision.Clerk;

        EmployeeDivision[] divisions = new EmployeeDivision[] {
            EmployeeDivision.Botanist,
            EmployeeDivision.Researcher,
            EmployeeDivision.Security,
            EmployeeDivision.Medic,
            EmployeeDivision.Engineer,
            EmployeeDivision.Clerk
        };
        return divisions[UnityEngine.Random.Range(0, divisions.Length)];
    }

    private Color GetDefaultSuitColorForDivision(EmployeeDivision div)
    {
        switch (div)
        {
            case EmployeeDivision.Botanist: return new Color(0.4f, 0.8f, 0.4f, 1f);
            case EmployeeDivision.Researcher: return new Color(0.4f, 0.6f, 0.9f, 1f);
            case EmployeeDivision.Security: return new Color(0.8f, 0.3f, 0.3f, 1f);
            case EmployeeDivision.Medic: return new Color(0.7f, 0.4f, 0.8f, 1f);
            case EmployeeDivision.Engineer: return new Color(0.9f, 0.7f, 0.3f, 1f);
            default: return new Color(0.8f, 0.8f, 0.8f, 1f);
        }
    }

    private void ExecuteUpgradePurchase(ShopItemData item)
    {
        if (item == null) return;

        if (string.Equals(item.id, "upgrade_electricity", System.StringComparison.OrdinalIgnoreCase) ||
            item.id.ToLower().Contains("electricity"))
        {
            GameSaveSystem.Instance.AddElectricityLevel(1);
            int newLevel = GameSaveSystem.Instance.ElectricityLevel;
            int newMaxElec = 100 + ((newLevel - 1) * 50);
            Debug.Log($"[ShopManager] Electricity upgraded to Level {newLevel}. Max Electricity is now {newMaxElec}.");
        }
    }

    private void AddPurchasedRoomToInventory(ShopItemData item)
    {
        if (item == null || !string.Equals(item.category, "Room", System.StringComparison.OrdinalIgnoreCase))
            return;

        string roomTypeId = "";
        string displayName = item.title;

        string idLower = (item.id ?? "").ToLower();
        string titleLower = (item.title ?? "").ToLower();

        if (idLower.Contains("hall") || titleLower.Contains("hall"))
        {
            roomTypeId = "HallRoom";
            displayName = "Hall Room";
        }
        else if (idLower.Contains("lift") || titleLower.Contains("lift"))
        {
            roomTypeId = "Lift";
            displayName = "Lift";
        }
        else if (idLower.Contains("main") || titleLower.Contains("main"))
        {
            roomTypeId = "MainRoom";
            displayName = "Main Hall";
        }
        else if (idLower.Contains("botanist") || titleLower.Contains("botanist"))
        {
            roomTypeId = "DivisionBotanist";
            displayName = "Botanist Room";
        }
        else if (idLower.Contains("containment") || titleLower.Contains("containment"))
        {
            roomTypeId = "ContainmentRoom";
            displayName = "Containment Room";
        }
        else
        {
            roomTypeId = item.id;
        }

        RoomInventorySaveSystem saveSys = RoomInventorySaveSystem.Instance;
        if (saveSys == null) saveSys = FindFirstObjectByType<RoomInventorySaveSystem>();
        if (saveSys == null)
        {
            GameObject go = new GameObject("RoomInventorySaveSystem");
            saveSys = go.AddComponent<RoomInventorySaveSystem>();
        }

        if (saveSys != null)
        {
            saveSys.AddRoomStock(roomTypeId, displayName, 1);
        }
    }

    private void AddPurchasedPlantToInventory(ShopItemData item)
    {
        if (item == null || !string.Equals(item.category, "Seed", System.StringComparison.OrdinalIgnoreCase))
            return;

        string plantId = item.title;

        PlantInventorySaveSystem saveSys = PlantInventorySaveSystem.Instance;
        if (saveSys == null) saveSys = FindFirstObjectByType<PlantInventorySaveSystem>();
        if (saveSys == null)
        {
            GameObject go = new GameObject("PlantInventorySaveSystem");
            saveSys = go.AddComponent<PlantInventorySaveSystem>();
        }

        if (saveSys != null)
        {
            saveSys.AddPlantStock(plantId);
        }
    }

    private void OnAddMoneyClicked()
    {
        GameSaveSystem.Instance.AddMoney(500);
        ShowToast("<color=#FFFF55>Added +500 Gold (Debug)</color>");
    }

    private void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = $"Gold: <color=#FFD700>{currentMoney}</color>";
        }
    }

    public void ShowToast(string message)
    {
        if (toastMessageText == null) return;

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastMessageText.text = message;
        toastMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        toastMessageText.gameObject.SetActive(false);
    }
}
