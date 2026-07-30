// RoomSaveSystem.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class ContainmentUnitSaveData
{
    public string unitName;
    public string monsterPrefabName;
}

[System.Serializable]
public class EmployeeSaveData
{
    public string employeeName;
    public string employeePrefabName;
}

[System.Serializable]
public class RoomSaveData
{
    public string roomType;
    public string roomName;
    public Vector3 position;
    public Vector3 scale;
    public float temperature;
    public bool isLocked;
    public bool isPoisoned;
    public bool isSterilizing;
    public List<ContainmentUnitSaveData> containmentUnits = new List<ContainmentUnitSaveData>();
    public List<EmployeeSaveData> employeesToSpawn = new List<EmployeeSaveData>();
}

[System.Serializable]
public class FacilityLayoutData
{
    public float defaultRoomTemperature;
    public float maxElectricity;
    public float maxEnergy;
    public List<RoomSaveData> rooms = new List<RoomSaveData>();
}

public class RoomSaveSystem : MonoBehaviour
{
    public static RoomSaveSystem Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string layoutResourcePath = "room_layout";

    [Header("Room Prefabs")]
    [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();

    [Header("Monster Prefabs")]
    [SerializeField] private List<GameObject> monsterPrefabs = new List<GameObject>();

    [Header("Employee Prefabs")]
    [SerializeField] private List<GameObject> employeePrefabs = new List<GameObject>();

    private Dictionary<string, GameObject> roomPrefabMap = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> monsterPrefabMap = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> employeePrefabMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Populate room prefab map
        foreach (var prefab in roomPrefabs)
        {
            if (prefab == null) continue;

            Room roomComp = prefab.GetComponent<Room>();
            if (roomComp != null)
            {
                string typeName = roomComp.GetType().Name;
                if (!roomPrefabMap.ContainsKey(typeName))
                {
                    roomPrefabMap.Add(typeName, prefab);
                }
            }
            if (!roomPrefabMap.ContainsKey(prefab.name))
            {
                roomPrefabMap.Add(prefab.name, prefab);
            }
        }

        // Populate monster prefab map
        foreach (var prefab in monsterPrefabs)
        {
            if (prefab == null) continue;
            if (!monsterPrefabMap.ContainsKey(prefab.name))
            {
                monsterPrefabMap.Add(prefab.name, prefab);
            }
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
    }

    private void Start()
    {
        LoadAndSpawnLayout();
    }

    /// <summary>
    /// Loads layout from Application.persistentDataPath if it exists, falling back to Resources.
    /// </summary>
    public void LoadAndSpawnLayout()
    {
        // 1. Destroy any existing runtime rooms to avoid duplication on reload
        Room[] existingRooms = FindObjectsOfType<Room>();
        foreach (var r in existingRooms)
        {
            if (r != null)
            {
                Destroy(r.gameObject);
            }
        }

        // Clear the Facility rooms list to prepare for new registrations
        if (Facility.Instance != null)
        {
            var roomsField = typeof(Facility).GetField("rooms", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (roomsField != null)
            {
                var roomsList = roomsField.GetValue(Facility.Instance) as List<Room>;
                if (roomsList != null)
                {
                    roomsList.Clear();
                }
            }
        }

        // 2. Load JSON content
        string savePath = Path.Combine(Application.persistentDataPath, "room_layout.json");
        string jsonText = "";

        if (File.Exists(savePath))
        {
            jsonText = File.ReadAllText(savePath);
            Debug.Log($"[RoomSaveSystem] Loading layout from persistent path: {savePath}");
        }
        else
        {
            TextAsset targetAsset = Resources.Load<TextAsset>(layoutResourcePath);
            if (targetAsset != null)
            {
                jsonText = targetAsset.text;
                Debug.Log($"[RoomSaveSystem] Loading default layout from Resources/{layoutResourcePath}");
            }
            else
            {
                Debug.LogError($"[RoomSaveSystem] No layout file found in persistent path or Resources.");
                return;
            }
        }

        FacilityLayoutData layoutData = JsonUtility.FromJson<FacilityLayoutData>(jsonText);
        if (layoutData == null || layoutData.rooms == null)
        {
            Debug.LogError("[RoomSaveSystem] Failed to parse layout JSON or rooms list is null.");
            return;
        }

        // Apply global facility settings if Facility instance exists
        if (Facility.Instance != null)
        {
            Facility.Instance.DefaultRoomTemperature = layoutData.defaultRoomTemperature;
        }

        // 3. Spawn rooms
        foreach (var roomData in layoutData.rooms)
        {
            if (roomPrefabMap.TryGetValue(roomData.roomType, out GameObject prefab))
            {
                GameObject roomObj = Instantiate(prefab, roomData.position, Quaternion.identity);
                roomObj.transform.localScale = roomData.scale;
                roomObj.name = roomData.roomName;

                Room roomComp = roomObj.GetComponent<Room>();
                if (roomComp != null)
                {
                    roomComp.RoomName = roomData.roomName;
                    roomComp.Temperature = roomData.temperature;
                    roomComp.SetLocked(roomData.isLocked);
                    roomComp.IsPoisoned = roomData.isPoisoned;
                    roomComp.IsSterilizing = roomData.isSterilizing;

                    // Handle containment room specific monster mapping
                    if (roomComp is ContainmentRoom containmentRoom)
                    {
                        var units = containmentRoom.ContainmentUnits;
                        for (int i = 0; i < units.Count && i < roomData.containmentUnits.Count; i++)
                        {
                            var unit = units[i];
                            var unitData = roomData.containmentUnits[i];

                            if (monsterPrefabMap.TryGetValue(unitData.monsterPrefabName, out GameObject monsterPrefab))
                            {
                                // Set the unit name
                                unit.UnitName = unitData.unitName;
                                unit.gameObject.name = unitData.unitName;

                                // Set private monsterPrefab via reflection so it gets spawned correctly during Start()
                                var field = typeof(ContainmentUnit).GetField("monsterPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (field != null)
                                {
                                    field.SetValue(unit, monsterPrefab);
                                }
                                else
                                {
                                    // Fallback if reflection fails
                                    unit.SpawnMonsterFromPrefab(monsterPrefab);
                                }
                            }
                        }
                    }

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

                                            if (nameField != null) nameField.SetValue(spawnData, empData.employeeName);
                                            if (prefabField != null) prefabField.SetValue(spawnData, empPrefab);

                                            newSpawnList.Add(spawnData);
                                        }
                                    }
                                }

                                field.SetValue(divisionRoom, newSpawnList);
                            }
                        }
                    }
                }

