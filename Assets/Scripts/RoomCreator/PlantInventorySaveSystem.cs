using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Sistem Save & Load Inventaris Plant/Tanaman berbasis JSON (plant_inventory.json).
/// Mengelola stok tanaman yang dimiliki player secara persisten.
/// </summary>
public class PlantInventorySaveSystem : MonoBehaviour
{
    public static PlantInventorySaveSystem Instance { get; private set; }

    private const string FILE_NAME = "plant_inventory.json";
    private const string RESOURCE_PATH = "plant_inventory";

    private PlantInventoryData currentData;

    public PlantInventoryData CurrentData => currentData;

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
    /// Memuat data inventaris plant dari plant_inventory.json di persistentDataPath atau Resources.
    /// </summary>
    public PlantInventoryData LoadInventory()
    {
        string jsonText = "";

#if UNITY_EDITOR
        // Di Unity Editor, prioritaskan membaca langsung dari file project Assets/Resources/plant_inventory.json
        string editorResourcesPath = Path.Combine(Application.dataPath, "Resources", FILE_NAME);
        if (File.Exists(editorResourcesPath))
        {
            jsonText = File.ReadAllText(editorResourcesPath);
            Debug.Log($"[PlantInventorySaveSystem] Loading inventory directly from project: {editorResourcesPath}");
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
                Debug.Log($"[PlantInventorySaveSystem] Loading inventory from persistent path: {savePath}");
            }
            else
            {
                TextAsset resourceAsset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (resourceAsset != null)
                {
                    jsonText = resourceAsset.text;
                    Debug.Log($"[PlantInventorySaveSystem] Loading inventory from Resources/{RESOURCE_PATH}");
                }
            }
        }

        if (!string.IsNullOrEmpty(jsonText))
        {
            try
            {
                currentData = JsonUtility.FromJson<PlantInventoryData>(jsonText);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlantInventorySaveSystem] Failed to parse plant inventory JSON: {ex.Message}");
            }
        }

        if (currentData == null || currentData.plants == null)
        {
            currentData = GetDefaultInventoryData();
            SaveInventory(currentData);
        }
        else
        {
            // Auto-upgrade old default stock items if their growth is 0.0f
            bool updated = false;
            foreach (var plant in currentData.plants)
            {
                if (plant != null && plant.growth <= 0f)
                {
                    if (plant.plantInstanceId == "Room_Unit-A1") { plant.growth = 0.7f; updated = true; }
                    else if (plant.plantInstanceId == "Room_Unit-A2") { plant.growth = 1.2f; updated = true; }
                    else if (plant.plantInstanceId == "Room_Unit-A3") { plant.growth = 1.02f; updated = true; }
                }
            }
            if (updated) SaveInventory(currentData);
        }

        return currentData;
    }

    /// <summary>
    /// Menyimpan data inventaris ke file plant_inventory.json.
    /// </summary>
    public void SaveInventory(PlantInventoryData data)
    {
        currentData = data;
        string jsonString = JsonUtility.ToJson(currentData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        File.WriteAllText(persistentPath, jsonString);
        Debug.Log($"[PlantInventorySaveSystem] Saved inventory to persistent path: {persistentPath}");

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
        Debug.Log($"[PlantInventorySaveSystem] Saved inventory to Resources: {resourcesPath}");
#endif
    }

    /// <summary>
    /// Menambah tanaman baru ke inventaris dan menyimpan ke plant_inventory.json.
    /// </summary>
    public void AddPlantStock(string plantId, string plantInstanceId = null)
    {
        LoadInventory();

        if (currentData == null) currentData = new PlantInventoryData();
        if (currentData.plants == null) currentData.plants = new List<PlantInventoryItemData>();

        if (string.IsNullOrEmpty(plantInstanceId))
        {
            int nextNumber = currentData.plants.Count + 1;
            plantInstanceId = $"Room_Unit-A{nextNumber}";

            // Ensure uniqueness
            while (currentData.plants.Exists(p => p != null && p.plantInstanceId == plantInstanceId))
            {
                nextNumber++;
                plantInstanceId = $"Room_Unit-A{nextNumber}";
            }
        }

        PlantInventoryItemData newItem = new PlantInventoryItemData
        {
            plantInstanceId = plantInstanceId,
            plantId = plantId,
            growth = 0.0f,
            completedResearchIds = new List<string>()
        };

        currentData.plants.Add(newItem);
        SaveInventory(currentData);

        Debug.Log($"[PlantInventorySaveSystem] Added plant '{plantId}' with Instance ID '{plantInstanceId}' to inventory. Total plants: {currentData.plants.Count}");
    }

    public static PlantInventoryData GetDefaultInventoryData()
    {
        return new PlantInventoryData
        {
            plants = new List<PlantInventoryItemData>
            {
                new PlantInventoryItemData { plantInstanceId = "Room_Unit-A1", plantId = "Sunny Flower", growth = 0.7f, completedResearchIds = new List<string>() },
                new PlantInventoryItemData { plantInstanceId = "Room_Unit-A2", plantId = "Sunny Flower", growth = 1.2f, completedResearchIds = new List<string>() },
                new PlantInventoryItemData { plantInstanceId = "Room_Unit-A3", plantId = "Dandelectric", growth = 1.02f, completedResearchIds = new List<string>() }
            }
        };
    }
}
