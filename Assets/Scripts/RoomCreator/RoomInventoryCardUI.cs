using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Card / Box untuk item room di inventaris (misal: "Hall Room (4)").
/// Mendukung Drag & Drop untuk mulai merakit room ke dalam scene.
/// </summary>
public class RoomInventoryCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private Text legacyText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private CanvasGroup canvasGroup;

    private RoomInventoryItemData itemData;
    private RoomCreatorManager manager;

    public RoomInventoryItemData ItemData => itemData;

    public void Setup(RoomInventoryItemData data, RoomCreatorManager managerRef)
    {
        itemData = data;
        manager = managerRef;
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (itemData == null) return;

        string labelString = $"{itemData.displayName} ({itemData.count})";

        if (tmpText != null)
        {
            tmpText.text = labelString;
        }
        else if (legacyText != null)
        {
            legacyText.text = labelString;
        }
        else
        {
            // Try get component automatically if not assigned
            var foundTmp = GetComponentInChildren<TextMeshProUGUI>();
            if (foundTmp != null)
            {
                tmpText = foundTmp;
                tmpText.text = labelString;
            }
            else
            {
                var foundLegacy = GetComponentInChildren<Text>();
                if (foundLegacy != null)
                {
                    legacyText = foundLegacy;
                    legacyText.text = labelString;
                }
            }
        }

        // Jika jumlah 0, buat tampilan agak redup / disabled
        if (canvasGroup != null)
        {
            canvasGroup.alpha = itemData.count > 0 ? 1.0f : 0.4f;
            canvasGroup.blocksRaycasts = itemData.count > 0;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Standby for drag
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;
        manager.StartDraggingRoom(itemData, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;
        manager.UpdateDraggingRoom(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;
        manager.DropDraggingRoom(eventData);
    }
}
