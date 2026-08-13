using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Manager utama untuk menugaskan Employee ke stasiun kerja (DivisionRoom) secara interaktif.
/// </summary>
public class EmployeeAssignmentManager : MonoBehaviour
{
    public static EmployeeAssignmentManager Instance { get; private set; }

    [Header("Room Prefabs")]
    [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();
    private Dictionary<string, GameObject> roomPrefabMap = new Dictionary<string, GameObject>();

    [Header("Employee Prefabs")]
    [SerializeField] private List<GameObject> employeePrefabs = new List<GameObject>();

    [Header("UI References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button playTestButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("Overlay Config")]
    [SerializeField] private GameObject overlayTextPrefab; // Optional prefab for world text

    private Dictionary<string, GameObject> employeePrefabMap = new Dictionary<string, GameObject>();
    private List<GameObject> placedRooms = new List<GameObject>();
    private List<EmployeeInventoryCardUI> cardUIList = new List<EmployeeInventoryCardUI>();

    // Mapping: Employee Name -> DivisionRoom they are currently assigned to
    private Dictionary<string, DivisionRoom> employeeRoomMap = new Dictionary<string, DivisionRoom>();
    // Mapping: Room -> World Text Gameobject/TextMeshPro
    private Dictionary<Room, TextMeshPro> roomOverlayMap = new Dictionary<Room, TextMeshPro>();

    private EmployeeInventoryCardUI selectedCard;

    private EmployeeInventoryCardUI draggedCard;
    private GameObject dragPreview;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        LoadRoomLayout("room_layout_1.json");
        LoadEmployeeInventory();
        RefreshUI();
    }

    private void Update()
    {
        // Handle clicking on DivisionRooms in the scene
        if (Input.GetMouseButtonDown(0))
        {
            // Skip if clicking UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            TryClickDivisionRoom();
        }
    }

    private void InitializeButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => SaveLayout("room_layout_1.json"));
        }

