using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class EmployeeInventoryItemSaveData
{
    public string employeeName;
    public string employeePrefabName; // e.g. EmployeeBotanist, EmployeeResearcher, etc.

    public EmployeeInventoryItemSaveData() { }

    public EmployeeInventoryItemSaveData(string name, string prefabName)
    {
        employeeName = name;
        employeePrefabName = prefabName;
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
                new EmployeeInventoryItemSaveData("Bob", "EmployeeBotanist"),
                new EmployeeInventoryItemSaveData("Alice", "EmployeeResearcher"),
                new EmployeeInventoryItemSaveData("Charlie", "EmployeeSecurity"),
                new EmployeeInventoryItemSaveData("Daniel", "EmployeeMedic"),
                new EmployeeInventoryItemSaveData("Edward", "EmployeeEngineer")
            }
        };
    }
}