                Debug.Log($"[RoomSaveSystem] Spawned {roomData.roomName} ({roomData.roomType}) at {roomData.position}");
            }
            else
            {
                Debug.LogWarning($"[RoomSaveSystem] Prefab for room type '{roomData.roomType}' not found in registry.");
            }
        }
    }

    /// <summary>
    /// Saves the current scene room placement configuration to a writable JSON file.
    /// This can be called at runtime in game builds.
    /// </summary>
    public void SaveLayout()
    {
        Room[] rooms = FindObjectsOfType<Room>();
        List<RoomSaveData> roomList = new List<RoomSaveData>();

        foreach (Room room in rooms)
        {
            if (room == null) continue;

            string roomType = room.GetType().Name;
            RoomSaveData data = new RoomSaveData
            {
                roomType = roomType,
                roomName = room.RoomName,
                position = room.transform.position,
                scale = room.transform.localScale,
                temperature = room.Temperature,
                isLocked = room.IsLocked,
                isPoisoned = room.IsPoisoned,
                isSterilizing = room.IsSterilizing
            };

            // Serialize containment units & their assigned monsters
            if (room is ContainmentRoom containmentRoom)
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

                        // Use reflection to get the private monsterPrefab field
                        var field = typeof(ContainmentUnit).GetField("monsterPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            GameObject monsterPrefabGo = (GameObject)field.GetValue(unit);
                            if (monsterPrefabGo != null)
                            {
                                unitData.monsterPrefabName = monsterPrefabGo.name;
                            }
                        }

                        data.containmentUnits.Add(unitData);
                    }
                }
            }

            // Serialize division room employees to spawn
            if (room is DivisionRoom divisionRoom)
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

                            data.employeesToSpawn.Add(new EmployeeSaveData
                            {
                                employeeName = empName,
                                employeePrefabName = prefabName
                            });
                        }
                    }
                }
            }

            roomList.Add(data);
        }

        FacilityLayoutData layoutData = new FacilityLayoutData
        {
            rooms = roomList
        };

        if (Facility.Instance != null)
        {
            layoutData.defaultRoomTemperature = Facility.Instance.DefaultRoomTemperature;
            layoutData.maxElectricity = Facility.Instance.MaxElectricity;
            layoutData.maxEnergy = Facility.Instance.MaxEnergy;
        }
        else
        {
            layoutData.defaultRoomTemperature = 20f;
            layoutData.maxElectricity = 100f;
            layoutData.maxEnergy = 100f;
        }

        string json = JsonUtility.ToJson(layoutData, true);
        string savePath = Path.Combine(Application.persistentDataPath, "room_layout.json");
        File.WriteAllText(savePath, json);
        Debug.Log($"[RoomSaveSystem] Successfully saved runtime layout to {savePath}");
    }
}
