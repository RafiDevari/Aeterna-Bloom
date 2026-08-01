using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manager utama untuk merakit/membuat layout room pada scene roomCreator.
/// Menangani daftar inventaris room user, drag & drop preview, overlap checking, 
/// serta konfirmasi Checklist dan Cancel.
/// </summary>
public class RoomCreatorManager : MonoBehaviour
{
    public static RoomCreatorManager Instance { get; private set; }

    [Header("Inventory Data")]
    [SerializeField] private List<RoomInventoryItemData> inventoryItems = new List<RoomInventoryItemData>();

    [Header("Grid & Placement Settings")]
    [SerializeField] private bool enableRoomSnapping = true;
    [SerializeField] private float roomSnapDistance = 2.5f;
    [SerializeField] private bool enableGridSnap = false;
    [SerializeField] private float gridSnapSize = 0.5f;
    [SerializeField] private Camera mainCamera;

    [Header("Prefabs Room Default (jika belum diassign di Inspector)")]
    [SerializeField] private GameObject hallRoomPrefab;
    [SerializeField] private GameObject mainHallPrefab;
    [SerializeField] private GameObject botanistRoomPrefab;

    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button checklistButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI statusMessageText;
    [SerializeField] private Text legacyStatusMessageText;

    private List<RoomInventoryCardUI> cardUIList = new List<RoomInventoryCardUI>();
    private List<GameObject> placedRooms = new List<GameObject>();

    private RoomPlacementPreview activePreview;
    private RoomInventoryItemData activeItemData;
    private RoomInventoryCardUI activeCardUI;