        if (playTestButton != null)
        {
            playTestButton.onClick.RemoveAllListeners();
            playTestButton.onClick.AddListener(SaveAndPlayTest);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetAllAssignments);
        }
    }

    /// <summary>
    /// Loads the room layout from json file, instantiating them without layout movement capability.
    /// </summary>
    private void LoadRoomLayout(string fileName)
    {
        if (roomPrefabMap == null || roomPrefabMap.Count == 0)
        {
            InitializePrefabMap();
        }

        // Clean up any existing rooms
        foreach (var room in placedRooms)
        {
            if (room != null) Destroy(room);
        }
        placedRooms.Clear();

        // Clear overlays
        foreach (var overlay in roomOverlayMap.Values)
        {
            if (overlay != null) Destroy(overlay.gameObject);
        }
        roomOverlayMap.Clear();
        employeeRoomMap.Clear();

        string jsonText = "";

#if UNITY_EDITOR
        string editorPath = Path.Combine(Application.dataPath, "Resources", fileName);
        if (File.Exists(editorPath))
        {
            jsonText = File.ReadAllText(editorPath);
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
            }
            else
            {
                TextAsset targetAsset = Resources.Load<TextAsset>(fileName.Replace(".json", ""));
                if (targetAsset != null)
                {
                    jsonText = targetAsset.text;
                }
            }
        }

        if (string.IsNullOrEmpty(jsonText))
        {
            SetStatusMessage("No saved room layout found. Build a layout in Room Creator first!");
            return;
        }

        FacilityLayoutData layoutData = JsonUtility.FromJson<FacilityLayoutData>(jsonText);
        if (layoutData == null || layoutData.rooms == null)
        {
            SetStatusMessage("Failed to parse room layout.");
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

                // Strip placement helpers/previews so they are NOT clickable/movable for building
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

                    // Handle division room specific loading
                    if (roomComp is DivisionRoom divisionRoom)
                    {
                        LoadDivisionRoomAssignments(divisionRoom, roomData);
                        CreateRoomOverlay(divisionRoom);
                    }
                    else if (roomComp is ContainmentRoom containmentRoom)
                    {
                        CreateRoomOverlay(roomComp);

                        // Clear any pre-placed containment units first to prevent duplicates
                        var oldUnits = new List<ContainmentUnit>(containmentRoom.ContainmentUnits);
                        foreach (var oldUnit in oldUnits)
                        {
                            if (oldUnit != null) DestroyImmediate(oldUnit.gameObject);
                        }
                        
                        var fieldClear = typeof(ContainmentRoom).GetField("containmentUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fieldClear != null)
                        {
                            var list = fieldClear.GetValue(containmentRoom) as System.Collections.IList;
                            if (list != null) list.Clear();
                        }

                        // Load and instantiate saved containment units
                        foreach (var unitData in roomData.containmentUnits)
                        {
                            GameObject cuPrefab = FindPrefabForRoom("Prefab_ContainmentUnit", "Containment Unit");
                            if (cuPrefab == null)
                            {
                                cuPrefab = LoadPrefabDynamic("Prefab_ContainmentUnit");
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

                                ContainmentUnit unit = cuObj.GetComponent<ContainmentUnit>();
                                if (unit != null)
                                {
                                    unit.UnitName = unitData.unitName;
                                    containmentRoom.AddContainmentUnit(unit);

                                    // Spawn monster visual inside it
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

                                    // Strip other MonoBehaviour scripts except ContainmentUnit so we can click it if needed
                                    MonoBehaviour[] scripts = cuObj.GetComponentsInChildren<MonoBehaviour>(true);
                                    foreach (var script in scripts)
                                    {
                                        if (script != null && !(script is ContainmentUnit))
                                        {
                                            DestroyImmediate(script);
                                        }
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
                                }

                                cuObj.SetActive(true);
                            }
                        }
                    }
                }

                placedRooms.Add(roomObj);
            }
        }

        SetStatusMessage("Room layout loaded successfully!");
        UpdateRoomOverlays();
    }

    private void LoadDivisionRoomAssignments(DivisionRoom divisionRoom, RoomSaveData roomData)
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

                            // Add to local assignment map
                            employeeRoomMap[empData.employeeName] = divisionRoom;
                        }
                    }
                }

                field.SetValue(divisionRoom, newSpawnList);
                divisionRoom.UpdateVisuals();
            }
        }
    }

    private void CreateRoomOverlay(Room room)
    {
        // Create world-space TextMeshPro above the room
        GameObject overlayObj = new GameObject($"Overlay_{room.RoomName}", typeof(TextMeshPro));
        overlayObj.transform.position = room.transform.position + new Vector3(0, 1.8f, 0);
        
        TextMeshPro tmp = overlayObj.GetComponent<TextMeshPro>();
        tmp.fontSize = 4.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Ensure text is rendered on top of rooms
        tmp.sortingOrder = 100;

        roomOverlayMap[room] = tmp;
    }

    private void UpdateRoomOverlays()
    {
        foreach (var pair in roomOverlayMap)
        {
            Room room = pair.Key;
            TextMeshPro tmp = pair.Value;

            if (room == null || tmp == null) continue;

            if (room is DivisionRoom divisionRoom)
            {
                // Find all employees assigned to this room
                List<string> assignedEmps = new List<string>();
                foreach (var mapPair in employeeRoomMap)
                {
                    if (mapPair.Value == divisionRoom)
                    {
                        assignedEmps.Add(mapPair.Key);
                    }
                }

                string cleanRoomName = room.RoomName;
                string listString = assignedEmps.Count > 0 ? string.Join("\n- ", assignedEmps) : "[None]";

                tmp.text = $"<color=#7FC2FF><b>{cleanRoomName}</b></color>\nAssigned:\n- {listString}";
            }
            else
            {
                string cleanRoomName = room.RoomName;
                tmp.text = $"<color=#CCCCCC><b>{cleanRoomName}</b></color>";
            }
        }
    }

    private GameObject FindPrefabForRoom(string roomType, string roomName)
    {
        if (string.IsNullOrEmpty(roomType) && string.IsNullOrEmpty(roomName)) return null;

        if (!string.IsNullOrEmpty(roomType) && roomPrefabMap.TryGetValue(roomType, out GameObject p1) && p1 != null) return p1;
        if (!string.IsNullOrEmpty(roomName) && roomPrefabMap.TryGetValue(roomName, out GameObject p2) && p2 != null) return p2;

        if (!string.IsNullOrEmpty(roomName))
        {
            string cleanDisplayName = roomName.Replace(" ", "");
            if (roomPrefabMap.TryGetValue(cleanDisplayName, out GameObject p3) && p3 != null) return p3;
        }

        // Dynamic fallback loading
        GameObject loaded = Resources.Load<GameObject>($"Rooms/Prefab_{roomType}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>($"Rooms/{roomType}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>($"Prefab_{roomType}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>(roomType);
        if (loaded != null) return loaded;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"Prefab_{roomType} t:Prefab");
        if (guids.Length == 0)
        {
            guids = UnityEditor.AssetDatabase.FindAssets($"{roomType} t:Prefab");
        }
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
        }
#endif

        return null;
    }

    /// <summary>
    /// Loads the employee inventory.
    /// </summary>
    private void LoadEmployeeInventory()
    {
        EmployeeInventorySaveSystem sys = EmployeeInventorySaveSystem.Instance;
        if (sys == null)
        {
            GameObject sysObj = new GameObject("EmployeeInventorySaveSystem");
            sys = sysObj.AddComponent<EmployeeInventorySaveSystem>();
        }

        EmployeeInventoryData invData = sys.LoadInventory();
        
        // Spawn cards
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
        cardUIList.Clear();

        foreach (var emp in invData.employees)
        {
            if (cardPrefab == null) continue;
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            cardObj.transform.localScale = Vector3.one;
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localRotation = Quaternion.identity;
            cardObj.SetActive(true);

            EmployeeInventoryCardUI cardUI = cardObj.GetComponent<EmployeeInventoryCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(emp, this);
                cardUIList.Add(cardUI);
            }
        }
    }

    public string GetAssignedRoomForEmployee(string employeeName)
    {
        if (employeeRoomMap.TryGetValue(employeeName, out DivisionRoom room) && room != null)
        {
            return room.RoomName;
        }
        return null;
    }

    public void SelectEmployeeCard(EmployeeInventoryCardUI card)
    {
        // Unselect previous
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = card;
        if (selectedCard != null)
        {
            selectedCard.SetSelected(true);
            SetStatusMessage($"Selected employee: {selectedCard.ItemData.employeeName}. Click a Division Room to assign.");
        }
        else
        {
            SetStatusMessage("Click on an employee card to select.");
        }
    }

    public void UnassignEmployee(string employeeName)
    {
        if (employeeRoomMap.ContainsKey(employeeName))
        {
            DivisionRoom room = employeeRoomMap[employeeName];
            employeeRoomMap.Remove(employeeName);

            // Update the room's internal list
            if (room != null)
            {
                SyncRoomEmployeesList(room);
            }

            RefreshUI();
            SetStatusMessage($"Unassigned {employeeName}.");
        }
    }

    private void TryClickDivisionRoom()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null)
        {
            DivisionRoom clickedRoom = hit.GetComponentInParent<DivisionRoom>();
            if (clickedRoom != null)
            {
                if (selectedCard == null)
                {
                    SetStatusMessage("Select an employee from the inventory card list first!");
                    return;
                }

                AssignEmployeeToRoom(selectedCard.ItemData, clickedRoom);
            }
        }
    }

    private void AssignEmployeeToRoom(EmployeeInventoryItemSaveData empData, DivisionRoom room)
    {
        // Remove from old room if already assigned
        if (employeeRoomMap.TryGetValue(empData.employeeName, out DivisionRoom oldRoom))
        {
            if (oldRoom == room)
            {
                SetStatusMessage($"{empData.employeeName} is already assigned to {room.RoomName}.");
                return;
            }
        }

        // Count how many employees are currently assigned to the room
        int currentCount = 0;
        foreach (var pair in employeeRoomMap)
        {
            if (pair.Value == room)
            {
                currentCount++;
            }
        }

        if (currentCount >= 5)
        {
            SetStatusMessage($"Cannot assign {empData.employeeName}. {room.RoomName} is full (max 5 employees)!");
            SelectEmployeeCard(null);
            return;
        }

        // Assign to new room
        employeeRoomMap[empData.employeeName] = room;

        // Update old room and new room internal arrays
        if (oldRoom != null) SyncRoomEmployeesList(oldRoom);
        SyncRoomEmployeesList(room);

        // Deselect card
        SelectEmployeeCard(null);

        RefreshUI();
        SetStatusMessage($"Assigned {empData.employeeName} to {room.RoomName}.");
    }

    private void SyncRoomEmployeesList(DivisionRoom room)
    {
        // Find all employees belonging to this room in local map
        List<EmployeeInventoryItemSaveData> assigned = new List<EmployeeInventoryItemSaveData>();
        foreach (var pair in employeeRoomMap)
        {
            if (pair.Value == room)
            {
                EmployeeInventoryItemSaveData foundItem = null;
                foreach (var card in cardUIList)
                {
                    if (card.ItemData != null && card.ItemData.employeeName == pair.Key)
                    {
                        foundItem = card.ItemData;
                        break;
                    }
                }
                if (foundItem != null)
                {
                    assigned.Add(foundItem);
                }
                else
                {
                    assigned.Add(new EmployeeInventoryItemSaveData(pair.Key, "EmployeeBotanist"));
                }
            }
        }

        // Apply via reflection to divisionRoom.employeesToSpawn
        SetRoomEmployeesList(room, assigned);
    }

    private void SetRoomEmployeesList(DivisionRoom room, List<EmployeeInventoryItemSaveData> listData)
    {
        var field = typeof(DivisionRoom).GetField("employeesToSpawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var nestedType = typeof(DivisionRoom).GetNestedType("EmployeeSpawnData", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (nestedType != null)
            {
                var spawnListType = typeof(List<>).MakeGenericType(nestedType);
                var newSpawnList = System.Activator.CreateInstance(spawnListType) as System.Collections.IList;

                foreach (var emp in listData)
                {
                    if (employeePrefabMap.TryGetValue(emp.employeePrefabName, out GameObject prefabGo))
                    {
                        Employee empPrefab = prefabGo.GetComponent<Employee>();
                        if (empPrefab != null)
                        {
                            object spawnData = System.Activator.CreateInstance(nestedType);
                            
                            var nameField = nestedType.GetField("employeeName");
                            var prefabField = nestedType.GetField("employeePrefab");
                            var suitColorField = nestedType.GetField("suitColor");
                            var hairColorField = nestedType.GetField("hairColor");

                            if (nameField != null) nameField.SetValue(spawnData, emp.employeeName);
                            if (prefabField != null) prefabField.SetValue(spawnData, empPrefab);
                            if (suitColorField != null) suitColorField.SetValue(spawnData, emp.suitColor);
                            if (hairColorField != null) hairColorField.SetValue(spawnData, emp.hairColor);

                            newSpawnList.Add(spawnData);
                        }
                    }
                }

                field.SetValue(room, newSpawnList);
                room.UpdateVisuals();
            }
        }
    }

    private void RefreshUI()
    {
        foreach (var card in cardUIList)
        {
            card.UpdateUI();
        }
        UpdateRoomOverlays();
    }

    public void SaveLayout(string fileName = "room_layout_1.json")
    {
        string jsonText = "";

#if UNITY_EDITOR
        string editorPath = Path.Combine(Application.dataPath, "Resources", fileName);
        if (File.Exists(editorPath))
        {
            jsonText = File.ReadAllText(editorPath);
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
            }
            else
            {
                TextAsset targetAsset = Resources.Load<TextAsset>(fileName.Replace(".json", ""));
                if (targetAsset != null)
                {
                    jsonText = targetAsset.text;
                }
            }
        }

        FacilityLayoutData layoutData = null;
        if (!string.IsNullOrEmpty(jsonText))
        {
            layoutData = JsonUtility.FromJson<FacilityLayoutData>(jsonText);
        }

        if (layoutData == null || layoutData.rooms == null)
        {
            layoutData = new FacilityLayoutData
            {
                defaultRoomTemperature = 20f,
                maxElectricity = 100f,
                maxEnergy = 100f,
                rooms = new List<RoomSaveData>()
            };
        }

        foreach (GameObject roomObj in placedRooms)
        {
            if (roomObj == null) continue;

            Room roomComp = roomObj.GetComponent<Room>();
            if (roomComp == null) continue;

            RoomSaveData roomData = null;
            
            // Try position matching first
            foreach (var rData in layoutData.rooms)
            {
                if (rData != null && Vector3.Distance(roomObj.transform.position, rData.position) < 0.05f)
                {
                    roomData = rData;
                    break;
                }
            }

            // Try name matching as fallback
            if (roomData == null)
            {
                foreach (var rData in layoutData.rooms)
                {
                    if (rData != null && rData.roomName == roomComp.RoomName)
                    {
                        roomData = rData;
                        break;
                    }
                }
            }

            if (roomData == null)
            {
                roomData = new RoomSaveData
                {
                    roomType = roomComp.GetType().Name,
                    roomName = roomComp.RoomName,
                    position = roomObj.transform.position,
                    scale = roomObj.transform.localScale,
                    temperature = roomComp.Temperature,
                    isLocked = roomComp.IsLocked,
                    isPoisoned = roomComp.IsPoisoned,
                    isSterilizing = roomComp.IsSterilizing,
                    containmentUnits = new List<ContainmentUnitSaveData>(),
                    employeesToSpawn = new List<EmployeeSaveData>()
                };
                layoutData.rooms.Add(roomData);
            }

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

            roomData.employeesToSpawn = emps;
        }

        string jsonString = JsonUtility.ToJson(layoutData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(persistentPath, jsonString);

        // Save to Assets/Resources for editor
#if UNITY_EDITOR
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }
        string resourcesPath = Path.Combine(resourcesDir, fileName);
        File.WriteAllText(resourcesPath, jsonString);
        UnityEditor.AssetDatabase.Refresh();
