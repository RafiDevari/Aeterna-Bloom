using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Card / Box untuk item room di inventaris (misal: "Hall Room (4)").
/// Mendukung Drag & Drop untuk mulai merakit room ke dalam scene.
/// </summary>
public class RoomInventoryCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private Text legacyText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private CanvasGroup canvasGroup;

    private RoomInventoryItemData itemData;
    private RoomCreatorManager manager;

    private ScrollRect parentScrollRect;
    private Vector2 pointerDownPos;
    private bool isDraggingRoom = false;
    private bool isDraggingScroll = false;

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

        parentScrollRect = GetComponentInParent<ScrollRect>();

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
        pointerDownPos = eventData.position;
        isDraggingRoom = false;
        isDraggingScroll = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDraggingScroll || isDraggingRoom) return;
        if (itemData == null || itemData.count <= 0 || manager == null) return;

        manager.StartDraggingRoom(itemData, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;

        if (parentScrollRect == null)
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        Vector2 delta = eventData.position - pointerDownPos;

        // Jika drag ke atas menuju area workspace / delta vertikal dominan -> drag room preview
        if (delta.y > 15f || (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 0.8f && delta.y > 5f))
        {
            isDraggingRoom = true;
            isDraggingScroll = false;
            manager.StartDraggingRoom(itemData, eventData);
        }
        else if (parentScrollRect != null)
        {
            isDraggingScroll = true;
            isDraggingRoom = false;
            parentScrollRect.OnBeginDrag(eventData);
        }
        else
        {
            isDraggingRoom = true;
            manager.StartDraggingRoom(itemData, eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;

        if (isDraggingRoom)
        {
            manager.UpdateDraggingRoom(eventData);
        }
        else if (isDraggingScroll)
        {
            // Jika pointer bergerak keluar dari bar panel bawah ke atas workspace -> alihkan ke drag room
            RectTransform scrollRectRt = parentScrollRect != null ? parentScrollRect.transform as RectTransform : null;
            bool movedOutOfPanel = false;
            if (scrollRectRt != null)
            {
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRectRt, eventData.position, eventData.pressEventCamera, out localPos);
                if (localPos.y > scrollRectRt.rect.height * 0.5f + 10f || localPos.y < -scrollRectRt.rect.height * 0.5f - 10f)
                {
                    movedOutOfPanel = true;
                }
            }
            else if (eventData.position.y - pointerDownPos.y > 30f)
            {
                movedOutOfPanel = true;
            }

            if (movedOutOfPanel)
            {
                if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
                isDraggingScroll = false;
                isDraggingRoom = true;
                manager.StartDraggingRoom(itemData, eventData);
            }
            else if (parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0 || manager == null) return;

        if (isDraggingRoom)
        {
            manager.DropDraggingRoom(eventData);
            isDraggingRoom = false;
        }
        else if (isDraggingScroll)
        {
            if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
            isDraggingScroll = false;
        }
    }
}
