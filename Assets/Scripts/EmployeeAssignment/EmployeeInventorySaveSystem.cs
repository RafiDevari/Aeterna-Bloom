using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class EmployeeInventoryItemSaveData
{
    public string employeeName;
    public string employeePrefabName; // e.g. EmployeeBotanist, EmployeeResearcher, etc.
    public string hair = "Hair";
    public string hairColor = "#FFFFFF";
    public string colorHair; // alias support for JSON
    public string body = "Suit01";
    public string bodyColor = "#FFFFFF";
    public string colorBody; // alias support for JSON

    public EmployeeInventoryItemSaveData() { }

    public EmployeeInventoryItemSaveData(string name, string prefabName, string hair = "Hair", string hairColor = "#FFFFFF", string body = "Suit01", string bodyColor = "#FFFFFF")
    {
        employeeName = name;
        employeePrefabName = prefabName;
        this.hair = hair;
        this.hairColor = hairColor;
        this.body = body;
        this.bodyColor = bodyColor;
    }

    public string GetHair()
    {
        return !string.IsNullOrEmpty(hair) ? hair : "Hair";
    }

    public string GetHairColorHex()
    {
        if (!string.IsNullOrEmpty(hairColor)) return hairColor;
        if (!string.IsNullOrEmpty(colorHair)) return colorHair;
        return "#FFFFFF";
    }

    public string GetBody()
    {
        return !string.IsNullOrEmpty(body) ? body : "Suit01";
    }

    public string GetBodyColorHex()
    {
        if (!string.IsNullOrEmpty(bodyColor)) return bodyColor;
        if (!string.IsNullOrEmpty(colorBody)) return colorBody;
        return "#FFFFFF";
    }
}

[System.Serializable]
public class EmployeeInventoryData
{
    public List<EmployeeInventoryItemSaveData> employees = new List<EmployeeInventoryItemSaveData>();
}

/// <summary>
/// Sistem Save & Load Inventaris Employee berbasis JSON (employee_inventory.json).
/// Mengelola daftar employee yang dimiliki player secara persisten.
/// </summary>
public class EmployeeInventorySaveSystem : MonoBehaviour
{
    public static EmployeeInventorySaveSystem Instance { get; private set; }

    private const string FILE_NAME = "employee_inventory.json";
    private const string RESOURCE_PATH = "employee_inventory";

    private EmployeeInventoryData currentData;

    public EmployeeInventoryData CurrentData => currentData;

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
    /// Memuat data inventaris employee dari employee_inventory.json di persistentDataPath atau Resources.
    /// </summary>
    public EmployeeInventoryData LoadInventory()
    {
        string jsonText = "";

#if UNITY_EDITOR
        // Di Unity Editor, prioritaskan membaca langsung dari file project Assets/Resources/employee_inventory.json
        string editorResourcesPath = Path.Combine(Application.dataPath, "Resources", FILE_NAME);
        if (File.Exists(editorResourcesPath))
        {
            jsonText = File.ReadAllText(editorResourcesPath);
            Debug.Log($"[EmployeeInventorySaveSystem] Loading inventory directly from project: {editorResourcesPath}");
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
                Debug.Log($"[EmployeeInventorySaveSystem] Loading inventory from persistent path: {savePath}");
            }
            else
            {
                TextAsset resourceAsset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (resourceAsset != null)
                {
                    jsonText = resourceAsset.text;
                    Debug.Log($"[EmployeeInventorySaveSystem] Loading inventory from Resources/{RESOURCE_PATH}");
                }
            }
        }

        if (!string.IsNullOrEmpty(jsonText))
        {
            currentData = JsonUtility.FromJson<EmployeeInventoryData>(jsonText);
        }

        if (currentData == null || currentData.employees == null || currentData.employees.Count == 0)
        {
            currentData = GetDefaultInventoryData();
            SaveInventory(currentData);
        }

        return currentData;
    }

    /// <summary>
    /// Menyimpan data inventaris ke file employee_inventory.json.
    /// </summary>
    public void SaveInventory(EmployeeInventoryData data)
    {
        currentData = data;
        string jsonString = JsonUtility.ToJson(currentData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        File.WriteAllText(persistentPath, jsonString);
        Debug.Log($"[EmployeeInventorySaveSystem] Saved inventory to persistent path: {persistentPath}");

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
        Debug.Log($"[EmployeeInventorySaveSystem] Saved inventory to Resources: {resourcesPath}");
#endif
    }

    /// <summary>
    /// Me-reset inventaris ke data default.
    /// </summary>
    public void ResetInventoryToDefault()
    {
        currentData = GetDefaultInventoryData();
        SaveInventory(currentData);
    }

    public static EmployeeInventoryData GetDefaultInventoryData()
    {
        return new EmployeeInventoryData
        {
            employees = new List<EmployeeInventoryItemSaveData>
            {
                new EmployeeInventoryItemSaveData("Bob", "EmployeeBotanist", "Hair", "#7CFC00", "Botanist", "#FFFFFF"),
                new EmployeeInventoryItemSaveData("Alice", "EmployeeBotanist", "Hair2", "#FFD700", "Botanist", "#FFFFFF"),
                new EmployeeInventoryItemSaveData("Charlie", "EmployeeSecurity", "Hair", "#1A1A1A", "Suit01", "#336699"),
                new EmployeeInventoryItemSaveData("Daniel", "EmployeeMedic", "Hair2", "#8B0000", "Suit01", "#FFFFFF"),
                new EmployeeInventoryItemSaveData("Edward", "EmployeeEngineer", "Hair", "#FF8C00", "Suit01", "#E6E6FA"),
                new EmployeeInventoryItemSaveData("Faelantern", "EmployeeResearcher", "Hair2", "#EE82EE", "Researcher", "#0000FF"),
                new EmployeeInventoryItemSaveData("Garion", "EmployeeEngineer", "Hair", "#2E8B57", "Suit01", "#DAA520"),
                new EmployeeInventoryItemSaveData("Harrold", "EmployeeEngineer", "Hair2", "#708090", "Suit01", "#4682B4"),
                new EmployeeInventoryItemSaveData("Ina", "EmployeeEngineer", "Hair", "#FF69B4", "Suit01", "#8A2BE2")
            }
        };
    }
}