    public List<RoomInventoryItemData> InventoryItems => inventoryItems;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        InitializeButtons();
        EnsureDefaultInventory();
        RefreshInventoryUI();
        HideConfirmationUI();
    }

    private void InitializeButtons()
    {
        if (checklistButton != null)
        {
            checklistButton.onClick.RemoveAllListeners();
            checklistButton.onClick.AddListener(OnChecklistClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    /// <summary>
    /// Memastikan inventory terisi sesuai contoh user jika masih kosong:
    /// 4 Hall Room, 2 Main Hall, 1 Botanist Room.
    /// </summary>
    public void EnsureDefaultInventory()
    {
        if (inventoryItems == null || inventoryItems.Count == 0)
        {
            inventoryItems = new List<RoomInventoryItemData>
            {
                new RoomInventoryItemData("Hall Room", 4, hallRoomPrefab),
                new RoomInventoryItemData("Main Hall", 2, mainHallPrefab),
                new RoomInventoryItemData("Botanist Room", 1, botanistRoomPrefab)
            };
        }
    }

    /// <summary>
    /// Memperbarui atau membuat kartu UI inventaris di UI Container.
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (cardContainer == null) return;

        // Jika cardUIList sudah sesuai dengan inventoryItems, tinggal updateUI
        if (cardUIList.Count == inventoryItems.Count)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                cardUIList[i].Setup(inventoryItems[i], this);
            }
            return;
        }

        // Hapus child lama jika ada
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
        cardUIList.Clear();

        foreach (var item in inventoryItems)
        {
            GameObject cardObj = cardPrefab != null ? Instantiate(cardPrefab, cardContainer) : CreateDefaultCardUI(item);
            if (cardPrefab != null)
            {
                cardObj.transform.SetParent(cardContainer, false);
            }
            RoomInventoryCardUI cardUI = cardObj.GetComponent<RoomInventoryCardUI>();
            if (cardUI == null)
            {
                cardUI = cardObj.AddComponent<RoomInventoryCardUI>();
            }

            cardUI.Setup(item, this);
            cardUIList.Add(cardUI);
        }
    }

    /// <summary>
    /// Mulai drag room dari UI card item.
    /// </summary>
    public void StartDraggingRoom(RoomInventoryItemData itemData, PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0) return;

        // Batal placement sebelumnya jika belum dikonfirmasi
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        activeItemData = itemData;
        HideConfirmationUI();

        GameObject prefabToSpawn = itemData.roomPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[RoomCreatorManager] Prefab untuk '{itemData.displayName}' null, membuat placeholder.");
            prefabToSpawn = CreatePlaceholderRoomPrefab(itemData.displayName);
        }

        Vector3 mouseWorldPos = GetWorldMousePosition(eventData.position);
        GameObject previewObj = Instantiate(prefabToSpawn, mouseWorldPos, Quaternion.identity);
        previewObj.name = $"Preview_{itemData.displayName}";

        activePreview = previewObj.GetComponent<RoomPlacementPreview>();
        if (activePreview == null)
        {
            activePreview = previewObj.AddComponent<RoomPlacementPreview>();
        }

        activePreview.CacheRenderersAndColliders();
        UpdateDraggingRoom(eventData);
    }

    /// <summary>
    /// Update posisi & warna visual room preview selama di-drag.
    /// </summary>
    public void UpdateDraggingRoom(PointerEventData eventData)
    {
        if (activePreview == null) return;

        Vector3 mouseWorldPos = GetWorldMousePosition(eventData.position);
        activePreview.UpdatePositionWithSnapping(mouseWorldPos, placedRooms, enableRoomSnapping, roomSnapDistance, enableGridSnap, gridSnapSize);

        bool isValid = activePreview.CheckValidity(placedRooms);
        activePreview.SetPreviewState(isValid);
    }

    /// <summary>
    /// Dipanggil ketika mouse/drag dilepas. Membekukan posisi room & menampilkan tombol Checklist & Cancel.
    /// </summary>
    public void DropDraggingRoom(PointerEventData eventData)
    {
        if (activePreview == null) return;

        UpdateDraggingRoom(eventData);
        ShowConfirmationUI();
    }

    /// <summary>
    /// Callback saat tombol Checklist (✔) ditekan oleh user.
    /// </summary>
    public void OnChecklistClicked()
    {
        if (activePreview == null || activeItemData == null) return;

        bool isValid = activePreview.CheckValidity(placedRooms);
        if (!isValid)
        {
            SetStatusMessage("Tidak dapat meletakkan room di sini (bertumpukan dengan room lain)!");
            return;
        }

        // Konfirmasi penempatan room
        GameObject confirmedObj = activePreview.gameObject;
        activePreview.ConfirmPlacement();
        placedRooms.Add(confirmedObj);

        // Kurangi jumlah stok room (misal: Hall Room 4 -> 3)
        activeItemData.count--;

        // Reset state & update UI
        activePreview = null;
        activeItemData = null;

        HideConfirmationUI();
        RefreshInventoryUI();
    }

    /// <summary>
    /// Callback saat tombol Cancel (✖) ditekan oleh user.
    /// </summary>
    public void OnCancelClicked()
    {
        if (activePreview != null)
        {
            activePreview.CancelPlacement();
            activePreview = null;
        }

        activeItemData = null;
        HideConfirmationUI();
    }

    private void ShowConfirmationUI()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }
        SetStatusMessage("");
    }

    private void HideConfirmationUI()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        SetStatusMessage("");
    }

    private void SetStatusMessage(string msg)
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = msg;
        }
        if (legacyStatusMessageText != null)
        {
            legacyStatusMessageText.text = msg;
        }
    }

    private Vector3 GetWorldMousePosition(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;

        float distZ = Mathf.Abs(mainCamera.transform.position.z);
        if (distZ < 0.001f) distZ = 10f;

        Vector3 mousePoint = new Vector3(screenPos.x, screenPos.y, distZ);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePoint);
        worldPos.z = 0f;
        return worldPos;
    }

    private GameObject CreateDefaultCardUI(RoomInventoryItemData item)
    {
        GameObject cardObj = new GameObject($"Card_{item.displayName}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 60);

        Image img = cardObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.28f, 0.95f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(cardObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{item.displayName} ({item.count})";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return cardObj;
    }

    private GameObject CreatePlaceholderRoomPrefab(string name)
    {
        GameObject room = new GameObject(name);
        SpriteRenderer sr = room.AddComponent<SpriteRenderer>();
        // Create simple white square texture for fallback
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);

        BoxCollider2D col = room.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 2f);

        return room;
    }
}
