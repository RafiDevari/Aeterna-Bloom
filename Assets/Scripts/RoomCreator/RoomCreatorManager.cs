using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manager utama untuk merakit/membuat layout room pada scene roomCreator.
/// Menangani daftar inventaris room user, drag & drop preview, overlap checking,
/// konfirmasi Checklist/Cancel, pemindahan ulang room yang sudah diletakkan,
/// serta fitur Save Layout ke room_layout_1.json.
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
    [SerializeField] private GameObject liftPrefab;

    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button checklistButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button testPlayButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusMessageText;
    [SerializeField] private Text legacyStatusMessageText;

    private List<RoomInventoryCardUI> cardUIList = new List<RoomInventoryCardUI>();
    private List<GameObject> placedRooms = new List<GameObject>();

    private RoomPlacementPreview activePreview;
    private RoomInventoryItemData activeItemData;
    private RoomInventoryCardUI activeCardUI;

    // State pemindahan & dragging preview
    private bool isDraggingPreview = false;
    private bool isRepositioningExisting = false;
    private Vector3 roomOriginalPos;

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

    private void Update()
    {
        // 1. Selama activePreview sedang di-drag mengikuti kursor mouse
        if (activePreview != null && isDraggingPreview)
        {
            UpdatePositionForActivePreview();

            // Saat tombol mouse dilepas (Mouse Up), kunci posisi sementara dan munculkan konfirmasi UI
            if (Input.GetMouseButtonUp(0))
            {
                isDraggingPreview = false; // Berhenti mengikutkan posisi ke kursor!
                ShowConfirmationUI();
            }
        }
        // 2. Jika tidak ada room preview yang aktif/pending konfirmasi, bisa me-pick up room yang diklik
        else if (activePreview == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryPickUpPlacedRoom();
            }
        }
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

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => SaveLayoutToJson("room_layout_1.json"));
        }

        if (testPlayButton != null)
        {
            testPlayButton.onClick.RemoveAllListeners();
            testPlayButton.onClick.AddListener(SaveAndPlayGameplayTest);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetLayout);
        }
    }

    /// <summary>
    /// Memastikan inventory terisi sesuai contoh user jika masih kosong.
    /// </summary>
    public void EnsureDefaultInventory()
    {
        if (inventoryItems == null || inventoryItems.Count == 0)
        {
            inventoryItems = new List<RoomInventoryItemData>
            {
                new RoomInventoryItemData("Hall Room", 4, hallRoomPrefab),
                new RoomInventoryItemData("Main Hall", 2, mainHallPrefab),
                new RoomInventoryItemData("Botanist Room", 1, botanistRoomPrefab),
                new RoomInventoryItemData("Lift", 2, liftPrefab)
            };
        }
    }

    /// <summary>
    /// Memperbarui atau membuat kartu UI inventaris di UI Container.
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (cardContainer == null) return;

        if (cardUIList.Count == inventoryItems.Count)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                cardUIList[i].Setup(inventoryItems[i], this);
            }
            return;
        }

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
    /// Mulai drag room baru dari UI card item.
    /// </summary>
    public void StartDraggingRoom(RoomInventoryItemData itemData, PointerEventData eventData)
    {
        if (itemData == null || itemData.count <= 0) return;

        if (activePreview != null)
        {
            OnCancelClicked();
        }

        activeItemData = itemData;
        isRepositioningExisting = false;
        isDraggingPreview = true;
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
        UpdatePositionForActivePreview();
    }

    /// <summary>
    /// Update posisi & warna visual room preview selama di-drag dari UI card.
    /// </summary>
    public void UpdateDraggingRoom(PointerEventData eventData)
    {
        if (activePreview == null || !isDraggingPreview) return;
        UpdatePositionForActivePreview();
    }

    /// <summary>
    /// Dipanggil ketika mouse/drag dilepas dari UI card.
    /// </summary>
    public void DropDraggingRoom(PointerEventData eventData)
    {
        if (activePreview == null) return;

        isDraggingPreview = false;
        UpdatePositionForActivePreview();
        ShowConfirmationUI();
    }

    // =========================================================================
    // FITUR PEMINDAHAN ULANG ROOM YANG SUDAH DILETAKKAN (CLICK TO REPOSITION)
    // =========================================================================

    private void TryPickUpPlacedRoom()
    {
        // Abaikan jika pointer sedang berada di atas elemen UI (misal tombol/card)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Abaikan jika sedang memunculkan konfirmasi / men-drag preview room lain
        if (activePreview != null) return;

        Vector3 worldMousePos = GetWorldMousePosition(Input.mousePosition);
        Collider2D hitCol = Physics2D.OverlapPoint(worldMousePos);

        if (hitCol != null)
        {
            GameObject hitRoomObj = GetParentPlacedRoom(hitCol.gameObject);
            if (hitRoomObj != null && placedRooms.Contains(hitRoomObj))
            {
                StartRepositioningRoom(hitRoomObj);
            }
        }
    }

    private GameObject GetParentPlacedRoom(GameObject hitObj)
    {
        Transform current = hitObj.transform;
        while (current != null)
        {
            if (placedRooms.Contains(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }
        return null;
    }

    private void StartRepositioningRoom(GameObject roomObj)
    {
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        isRepositioningExisting = true;
        isDraggingPreview = true;
        roomOriginalPos = roomObj.transform.position;

        // Keluarkan sementara dari placedRooms agar tidak bertumpukan dengan dirinya sendiri
        placedRooms.Remove(roomObj);

        activePreview = roomObj.GetComponent<RoomPlacementPreview>();
        if (activePreview == null)
        {
            activePreview = roomObj.AddComponent<RoomPlacementPreview>();
        }

        activePreview.CacheRenderersAndColliders();
        HideConfirmationUI();
        UpdatePositionForActivePreview();
    }

    private void UpdatePositionForActivePreview()
    {
        if (activePreview == null) return;

        Vector3 mouseWorldPos = GetWorldMousePosition(Input.mousePosition);
        activePreview.UpdatePositionWithSnapping(mouseWorldPos, placedRooms, enableRoomSnapping, roomSnapDistance, enableGridSnap, gridSnapSize);

        bool isValid = activePreview.CheckValidity(placedRooms);
        activePreview.SetPreviewState(isValid);
    }

    // =========================================================================
    // KONFIRMASI (CHECKLIST & CANCEL)
    // =========================================================================

    /// <summary>
    /// Callback saat tombol Checklist (✔) ditekan oleh user.
    /// </summary>
    public void OnChecklistClicked()
    {
        if (activePreview == null) return;

        bool isValid = activePreview.CheckValidity(placedRooms);
        if (!isValid)
        {
            if (activePreview.IsLift)
            {
                SetStatusMessage("Lift hanya dapat diletakkan di atas atau di bawah Main Hall / Lift!");
            }
            else if (!activePreview.IsMainHall && !activePreview.IsCurrentlySnapped)
            {
                SetStatusMessage("Room ini harus diletakkan menempel pada room lain!");
            }
            else
            {
                SetStatusMessage("Tidak dapat meletakkan room di sini (bertumpukan dengan room lain)!");
            }
            return;
        }

        // Konfirmasi penempatan room di posisi baru
        GameObject confirmedObj = activePreview.gameObject;
        activePreview.ConfirmPlacement();
        placedRooms.Add(confirmedObj);

        // Hanya kurangi stok jika room baru dari UI card (bukan room lama yang dipindahkan)
        if (!isRepositioningExisting && activeItemData != null)
        {
            activeItemData.count--;
        }

        activePreview = null;
        activeItemData = null;
        isDraggingPreview = false;
        isRepositioningExisting = false;

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
            if (isRepositioningExisting)
            {
                // Kembalikan room ke posisi semula sebelum dipindahkan
                activePreview.transform.position = roomOriginalPos;
                Physics2D.SyncTransforms();

                GameObject roomObj = activePreview.gameObject;
                activePreview.ConfirmPlacement();
                placedRooms.Add(roomObj);
            }
            else
            {
                // Hapus preview untuk room baru yang batal diletakkan
                activePreview.CancelPlacement();
            }

            activePreview = null;
        }

        activeItemData = null;
        isDraggingPreview = false;
        isRepositioningExisting = false;
        HideConfirmationUI();
    }

    /// <summary>
    /// Menyimpan semua room yang sudah diletakkan ke file JSON (misal room_layout_1.json).
    /// </summary>
    public void SaveLayoutToJson(string fileName = "room_layout_1.json")
    {
        FacilityLayoutData layoutData = new FacilityLayoutData
        {
            defaultRoomTemperature = 20f,
            maxElectricity = 100f,
            maxEnergy = 100f,
            rooms = new List<RoomSaveData>()
        };

        foreach (GameObject roomObj in placedRooms)
        {
            if (roomObj == null) continue;

            Room roomComp = roomObj.GetComponent<Room>();

            string typeName = "Room";
            string displayName = roomObj.name.Replace("Preview_", "").Replace("(Clone)", "").Trim();

            if (roomComp != null)
            {
                typeName = roomComp.GetType().Name;
                if (!string.IsNullOrEmpty(roomComp.RoomName))
                {
                    displayName = roomComp.RoomName;
                }
            }
            else
            {
                if (displayName.ToLower().Contains("main")) typeName = "MainRoom";
                else if (displayName.ToLower().Contains("lift")) typeName = "Lift";
                else if (displayName.ToLower().Contains("botanist")) typeName = "DivisionBotanist";
                else if (displayName.ToLower().Contains("hall")) typeName = "HallRoom";
            }

            RoomSaveData roomData = new RoomSaveData
            {
                roomType = typeName,
                roomName = displayName,
                position = roomObj.transform.position,
                scale = roomObj.transform.localScale,
                temperature = roomComp != null ? roomComp.Temperature : 20f,
                isLocked = roomComp != null ? roomComp.IsLocked : false,
                isPoisoned = roomComp != null ? roomComp.IsPoisoned : false,
                isSterilizing = roomComp != null ? roomComp.IsSterilizing : false,
                containmentUnits = new List<ContainmentUnitSaveData>(),
                employeesToSpawn = new List<EmployeeSaveData>()
            };

            layoutData.rooms.Add(roomData);
        }

        string jsonString = JsonUtility.ToJson(layoutData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(persistentPath, jsonString);
        Debug.Log($"[RoomCreatorManager] Successfully saved layout to persistent path: {persistentPath}");

        // Also save to Assets/Resources/ for Unity Editor so it's ready to load
#if UNITY_EDITOR
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }
        string resourcesPath = Path.Combine(resourcesDir, fileName);
        File.WriteAllText(resourcesPath, jsonString);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"[RoomCreatorManager] Successfully saved layout to Resources: {resourcesPath}");
#endif

        SetStatusMessage($"Layout berhasil disimpan ke {fileName} ({layoutData.rooms.Count} room)!");
    }

    /// <summary>
    /// Menyimpan layout ke JSON dan langsung berpindah ke scene Gameplay (GameplaySaveLoad) untuk menguji layout.
    /// </summary>
    public void SaveAndPlayGameplayTest()
    {
        SaveLayoutToJson("room_layout_1.json");
        SaveLayoutToJson("room_layout.json");

        Debug.Log("[RoomCreatorManager] Loading GameplaySaveLoad scene for testing...");

#if UNITY_EDITOR
        EnsureBuildSettingsInEditor();
        string scenePath = "Assets/Scenes/GameplaySaveLoad.unity";
        if (File.Exists(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
            );
            return;
        }
#endif

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameplaySaveLoad");
    }

    /// <summary>
    /// Me-reset seluruh layout room yang sudah diletakkan, mengembalikan stok inventaris room ke jumlah awal,
    /// dan membersihkan scene dari room yang sudah diletakkan.
    /// </summary>
    public void ResetLayout()
    {
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        foreach (GameObject roomObj in placedRooms)
        {
            if (roomObj != null)
            {
                Destroy(roomObj);
            }
        }
        placedRooms.Clear();

        inventoryItems.Clear();
        EnsureDefaultInventory();
        RefreshInventoryUI();

        HideConfirmationUI();
        SetStatusMessage("Layout berhasil di-reset!");
        Debug.Log("[RoomCreatorManager] Layout reset complete.");
    }

#if UNITY_EDITOR
    private static void EnsureBuildSettingsInEditor()
    {
        var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        bool modified = false;

        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/RoomCreator.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Gameplay1.unity");

        if (modified)
        {
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[RoomCreatorManager] Updated Build Settings with required scenes.");
        }
    }

    private static bool AddBuildSceneIfMissing(List<UnityEditor.EditorBuildSettingsScene> list, string path)
    {
        if (!File.Exists(path)) return false;
        foreach (var s in list)
        {
            if (s.path == path) return false;
        }
        list.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
        return true;
    }
#endif

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
