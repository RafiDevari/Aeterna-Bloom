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

    [Header("Room Prefabs")]
    [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();
    private Dictionary<string, GameObject> roomPrefabMap = new Dictionary<string, GameObject>();

    [Header("Employee Prefabs")]
    [SerializeField] private List<GameObject> employeePrefabs = new List<GameObject>();
    private Dictionary<string, GameObject> employeePrefabMap = new Dictionary<string, GameObject>();

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
    private List<GameObject> placedContainmentUnits = new List<GameObject>();
    private HashSet<string> placedPlantInstanceIds = new HashSet<string>();

    private RoomPlacementPreview activePreview;
    private RoomInventoryItemData activeItemData;
    private RoomInventoryCardUI activeCardUI;
    private string activePlantInstanceId;
    private string activePlantId;

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

        // Populate employee prefab map
        foreach (var prefab in employeePrefabs)
        {
            if (prefab == null) continue;
            if (!employeePrefabMap.ContainsKey(prefab.name))
            {
                employeePrefabMap.Add(prefab.name, prefab);
            }
        }

        InitializePrefabMap();
    }

    private void InitializePrefabMap()
    {
        roomPrefabMap.Clear();
        foreach (var prefab in roomPrefabs)
        {
            if (prefab == null) continue;

            if (!roomPrefabMap.ContainsKey(prefab.name))
            {
                roomPrefabMap.Add(prefab.name, prefab);
            }
            if (prefab.name.StartsWith("Prefab_"))
            {
                string shortName = prefab.name.Substring(7);
                if (!roomPrefabMap.ContainsKey(shortName))
                {
                    roomPrefabMap.Add(shortName, prefab);
                }
            }

            Room roomComp = prefab.GetComponent<Room>();
            if (roomComp != null)
            {
                string typeName = roomComp.GetType().Name;
                if (!roomPrefabMap.ContainsKey(typeName))
                {
                    roomPrefabMap.Add(typeName, prefab);
                }
            }
        }
    }

    private void Start()
    {
        InitializeButtons();
        EnsureDefaultInventory();
        RefreshInventoryUI();
        HideConfirmationUI();
        LoadSavedLayout("room_layout_1.json");
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
            testPlayButton.onClick.AddListener(SaveAndProceedToEmployeeAssign);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetLayout);
        }
    }

    /// <summary>
    /// Memuat daftar inventaris room milik user dari room_inventory.json.
    /// </summary>
    public void EnsureDefaultInventory()
    {
        if (roomPrefabMap == null || roomPrefabMap.Count == 0)
        {
            InitializePrefabMap();
        }

        RoomInventoryData savedData = null;

        if (RoomInventorySaveSystem.Instance != null)
        {
            savedData = RoomInventorySaveSystem.Instance.LoadInventory();
        }
        else
        {
            RoomInventorySaveSystem sys = FindFirstObjectByType<RoomInventorySaveSystem>();
            if (sys == null)
            {
                GameObject saveSysObj = new GameObject("RoomInventorySaveSystem");
                sys = saveSysObj.AddComponent<RoomInventorySaveSystem>();
            }
            savedData = sys.LoadInventory();
        }

        if (savedData != null && savedData.items != null && savedData.items.Count > 0)
        {
            inventoryItems.Clear();

            foreach (var item in savedData.items)
            {
                GameObject prefab = FindPrefabForRoom(item.roomTypeId, item.displayName);
                inventoryItems.Add(new RoomInventoryItemData(item.displayName, item.count, prefab));
            }
        }
        else
        {
            inventoryItems = new List<RoomInventoryItemData>
            {
                new RoomInventoryItemData("Hall Room", 4, FindPrefabForRoom("HallRoom", "Hall Room")),
                new RoomInventoryItemData("Main Hall", 2, FindPrefabForRoom("MainRoom", "Main Hall")),
                new RoomInventoryItemData("Botanist Room", 1, FindPrefabForRoom("DivisionBotanist", "Botanist Room")),
                new RoomInventoryItemData("Lift", 2, FindPrefabForRoom("Lift", "Lift")),
                new RoomInventoryItemData("Containment Room", 2, FindPrefabForRoom("ContainmentRoom", "Containment Room"))
            };
        }
    }

    private GameObject FindPrefabForRoom(string roomTypeId, string displayName)
    {
        if (string.IsNullOrEmpty(roomTypeId) && string.IsNullOrEmpty(displayName)) return null;

        if (!string.IsNullOrEmpty(roomTypeId) && roomPrefabMap.TryGetValue(roomTypeId, out GameObject p1) && p1 != null) return p1;
        if (!string.IsNullOrEmpty(displayName) && roomPrefabMap.TryGetValue(displayName, out GameObject p2) && p2 != null) return p2;

        if (!string.IsNullOrEmpty(displayName))
        {
            string cleanDisplayName = displayName.Replace(" ", "");
            if (roomPrefabMap.TryGetValue(cleanDisplayName, out GameObject p3) && p3 != null) return p3;
        }

        GameObject dynamicPrefab = LoadPrefabDynamic(roomTypeId);
        if (dynamicPrefab != null) return dynamicPrefab;

        dynamicPrefab = LoadPrefabDynamic(displayName);
        if (dynamicPrefab != null) return dynamicPrefab;

        dynamicPrefab = LoadPrefabDynamic($"Prefab_{roomTypeId}");
        if (dynamicPrefab != null) return dynamicPrefab;

        return null;
    }

    private GameObject LoadPrefabDynamic(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        GameObject prefab = Resources.Load<GameObject>($"Rooms/{name}");
        if (prefab != null) return prefab;

        prefab = Resources.Load<GameObject>(name);
        if (prefab != null) return prefab;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{name} t:Prefab");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif
        return null;
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
        DisableGameplayScripts(previewObj);

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
            if (hitRoomObj != null)
            {
                if (placedContainmentUnits.Contains(hitRoomObj))
                {
                    StartRepositioningContainmentUnit(hitRoomObj);
                }
                else if (placedRooms.Contains(hitRoomObj))
                {
                    StartRepositioningRoom(hitRoomObj);
                }
            }
        }
    }

    private GameObject GetParentPlacedRoom(GameObject hitObj)
    {
        Transform current = hitObj.transform;
        while (current != null)
        {
            if (placedRooms.Contains(current.gameObject) || placedContainmentUnits.Contains(current.gameObject))
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

    public void StartDraggingContainmentUnit(string plantInstanceId, string plantId, PointerEventData eventData)
    {
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        activePlantInstanceId = plantInstanceId;
        activePlantId = plantId;
        isRepositioningExisting = false;
        isDraggingPreview = true;
        HideConfirmationUI();

        GameObject prefabToSpawn = FindPrefabForRoom("Prefab_ContainmentUnit", "Containment Unit");
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Rooms/Prefab_ContainmentUnit");
        }

        Vector3 mouseWorldPos = GetWorldMousePosition(eventData.position);
        
        bool originalActive = prefabToSpawn.activeSelf;
        prefabToSpawn.SetActive(false);
        GameObject previewObj = Instantiate(prefabToSpawn, mouseWorldPos, Quaternion.identity);
        prefabToSpawn.SetActive(originalActive);

        previewObj.name = $"ContainmentUnit:{plantInstanceId}:{plantId}";
        
        GameObject monsterPrefab = LoadPrefabDynamic(plantId);
        GameObject monsterInstance = null;
        if (monsterPrefab != null)
        {
            bool monsterOriginalActive = monsterPrefab.activeSelf;
            monsterPrefab.SetActive(false);
            monsterInstance = Instantiate(monsterPrefab, previewObj.transform);
            monsterPrefab.SetActive(monsterOriginalActive);
            monsterInstance.name = $"Monster_{plantId}";
            monsterInstance.transform.localPosition = Vector3.zero;
            monsterInstance.transform.localScale = Vector3.one;
        }

        DisableGameplayScripts(previewObj);

        activePreview = previewObj.GetComponent<RoomPlacementPreview>();
        if (activePreview == null)
        {
            activePreview = previewObj.AddComponent<RoomPlacementPreview>();
        }

        activePreview.CacheRenderersAndColliders();

        SpriteRenderer cuRenderer = previewObj.GetComponent<SpriteRenderer>();
        if (cuRenderer != null)
        {
            cuRenderer.sortingOrder = 5;
            if (monsterInstance != null)
            {
                SpriteRenderer[] monsterRenderers = monsterInstance.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var mr in monsterRenderers)
                {
                    if (mr != null) mr.sortingOrder = cuRenderer.sortingOrder + 1;
                }
            }
        }

        previewObj.SetActive(true);
        UpdatePositionForActivePreview();
    }

    private void StartRepositioningContainmentUnit(GameObject cuObj)
    {
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        isRepositioningExisting = true;
        isDraggingPreview = true;
        roomOriginalPos = cuObj.transform.position;

        string[] parts = cuObj.name.Split(':');
        if (parts.Length > 2)
        {
            activePlantInstanceId = parts[1];
            activePlantId = parts[2];
        }

        placedContainmentUnits.Remove(cuObj);
        cuObj.transform.SetParent(null);

        activePreview = cuObj.GetComponent<RoomPlacementPreview>();
        if (activePreview == null)
        {
            activePreview = cuObj.AddComponent<RoomPlacementPreview>();
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

        bool isValid = activePreview.CheckValidity(placedRooms, placedContainmentUnits);
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

        bool isValid = activePreview.CheckValidity(placedRooms, placedContainmentUnits);
        if (!isValid)
        {
            if (activePreview.gameObject.name.Contains("ContainmentUnit"))
            {
                SetStatusMessage("Containment Unit harus diletakkan di dalam Containment Room!");
            }
            else if (activePreview.IsLift)
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

        GameObject confirmedObj = activePreview.gameObject;
        activePreview.ConfirmPlacement();

        if (confirmedObj.name.Contains("ContainmentUnit"))
        {
            // It is a containment unit!
            // Find the containment room it is inside of
            Bounds cuBounds = RoomPlacementPreview.GetAccurateBounds(confirmedObj);
            GameObject parentRoomObj = null;
            foreach (GameObject roomObj in placedRooms)
            {
                if (roomObj == null) continue;
                bool isContainmentRoom = roomObj.GetComponent<ContainmentRoom>() != null || roomObj.name.ToLower().Contains("containmentroom") || roomObj.name.ToLower().Contains("containment room");
                if (isContainmentRoom)
                {
                    Bounds roomBounds = RoomPlacementPreview.GetAccurateBounds(roomObj);
                    if (cuBounds.min.x >= roomBounds.min.x &&
                        cuBounds.max.x <= roomBounds.max.x &&
                        cuBounds.min.y >= roomBounds.min.y &&
                        cuBounds.max.y <= roomBounds.max.y)
                    {
                        parentRoomObj = roomObj;
                        break;
                    }
                }
            }

            if (parentRoomObj != null)
            {
                confirmedObj.transform.SetParent(parentRoomObj.transform, true);
            }

            placedContainmentUnits.Add(confirmedObj);

            if (!isRepositioningExisting && !string.IsNullOrEmpty(activePlantInstanceId))
            {
                placedPlantInstanceIds.Add(activePlantInstanceId);
            }

            // Strip any remaining scripts
            MonoBehaviour[] scripts = confirmedObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in scripts)
            {
                if (script != null) DestroyImmediate(script);
            }
        }
        else
        {
            // It is a room!
            placedRooms.Add(confirmedObj);

            // Hanya kurangi stok jika room baru dari UI card (bukan room lama yang dipindahkan)
            if (!isRepositioningExisting && activeItemData != null)
            {
                activeItemData.count--;

                // Simpan pembaruan stok ke room_inventory.json
                if (RoomInventorySaveSystem.Instance != null)
                {
                    RoomInventorySaveSystem.Instance.SaveFromItemDataList(inventoryItems);
                }
            }
        }

        activePreview = null;
        activeItemData = null;
        activePlantInstanceId = null;
        activePlantId = null;
        isDraggingPreview = false;
        isRepositioningExisting = false;

        HideConfirmationUI();
        RefreshInventoryUI();

        // Refresh left UI panel
        RoomCreatorSetup.RefreshLeftContainmentPanel(placedPlantInstanceIds);
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

                if (roomObj.name.Contains("ContainmentUnit"))
                {
                    // Find target room and parent it back
                    Bounds cuBounds = RoomPlacementPreview.GetAccurateBounds(roomObj);
                    GameObject parentRoomObj = null;
                    foreach (GameObject rObj in placedRooms)
                    {
                        if (rObj == null) continue;
                        bool isContainmentRoom = rObj.GetComponent<ContainmentRoom>() != null || rObj.name.ToLower().Contains("containmentroom") || rObj.name.ToLower().Contains("containment room");
                        if (isContainmentRoom)
                        {
                            Bounds roomBounds = RoomPlacementPreview.GetAccurateBounds(rObj);
                            if (cuBounds.min.x >= roomBounds.min.x &&
                                cuBounds.max.x <= roomBounds.max.x &&
                                cuBounds.min.y >= roomBounds.min.y &&
                                cuBounds.max.y <= roomBounds.max.y)
                            {
                                parentRoomObj = rObj;
                                break;
                            }
                        }
                    }
                    if (parentRoomObj != null)
                    {
                        roomObj.transform.SetParent(parentRoomObj.transform, true);
                    }
                    placedContainmentUnits.Add(roomObj);
                }
                else
                {
                    placedRooms.Add(roomObj);
                }
            }
            else
            {
                // Hapus preview untuk room baru yang batal diletakkan
                activePreview.CancelPlacement();
            }

            activePreview = null;
        }

        activeItemData = null;
        activePlantInstanceId = null;
        activePlantId = null;
        isDraggingPreview = false;
        isRepositioningExisting = false;
        HideConfirmationUI();
        RoomCreatorSetup.RefreshLeftContainmentPanel(placedPlantInstanceIds);
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

            // Serialize division room employees to spawn
            List<EmployeeSaveData> emps = new List<EmployeeSaveData>();
            if (roomComp is DivisionRoom divisionRoom)
            {
                var field = typeof(DivisionRoom).GetField("employeesToSpawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var list = field.GetValue(divisionRoom) as System.Collections.IList;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (item == null) continue;

                            var nameField = item.GetType().GetField("employeeName");
                            var prefabField = item.GetType().GetField("employeePrefab");
                            var suitColorField = item.GetType().GetField("suitColor");
                            var hairColorField = item.GetType().GetField("hairColor");

                            string empName = nameField != null ? (string)nameField.GetValue(item) : "";
                            Employee empPrefab = prefabField != null ? (Employee)prefabField.GetValue(item) : null;
                            Color sColor = suitColorField != null ? (Color)suitColorField.GetValue(item) : Color.white;
                            Color hColor = hairColorField != null ? (Color)hairColorField.GetValue(item) : Color.white;

                            string prefabName = empPrefab != null ? empPrefab.gameObject.name : "";

                            emps.Add(new EmployeeSaveData
                            {
                                employeeName = empName,
                                employeePrefabName = prefabName,
                                suitColor = sColor,
                                hairColor = hColor
                            });
                        }
                    }
                }
            }

            List<ContainmentUnitSaveData> roomCus = new List<ContainmentUnitSaveData>();
            if (typeName == "ContainmentRoom" || typeName.Contains("Containment"))
            {
                foreach (GameObject cuObj in placedContainmentUnits)
                {
                    if (cuObj == null) continue;
                    if (cuObj.transform.parent == roomObj.transform)
                    {
                        string cuName = cuObj.name; // name is "ContainmentUnit:plantInstanceId:plantId"
                        string[] parts = cuName.Split(':');
                        string instanceId = parts.Length > 1 ? parts[1] : "";
                        string plantId = parts.Length > 2 ? parts[2] : "";

                        Vector3 localPos = roomObj.transform.InverseTransformPoint(cuObj.transform.position);

                        roomCus.Add(new ContainmentUnitSaveData
                        {
                            unitName = "Containment Unit",
                            monsterPrefabName = plantId,
                            localPosition = localPos,
                            plantInstanceId = instanceId
                        });
                    }
                }
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
                containmentUnits = roomCus,
                employeesToSpawn = emps
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
    /// Memuat layout room yang sudah di-save sebelumnya ke scene editor agar dapat diedit kembali.
    /// </summary>
    public void LoadSavedLayout(string fileName = "room_layout_1.json")
    {
        foreach (GameObject roomObj in placedRooms)
        {
            if (roomObj != null)
            {
                Destroy(roomObj);
            }
        }
        placedRooms.Clear();

        foreach (GameObject cuObj in placedContainmentUnits)
        {
            if (cuObj != null)
            {
                Destroy(cuObj);
            }
        }
        placedContainmentUnits.Clear();
        placedPlantInstanceIds.Clear();

        string jsonText = "";

#if UNITY_EDITOR
        string editorPath = Path.Combine(Application.dataPath, "Resources", fileName);
        if (File.Exists(editorPath))
        {
            jsonText = File.ReadAllText(editorPath);
            Debug.Log($"[RoomCreatorManager] Loading editor layout from project resources: {editorPath}");
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
                Debug.Log($"[RoomCreatorManager] Loading editor layout from persistent path: {savePath}");
            }
            else
            {
                TextAsset targetAsset = Resources.Load<TextAsset>(fileName.Replace(".json", ""));
                if (targetAsset != null)
                {
                    jsonText = targetAsset.text;
                    Debug.Log($"[RoomCreatorManager] Loading editor layout from Resources/{fileName}");
                }
            }
        }

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.Log("[RoomCreatorManager] No saved layout found to load.");
            return;
        }

        FacilityLayoutData layoutData = JsonUtility.FromJson<FacilityLayoutData>(jsonText);
        if (layoutData == null || layoutData.rooms == null)
        {
            Debug.LogError("[RoomCreatorManager] Failed to parse loaded layout JSON.");
            return;
        }

        foreach (var roomData in layoutData.rooms)
        {
            GameObject prefab = FindPrefabForRoom(roomData.roomType, roomData.roomName);
            if (prefab != null)
            {
                GameObject roomObj = Instantiate(prefab, roomData.position, Quaternion.identity);
                roomObj.transform.localScale = roomData.scale;
                roomObj.name = roomData.roomName;
                DisableGameplayScripts(roomObj);

                // Pastikan preview component tidak aktif/ter-destroy pada room yang di-load
                RoomPlacementPreview previewComp = roomObj.GetComponent<RoomPlacementPreview>();
                if (previewComp != null)
                {
                    Destroy(previewComp);
                }

                Room roomComp = roomObj.GetComponent<Room>();
                if (roomComp != null)
                {
                    roomComp.RoomName = roomData.roomName;
                    roomComp.Temperature = roomData.temperature;
                    roomComp.SetLocked(roomData.isLocked);
                    roomComp.IsPoisoned = roomData.isPoisoned;
                    roomComp.IsSterilizing = roomData.isSterilizing;

                    // Handle division room specific employee mapping
                    if (roomComp is DivisionRoom divisionRoom)
                    {
                        var field = typeof(DivisionRoom).GetField("employeesToSpawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            var nestedType = typeof(DivisionRoom).GetNestedType("EmployeeSpawnData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (nestedType != null)
                            {
                                var spawnListType = typeof(List<>).MakeGenericType(nestedType);
                                var newSpawnList = System.Activator.CreateInstance(spawnListType) as System.Collections.IList;

                                foreach (var empData in roomData.employeesToSpawn)
                                {
                                    if (employeePrefabMap.TryGetValue(empData.employeePrefabName, out GameObject empPrefabGo))
                                    {
                                        Employee empPrefab = empPrefabGo.GetComponent<Employee>();
                                        if (empPrefab != null)
                                        {
                                            object spawnData = System.Activator.CreateInstance(nestedType);
                                            
                                            var nameField = nestedType.GetField("employeeName");
                                            var prefabField = nestedType.GetField("employeePrefab");
                                            var suitColorField = nestedType.GetField("suitColor");
                                            var hairColorField = nestedType.GetField("hairColor");

                                            if (nameField != null) nameField.SetValue(spawnData, empData.employeeName);
                                            if (prefabField != null) prefabField.SetValue(spawnData, empPrefab);
                                            if (suitColorField != null) suitColorField.SetValue(spawnData, empData.suitColor);
                                            if (hairColorField != null) hairColorField.SetValue(spawnData, empData.hairColor);

                                            newSpawnList.Add(spawnData);
                                        }
                                    }
                                }

                                field.SetValue(divisionRoom, newSpawnList);
                            }
                        }
                    }
                }

                // Load containment units if it is a ContainmentRoom
                if (roomData.roomType == "ContainmentRoom" || roomData.roomType.Contains("Containment"))
                {
                    foreach (var unitData in roomData.containmentUnits)
                    {
                        GameObject cuPrefab = FindPrefabForRoom("Prefab_ContainmentUnit", "Containment Unit");
                        if (cuPrefab == null)
                        {
                            cuPrefab = Resources.Load<GameObject>("Rooms/Prefab_ContainmentUnit");
                        }

                        if (cuPrefab != null)
                        {
                            bool originalActive = cuPrefab.activeSelf;
                            cuPrefab.SetActive(false);
                            GameObject cuObj = Instantiate(cuPrefab, roomObj.transform);
                            cuPrefab.SetActive(originalActive);

                            cuObj.name = $"ContainmentUnit:{unitData.plantInstanceId}:{unitData.monsterPrefabName}";
                            cuObj.transform.localPosition = unitData.localPosition;
                            cuObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

                            // Load the monster prefab
                            GameObject monsterPrefab = LoadPrefabDynamic(unitData.monsterPrefabName);
                            GameObject monsterInstance = null;
                            if (monsterPrefab != null)
                            {
                                bool monsterOriginalActive = monsterPrefab.activeSelf;
                                monsterPrefab.SetActive(false);
                                monsterInstance = Instantiate(monsterPrefab, cuObj.transform);
                                monsterPrefab.SetActive(monsterOriginalActive);

                                monsterInstance.name = $"Monster_{unitData.monsterPrefabName}";
                                monsterInstance.transform.localPosition = Vector3.zero;
                                monsterInstance.transform.localScale = Vector3.one;
                            }

                            // Strip scripts
                            MonoBehaviour[] scripts = cuObj.GetComponentsInChildren<MonoBehaviour>(true);
                            foreach (var script in scripts)
                            {
                                if (script != null) DestroyImmediate(script);
                            }

                            // Set sorting orders
                            SpriteRenderer cuRenderer = cuObj.GetComponent<SpriteRenderer>();
                            if (cuRenderer != null)
                            {
                                cuRenderer.sortingOrder = 5;
                                if (monsterInstance != null)
                                {
                                    SpriteRenderer[] monsterRenderers = monsterInstance.GetComponentsInChildren<SpriteRenderer>(true);
                                    foreach (var mr in monsterRenderers)
                                    {
                                        if (mr != null) mr.sortingOrder = cuRenderer.sortingOrder + 1;
                                    }
                                }
                            }

                            cuObj.SetActive(true);
                            placedContainmentUnits.Add(cuObj);

                            if (!string.IsNullOrEmpty(unitData.plantInstanceId))
                            {
                                placedPlantInstanceIds.Add(unitData.plantInstanceId);
                            }
                        }
                    }
                }

                placedRooms.Add(roomObj);
            }
            else
            {
                Debug.LogWarning($"[RoomCreatorManager] Prefab for room type '{roomData.roomType}' name '{roomData.roomName}' not found during load.");
            }
        }

        Debug.Log($"[RoomCreatorManager] Loaded {placedRooms.Count} rooms into editor scene.");
        RoomCreatorSetup.RefreshLeftContainmentPanel(placedPlantInstanceIds);
    }

    /// <summary>
    /// Menyimpan layout ke JSON dan langsung berpindah ke scene EmployeeAssignment untuk menugaskan employee.
    /// </summary>
    public void SaveAndProceedToEmployeeAssign()
    {
        SaveLayoutToJson("room_layout_1.json");
        SaveLayoutToJson("room_layout.json");

        Debug.Log("[RoomCreatorManager] Loading EmployeeAssignment scene...");

#if UNITY_EDITOR
        EnsureBuildSettingsInEditor();
        string scenePath = "Assets/Scenes/EmployeeAssignment.unity";
        if (File.Exists(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
            );
            return;
        }
#endif

        UnityEngine.SceneManagement.SceneManager.LoadScene("EmployeeAssignment");
    }

    /// <summary>
    /// Me-reset seluruh layout room yang sudah diletakkan.
    /// Semua room yang terpasang akan dihapus dan stoknya dikembalikan ke inventaris user.
    /// </summary>
    public void ResetLayout()
    {
        if (activePreview != null)
        {
            OnCancelClicked();
        }

        // Kembalikan setiap room yang ada di scene ke stok inventaris user
        foreach (GameObject roomObj in placedRooms)
        {
            if (roomObj == null) continue;

            // Cari item inventaris yang cocok dan tambahkan kembali stoknya (+1)
            RoomInventoryItemData matchedItem = FindMatchingInventoryItem(roomObj);
            if (matchedItem != null)
            {
                matchedItem.count++;
            }
            else
            {
                string roomName = roomObj.name.Replace("Preview_", "").Replace("(Clone)", "").Trim();
                // Jika jenis room belum ada di list inventaris, buatkan entry baru
                GameObject prefab = FindPrefabForRoom(roomName, roomName);
                inventoryItems.Add(new RoomInventoryItemData(roomName, 1, prefab));
            }

            Destroy(roomObj);
        }
        placedRooms.Clear();

        // Kembalikan & hapus containment units
        foreach (GameObject cuObj in placedContainmentUnits)
        {
            if (cuObj != null)
            {
                Destroy(cuObj);
            }
        }
        placedContainmentUnits.Clear();
        placedPlantInstanceIds.Clear();

        // Simpan stok inventaris yang sudah dikembalikan ke room_inventory.json
        if (RoomInventorySaveSystem.Instance != null)
        {
            RoomInventorySaveSystem.Instance.SaveFromItemDataList(inventoryItems);
        }

        RefreshInventoryUI();
        HideConfirmationUI();
        RoomCreatorSetup.RefreshLeftContainmentPanel(placedPlantInstanceIds);
        SetStatusMessage("Layout di-reset! Semua room yang terpasang telah dikembalikan ke inventaris.");
        Debug.Log("[RoomCreatorManager] Layout reset complete. Placed rooms and containment units cleared.");
    }

    private RoomInventoryItemData FindMatchingInventoryItem(GameObject roomObj)
    {
        if (roomObj == null) return null;
        Room roomComp = roomObj.GetComponent<Room>();
        if (roomComp == null) return null;

        string componentTypeName = roomComp.GetType().Name;

        foreach (var item in inventoryItems)
        {
            if (item == null) continue;

            // Match via associated room prefab's Room component type
            if (item.roomPrefab != null)
            {
                Room prefabRoomComp = item.roomPrefab.GetComponent<Room>();
                if (prefabRoomComp != null && prefabRoomComp.GetType().Name == componentTypeName)
                {
                    return item;
                }
            }

            // Fallback string matching on component type name vs display name
            string typeNameLower = componentTypeName.ToLower();
            string itemLower = item.displayName.ToLower();
            if (itemLower.Contains(typeNameLower) || typeNameLower.Contains(itemLower))
            {
                return item;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    private static void EnsureBuildSettingsInEditor()
    {
        var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        bool modified = false;

        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/RoomCreator.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/EmployeeAssignment.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Gameplay1.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Tutorial.unity");

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

    private void DisableGameplayScripts(GameObject roomObj)
    {
        if (roomObj == null) return;

        // Manually configure BoxCollider2D for the Room since its Start() won't run when disabled
        Room roomComp = roomObj.GetComponent<Room>();
        if (roomComp != null)
        {
            BoxCollider2D col = roomObj.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                SpriteRenderer sr = roomObj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    col.size = sr.sprite.bounds.size;
                    col.offset = sr.sprite.bounds.center;
                }
            }
        }

        MonoBehaviour[] scripts = roomObj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour script in scripts)
        {
            if (script == null) continue;
            if (script is RoomPlacementPreview) continue;

            System.Type type = script.GetType();
            string assemblyName = type.Assembly.GetName().Name;
            if (assemblyName == "Assembly-CSharp" || assemblyName.StartsWith("Assembly-CSharp-firstpass"))
            {
                script.enabled = false;
            }
        }
    }
}
