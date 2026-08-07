#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ExportRoomLayoutTool : EditorWindow
{
    [MenuItem("Tools/Save and Export Room Layout")]
    public static void ExportLayoutAndGenerateScene()
    {
        // 1. Scan current scene for all rooms
        Room[] rooms = FindObjectsOfType<Room>();
        if (rooms.Length == 0)
        {
            Debug.LogError("[ExportRoomLayoutTool] No Room objects found in active scene!");
            EditorUtility.DisplayDialog("Error", "No Room objects found in the active scene. Please open Gameplay1 or a valid room scene first.", "OK");
            return;
        }

        // Define folders
        string prefabDir = "Assets/Prefabs/Rooms";
        if (!Directory.Exists(prefabDir))
        {
            Directory.CreateDirectory(prefabDir);
            AssetDatabase.Refresh();
        }

        string resourcesDir = "Assets/Resources";
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
            AssetDatabase.Refresh();
        }

        List<RoomSaveData> roomList = new List<RoomSaveData>();

        // 2. Export Layout data & create Prefabs if they do not exist
        foreach (Room room in rooms)
        {
            string roomType = room.GetType().Name;
            string prefabPath = $"{prefabDir}/Prefab_{roomType}.prefab";

            // If prefab doesn't exist, create it from this instance
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                // Create temporary clone to save as prefab
                GameObject tempGo = Instantiate(room.gameObject);
                tempGo.name = $"Prefab_{roomType}";

                // Clean up runtime elements (like thermometer button to keep the prefab clean,
                // it will be rebuilt by SetupThermometerTool or we keep it if pre-placed)
                Transform therm = tempGo.transform.Find("ThermometerButton");
                if (therm != null)
                {
                    DestroyImmediate(therm.gameObject);
                }

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
                DestroyImmediate(tempGo);
                Debug.Log($"[ExportRoomLayoutTool] Created prefab: {prefabPath}");
            }

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
                        string instanceId = "";
                        if (unit.gameObject.name.Contains(":"))
                        {
                            string[] nameParts = unit.gameObject.name.Split(':');
                            if (nameParts.Length > 1) instanceId = nameParts[1];
                        }

                        var unitData = new ContainmentUnitSaveData
                        {
                            unitName = unit.UnitName,
                            monsterPrefabName = "",
                            localPosition = unit.transform.localPosition,
                            plantInstanceId = instanceId
                        };

                        // Use reflection to get the private monsterPrefab field assigned in Inspector
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
                            var suitColorField = item.GetType().GetField("suitColor");
                            var hairColorField = item.GetType().GetField("hairColor");

                            string empName = nameField != null ? (string)nameField.GetValue(item) : "";
                            Employee empPrefab = prefabField != null ? (Employee)prefabField.GetValue(item) : null;
                            Color sColor = suitColorField != null ? (Color)suitColorField.GetValue(item) : Color.white;
                            Color hColor = hairColorField != null ? (Color)hairColorField.GetValue(item) : Color.white;

                            string prefabName = empPrefab != null ? empPrefab.gameObject.name : "";

                            data.employeesToSpawn.Add(new EmployeeSaveData
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
        string jsonPath = $"{resourcesDir}/room_layout.json";
        File.WriteAllText(jsonPath, json);
        AssetDatabase.Refresh();
        Debug.Log($"[ExportRoomLayoutTool] Exported layout to {jsonPath}");

        // 3. Duplicate scene
        string sourceScenePath = "Assets/Scenes/Gameplay1.unity";
        string targetScenePath = "Assets/Scenes/GameplaySaveLoad.unity";

        if (!File.Exists(sourceScenePath))
        {
            Debug.LogError($"[ExportRoomLayoutTool] Source scene {sourceScenePath} does not exist!");
            EditorUtility.DisplayDialog("Error", $"Source scene {sourceScenePath} not found.", "OK");
            return;
        }

        if (AssetDatabase.CopyAsset(sourceScenePath, targetScenePath))
        {
            Debug.Log($"[ExportRoomLayoutTool] Copied scene to {targetScenePath}");
        }
        else
        {
            Debug.LogError($"[ExportRoomLayoutTool] Failed to copy scene to {targetScenePath}!");
            EditorUtility.DisplayDialog("Error", $"Failed to copy scene to {targetScenePath}.", "OK");
            return;
        }

        // 4. Open new scene and configure it
        var newScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

        // Find and delete all static Room instances in the new scene
        Room[] roomsInNewScene = FindObjectsOfType<Room>();
        int deletedCount = 0;
        foreach (var r in roomsInNewScene)
        {
            if (r != null)
            {
                DestroyImmediate(r.gameObject);
                deletedCount++;
            }
        }
        Debug.Log($"[ExportRoomLayoutTool] Deleted {deletedCount} static rooms in the new scene.");

        // Clear the Facility rooms list in the new scene
        Facility facilityInNewScene = FindObjectOfType<Facility>();
        if (facilityInNewScene != null)
        {
            SerializedObject facSO = new SerializedObject(facilityInNewScene);
            SerializedProperty roomsProp = facSO.FindProperty("rooms");
            if (roomsProp != null)
            {
                roomsProp.ClearArray();
                facSO.ApplyModifiedProperties();
                Debug.Log("[ExportRoomLayoutTool] Cleared static rooms list in Facility component.");
            }
        }

        // Create RoomSaveSystem GameObject and attach component
        GameObject saveSystemGo = new GameObject("RoomSaveSystem");
        RoomSaveSystem saveSystem = saveSystemGo.AddComponent<RoomSaveSystem>();

        // Populate lists on the RoomSaveSystem component
        SerializedObject saveSystemSO = new SerializedObject(saveSystem);
        
        // Find Room Prefabs
        string[] roomPrefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabDir });
        SerializedProperty roomPrefabsProp = saveSystemSO.FindProperty("roomPrefabs");
        roomPrefabsProp.ClearArray();
        int roomIndex = 0;
        foreach (var guid in roomPrefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponent<Room>() != null)
            {
                roomPrefabsProp.InsertArrayElementAtIndex(roomIndex);
                roomPrefabsProp.GetArrayElementAtIndex(roomIndex).objectReferenceValue = go;
                roomIndex++;
            }
        }

        // Find Monster Prefabs
        string[] monsterPrefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/MonsterPrefab" });
        SerializedProperty monsterPrefabsProp = saveSystemSO.FindProperty("monsterPrefabs");
        monsterPrefabsProp.ClearArray();
        int monsterIndex = 0;
        foreach (var guid in monsterPrefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null)
            {
                monsterPrefabsProp.InsertArrayElementAtIndex(monsterIndex);
                monsterPrefabsProp.GetArrayElementAtIndex(monsterIndex).objectReferenceValue = go;
                monsterIndex++;
            }
        }

        // Find Employee Prefabs
        string[] employeePrefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/EmployeePrefab" });
        SerializedProperty employeePrefabsProp = saveSystemSO.FindProperty("employeePrefabs");
        employeePrefabsProp.ClearArray();
        int employeeIndex = 0;
        foreach (var guid in employeePrefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponent<Employee>() != null)
            {
                employeePrefabsProp.InsertArrayElementAtIndex(employeeIndex);
                employeePrefabsProp.GetArrayElementAtIndex(employeeIndex).objectReferenceValue = go;
                employeeIndex++;
            }
        }

        saveSystemSO.ApplyModifiedProperties();
        Debug.Log($"[ExportRoomLayoutTool] Configured RoomSaveSystem with {roomIndex} rooms, {monsterIndex} monsters, and {employeeIndex} employees.");

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(newScene);
        EditorSceneManager.SaveScene(newScene);
        Debug.Log("[ExportRoomLayoutTool] Saved new scene successfully.");

        EditorUtility.DisplayDialog("Generation Successful", 
            $"Successfully exported {rooms.Length} rooms to Resources/room_layout.json!\n\n" +
            $"Created new scene: Assets/Scenes/GameplaySaveLoad.unity\n" +
            $"Static rooms were deleted and RoomSaveSystem was configured. Enter Play Mode to test!", "OK");
    }
}
#endif
