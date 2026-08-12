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

    [SerializeField] private Image seedButtonBg;
    [SerializeField] private Image roomButtonBg;
    [SerializeField] private Image accessoryButtonBg;

    [Header("Carousel Elements")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("Item Card UI Elements (Title Above, Image Center, Price Under)")]
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

        if (prevButton != null) prevButton.onClick.AddListener(OnPrevItemClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextItemClicked);

        if (buyButton != null) buyButton.onClick.AddListener(OnBuyItemClicked);
        if (addMoneyDebugButton != null) addMoneyDebugButton.onClick.AddListener(OnAddMoneyClicked);

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

        UpdateCategoryButtonVisuals();
        RefreshCarouselDisplay();
    }

    private void UpdateCategoryButtonVisuals()
    {
        if (seedButtonBg != null) seedButtonBg.color = (currentCategory == "Seed") ? activeCategoryColor : inactiveCategoryColor;
        if (roomButtonBg != null) roomButtonBg.color = (currentCategory == "Room") ? activeCategoryColor : inactiveCategoryColor;
        if (accessoryButtonBg != null) accessoryButtonBg.color = (currentCategory == "Accessory") ? activeCategoryColor : inactiveCategoryColor;
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

        // Title ABOVE Image
        if (itemTitleText != null)
        {
            itemTitleText.text = item.title;
        }

        // Image in Center
        if (itemImage != null)
        {
            Sprite itemSprite = null;
            if (!string.IsNullOrEmpty(item.spritePath))
            {
                itemSprite = Resources.Load<Sprite>(item.spritePath);
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
        if (itemPriceText != null)
        {
            itemPriceText.text = $"{item.price} Gold";
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
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
        if (currentItemIndex > 0)
        {
            currentItemIndex--;
            RefreshCarouselDisplay();
        }
    }

    private void OnNextItemClicked()
    {
        if (currentItemIndex < currentCategoryItems.Count - 1)
        {
            currentItemIndex++;
            RefreshCarouselDisplay();
        }
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

        if (confirmationPopup != null)
        {
            confirmationPopup.Show(currentItem, () =>
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

        // Try spend money using GameSaveSystem (returns true if successful, false if insufficient funds)
        bool success = GameSaveSystem.Instance.TrySpendMoney(item.price);

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

            ShowToast($"<color=#55FF55>Successfully bought {item.title} for {item.price} Gold!</color>");
            RefreshCarouselDisplay();
        }
        else
        {
            ShowToast($"<color=#FF5555>Not enough money! Need {item.price} Gold (Have {GameSaveSystem.Instance.Money} Gold).</color>");
        }

        return success;
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
