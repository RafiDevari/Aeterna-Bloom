using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class RoomInventoryItemSaveData
{
    public string roomTypeId;
    public string displayName;
    public int count;

    public RoomInventoryItemSaveData() { }

    public RoomInventoryItemSaveData(string typeId, string name, int initialCount)
    {
        roomTypeId = typeId;
        displayName = name;
        count = initialCount;
    }
}

[System.Serializable]
public class RoomInventoryData
{
    public List<RoomInventoryItemSaveData> items = new List<RoomInventoryItemSaveData>();
}

/// <summary>
/// Sistem Save & Load Inventaris Room berbasis JSON (room_inventory.json).
/// Mengelola stok room yang dimiliki player secara persisten.
/// </summary>
public class RoomInventorySaveSystem : MonoBehaviour
{
    public static RoomInventorySaveSystem Instance { get; private set; }

    private const string FILE_NAME = "room_inventory.json";
    private const string RESOURCE_PATH = "room_inventory";

    private RoomInventoryData currentData;

    public RoomInventoryData CurrentData => currentData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadInventory();
    }

    /// <summary>
    /// Memuat data inventaris room dari room_inventory.json di persistentDataPath atau Resources.
    /// </summary>
    public RoomInventoryData LoadInventory()
    {
        string jsonText = "";

#if UNITY_EDITOR
        // Di Unity Editor, prioritaskan membaca langsung dari file project Assets/Resources/room_inventory.json
        // agar setiap editan manual user pada file di VS Code/Unity langsung terbaca 100%!
        string editorResourcesPath = Path.Combine(Application.dataPath, "Resources", FILE_NAME);
        if (File.Exists(editorResourcesPath))
        {
            jsonText = File.ReadAllText(editorResourcesPath);
            Debug.Log($"[RoomInventorySaveSystem] Loading inventory directly from project: {editorResourcesPath}");
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
                Debug.Log($"[RoomInventorySaveSystem] Loading inventory from persistent path: {savePath}");
            }
            else
            {
                TextAsset resourceAsset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (resourceAsset != null)
                {
                    jsonText = resourceAsset.text;
                    Debug.Log($"[RoomInventorySaveSystem] Loading inventory from Resources/{RESOURCE_PATH}");
                }
            }
        }

        if (!string.IsNullOrEmpty(jsonText))
        {
            currentData = JsonUtility.FromJson<RoomInventoryData>(jsonText);
        }

        if (currentData == null || currentData.items == null || currentData.items.Count == 0)
        {
            currentData = GetDefaultInventoryData();
            SaveInventory(currentData);
        }

        return currentData;
    }

    /// <summary>
    /// Menyimpan data inventaris ke file room_inventory.json.
    /// </summary>
    public void SaveInventory(RoomInventoryData data)
    {
        currentData = data;
        string jsonString = JsonUtility.ToJson(currentData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        File.WriteAllText(persistentPath, jsonString);
        Debug.Log($"[RoomInventorySaveSystem] Saved inventory to persistent path: {persistentPath}");

        // Also save to Assets/Resources/ in Unity Editor
#if UNITY_EDITOR
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }
        string resourcesPath = Path.Combine(resourcesDir, FILE_NAME);
        File.WriteAllText(resourcesPath, jsonString);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"[RoomInventorySaveSystem] Saved inventory to Resources: {resourcesPath}");
#endif
    }

    /// <summary>
    /// Menyimpan data dari list RoomInventoryItemData di RoomCreatorManager.
    /// </summary>
    public void SaveFromItemDataList(List<RoomInventoryItemData> itemDataList)
    {
        RoomInventoryData data = new RoomInventoryData();
        foreach (var item in itemDataList)
        {
            if (item == null) continue;

            string typeId = item.displayName.Replace(" ", "");
            if (item.roomPrefab != null)
            {
                Room rComp = item.roomPrefab.GetComponent<Room>();
                if (rComp != null) typeId = rComp.GetType().Name;
            }

            data.items.Add(new RoomInventoryItemSaveData(typeId, item.displayName, item.count));
        }

        SaveInventory(data);
    }

    /// <summary>
    /// Menambah stok room ke inventaris dan menyimpan ke room_inventory.json.
    /// </summary>
    public void AddRoomStock(string roomTypeId, string displayName, int amount = 1)
    {
        LoadInventory();

        if (currentData == null) currentData = new RoomInventoryData();
        if (currentData.items == null) currentData.items = new List<RoomInventoryItemSaveData>();

        RoomInventoryItemSaveData existingItem = currentData.items.Find(x => 
            string.Equals(x.roomTypeId, roomTypeId, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.displayName, displayName, System.StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            existingItem.count += amount;
        }
        else
        {
            currentData.items.Add(new RoomInventoryItemSaveData(roomTypeId, displayName, amount));
        }

        SaveInventory(currentData);
        Debug.Log($"[RoomInventorySaveSystem] Added +{amount} stock for room '{roomTypeId}' ({displayName}). New count: {(existingItem != null ? existingItem.count : amount)}");
    }

    /// <summary>
    /// Me-reset inventaris ke data default (4 Hall Room, 1 Main Hall, 4 Botanist Room, 2 Lift).
    /// </summary>
    public void ResetInventoryToDefault()
    {
        currentData = GetDefaultInventoryData();
        SaveInventory(currentData);
    }

    public static RoomInventoryData GetDefaultInventoryData()
    {
        return new RoomInventoryData
        {
            items = new List<RoomInventoryItemSaveData>
            {
                new RoomInventoryItemSaveData("HallRoom", "Hall Room", 4),
                new RoomInventoryItemSaveData("MainRoom", "Main Hall", 1),
                new RoomInventoryItemSaveData("DivisionBotanist", "Botanist Room", 4),
                new RoomInventoryItemSaveData("Lift", "Lift", 2),
                new RoomInventoryItemSaveData("ContainmentRoom", "Containment Room", 2)
            }
        };
    }
}
