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
    [SerializeField] private GameObject hallRoomPrefab;
    [SerializeField] private GameObject mainHallPrefab;
    [SerializeField] private GameObject botanistRoomPrefab;
    [SerializeField] private GameObject liftPrefab;

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
    // Mapping: DivisionRoom -> World Text Gameobject/TextMeshPro
    private Dictionary<DivisionRoom, TextMeshPro> roomOverlayMap = new Dictionary<DivisionRoom, TextMeshPro>();

    private EmployeeInventoryCardUI selectedCard;

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

                            if (nameField != null) nameField.SetValue(spawnData, empData.employeeName);
                            if (prefabField != null) prefabField.SetValue(spawnData, empPrefab);

                            newSpawnList.Add(spawnData);

                            // Add to local assignment map
                            employeeRoomMap[empData.employeeName] = divisionRoom;
                        }
                    }
                }

                field.SetValue(divisionRoom, newSpawnList);
            }
        }
    }

    private void CreateRoomOverlay(DivisionRoom room)
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
            DivisionRoom room = pair.Key;
            TextMeshPro tmp = pair.Value;

            if (room == null || tmp == null) continue;

            // Find all employees assigned to this room
            List<string> assignedEmps = new List<string>();
            foreach (var mapPair in employeeRoomMap)
            {
                if (mapPair.Value == room)
                {
                    assignedEmps.Add(mapPair.Key);
                }
            }

            string cleanRoomName = room.RoomName;
            string listString = assignedEmps.Count > 0 ? string.Join("\n- ", assignedEmps) : "[None]";

            tmp.text = $"<color=#7FC2FF><b>{cleanRoomName}</b></color>\nAssigned:\n- {listString}";
        }
    }

    private GameObject FindPrefabForRoom(string roomType, string roomName)
    {
        string nameLower = roomName.ToLower();
        if (nameLower.Contains("hall room") || roomType == "HallRoom") return hallRoomPrefab;
        if (nameLower.Contains("main") || roomType == "MainRoom") return mainHallPrefab;
        if (nameLower.Contains("botanist") || roomType == "DivisionBotanist") return botanistRoomPrefab;
        if (nameLower.Contains("lift") || roomType == "Lift") return liftPrefab;
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
                // Find prefab name from our inventory cards list
                string prefabName = "EmployeeBotanist";
                foreach (var card in cardUIList)
                {
                    if (card.ItemData.employeeName == pair.Key)
                    {
                        prefabName = card.ItemData.employeePrefabName;
                        break;
                    }
                }
                assigned.Add(new EmployeeInventoryItemSaveData(pair.Key, prefabName));
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

                            if (nameField != null) nameField.SetValue(spawnData, emp.employeeName);
                            if (prefabField != null) prefabField.SetValue(spawnData, empPrefab);

                            newSpawnList.Add(spawnData);
                        }
                    }
                }

                field.SetValue(room, newSpawnList);
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
            if (roomComp == null) continue;

            string typeName = roomComp.GetType().Name;
            string displayName = roomComp.RoomName;

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

                            string empName = nameField != null ? (string)nameField.GetValue(item) : "";
                            Employee empPrefab = prefabField != null ? (Employee)prefabField.GetValue(item) : null;
                            string prefabName = empPrefab != null ? empPrefab.gameObject.name : "";

                            emps.Add(new EmployeeSaveData
                            {
                                employeeName = empName,
                                employeePrefabName = prefabName
                            });
                        }
                    }
                }
            }

            RoomSaveData roomData = new RoomSaveData
            {
                roomType = typeName,
                roomName = displayName,
                position = roomObj.transform.position,
                scale = roomObj.transform.localScale,
                temperature = roomComp.Temperature,
                isLocked = roomComp.IsLocked,
                isPoisoned = roomComp.IsPoisoned,
                isSterilizing = roomComp.IsSterilizing,
                containmentUnits = new List<ContainmentUnitSaveData>(),
                employeesToSpawn = emps
            };

            // Containment room units saving
            if (roomComp is ContainmentRoom containmentRoom)
            {
                foreach (var unit in containmentRoom.ContainmentUnits)
                {
                    if (unit != null)
                    {
                        var unitData = new ContainmentUnitSaveData
                        {
                            unitName = unit.UnitName,
                            monsterPrefabName = ""
                        };

                        var field = typeof(ContainmentUnit).GetField("monsterPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            GameObject monsterPrefabGo = (GameObject)field.GetValue(unit);
                            if (monsterPrefabGo != null)
                            {
                                unitData.monsterPrefabName = monsterPrefabGo.name;
                            }
                        }

                        roomData.containmentUnits.Add(unitData);
                    }
                }
            }

            layoutData.rooms.Add(roomData);
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

#if UNITY_EDITOR
    private static void EnsureBuildSettingsInEditor()
    {
        var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        bool modified = false;

        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/EmployeeAssignment.unity");
        modified |= AddBuildSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");

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
}