#endif

        SetStatusMessage($"Layout and assignments saved to {fileName}!");
    }

    public void SaveAndPlayTest()
    {
        SaveLayout("room_layout_1.json");
        SaveLayout("room_layout.json");

        Debug.Log("[EmployeeAssignmentManager] Launching gameplay play test...");

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

    public void ResetAllAssignments()
    {
        employeeRoomMap.Clear();

        // Clear employeesToSpawn inside room instances
        foreach (var roomObj in placedRooms)
        {
            if (roomObj == null) continue;
            Room roomComp = roomObj.GetComponent<Room>();
            if (roomComp is DivisionRoom divisionRoom)
            {
                SetRoomEmployeesList(divisionRoom, new List<EmployeeInventoryItemSaveData>());
            }
        }

        SelectEmployeeCard(null);
        RefreshUI();
        SetStatusMessage("All employee assignments have been reset.");
    }

    private void SetStatusMessage(string msg)
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = msg;
        }
        Debug.Log($"[EmployeeAssignment] {msg}");
    }

    // =========================================================================
    // DRAG AND DROP EMPLOYEE ASSIGNMENT
    // =========================================================================

    public void StartDraggingEmployee(EmployeeInventoryCardUI card, PointerEventData eventData)
    {
        draggedCard = card;
        SelectEmployeeCard(card); // auto select on drag start
        CreateDragPreview(card.ItemData);
        UpdateDraggingEmployee(eventData);
    }

    public void UpdateDraggingEmployee(PointerEventData eventData)
    {
        if (dragPreview != null)
        {
            dragPreview.transform.position = eventData.position;
        }

        // Raycast to check if hovering over a room
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D hitCol = Physics2D.OverlapPoint(worldMousePos);

        if (hitCol != null)
        {
            DivisionRoom clickedRoom = hitCol.GetComponentInParent<DivisionRoom>();
            if (clickedRoom != null)
            {
                SetStatusMessage($"Hovering over {clickedRoom.RoomName}. Release to assign!");
                return;
            }
        }
        
        if (draggedCard != null)
        {
            SetStatusMessage($"Dragging {draggedCard.ItemData.employeeName}. Drop on a Division Room to assign.");
        }
    }

    public void EndDraggingEmployee(PointerEventData eventData)
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }

        if (draggedCard == null) return;

        // Raycast to see where we dropped the employee
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D hitCol = Physics2D.OverlapPoint(worldMousePos);

        if (hitCol != null)
        {
            DivisionRoom clickedRoom = hitCol.GetComponentInParent<DivisionRoom>();
            if (clickedRoom != null)
            {
                AssignEmployeeToRoom(draggedCard.ItemData, clickedRoom);
                draggedCard = null;
                return;
            }
        }

        SetStatusMessage($"Dropped {draggedCard.ItemData.employeeName} outside a Division Room. Assignment canceled.");
        SelectEmployeeCard(null);
        draggedCard = null;
    }

    private void CreateDragPreview(EmployeeInventoryItemSaveData empData)
    {
        if (dragPreview != null) Destroy(dragPreview);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        dragPreview = new GameObject("DragPreview", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        dragPreview.transform.SetParent(canvas.transform, false);

        RectTransform rt = dragPreview.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 55);

        Image img = dragPreview.GetComponent<Image>();
        string cleanRole = empData.employeePrefabName.Replace("Employee", "");
        img.color = GetRoleColor(cleanRole) * new Color(1, 1, 1, 0.75f); // 75% opacity

        CanvasGroup cg = dragPreview.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // ensure raycast goes through the preview!

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(dragPreview.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = empData.employeeName;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
    }

    private Color GetRoleColor(string role)
    {
        switch (role)
        {
            case "Botanist": return new Color(0.12f, 0.35f, 0.2f, 0.9f);
            case "Researcher": return new Color(0.12f, 0.25f, 0.45f, 0.9f);
            case "Security": return new Color(0.45f, 0.15f, 0.15f, 0.9f);
            case "Medic": return new Color(0.35f, 0.15f, 0.4f, 0.9f);
            case "Engineer": return new Color(0.4f, 0.3f, 0.1f, 0.9f);
            default: return new Color(0.2f, 0.22f, 0.28f, 0.9f);
        }
    }

#if UNITY_EDITOR
    private static void EnsureBuildSettingsInEditor()
    {
        var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        bool modified = false;

        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/EmployeeAssignment.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/Tutorial.unity");

        if (modified)
        {
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
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

    private GameObject LoadPrefabDynamic(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        GameObject loaded = Resources.Load<GameObject>($"Rooms/Prefab_{name}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>($"Rooms/{name}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>($"Prefab_{name}");
        if (loaded != null) return loaded;
        loaded = Resources.Load<GameObject>(name);
        if (loaded != null) return loaded;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"Prefab_{name} t:Prefab");
        if (guids.Length == 0)
        {
            guids = UnityEditor.AssetDatabase.FindAssets($"{name} t:Prefab");
        }
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
        }
#endif
        return null;
    }
}
