using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopConfirmationPopup : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI promptMessageText;
    [SerializeField] private Image itemPreviewImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    public void Show(ShopItemData item, int actualPrice, Action onConfirm, Action onCancel = null)
    {
        if (item == null) return;

        onConfirmAction = onConfirm;
        onCancelAction = onCancel;

        // Ensure target listeners are fresh and registered
        SetupButtonListeners();

        if (titleText != null)
        {
            titleText.text = "CONFIRM PURCHASE";
        }

        if (promptMessageText != null)
        {
            promptMessageText.text = $"Are you sure you want to buy\n<color=#FFD700><b>{item.title}</b></color>?";
        }

        if (priceText != null)
        {
            priceText.text = $"{actualPrice} Gold";
        }

        if (itemPreviewImage != null)
        {
            Sprite loadedSprite = null;
            if (!string.IsNullOrEmpty(item.spritePath))
            {
                loadedSprite = Resources.Load<Sprite>(item.spritePath);
            }
            itemPreviewImage.sprite = loadedSprite;
            itemPreviewImage.gameObject.SetActive(loadedSprite != null);
        }

        // Activate panel
        if (popupContainer != null)
        {
            popupContainer.SetActive(true);
        }
        gameObject.SetActive(true);
    }

    public void Show(ShopItemData item, Action onConfirm, Action onCancel = null)
    {
        Show(item, item != null ? item.price : 0, onConfirm, onCancel);
    }

    public void Hide()
    {
        if (popupContainer != null)
        {
            popupContainer.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        Debug.Log("[ShopConfirmationPopup] Confirm button clicked!");
        Hide();
        onConfirmAction?.Invoke();
    }

    private void OnCancelClicked()
    {
        Debug.Log("[ShopConfirmationPopup] Cancel button clicked!");
        Hide();
        onCancelAction?.Invoke();
    }
}
