using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public int money = 1000;
    public int day = 1;
    public int electricityLevel = 1;
    public List<string> purchasedItemIds = new List<string>();

    public GameData() { }

    public GameData(int initialMoney, int initialDay, int initialElectricityLevel = 1)
    {
        money = initialMoney;
        day = initialDay;
        electricityLevel = initialElectricityLevel;
        purchasedItemIds = new List<string>();
    }
}

/// <summary>
/// Sistem Save & Load Data Game Utama (Money & Day & Electricity Level) berbasis JSON (game_data.json).
/// </summary>
public class GameSaveSystem : MonoBehaviour
{
    private static GameSaveSystem instance;
    public static GameSaveSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSaveSystem>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameSaveSystem");
                    instance = go.AddComponent<GameSaveSystem>();
                }
            }
            return instance;
        }
    }

    public static event System.Action<int> OnMoneyChanged;
    public static event System.Action<int> OnDayChanged;
    public static event System.Action<int> OnElectricityLevelChanged;
    public static event System.Action OnDataLoaded;

    private const string FILE_NAME = "game_data.json";
    private const string RESOURCE_PATH = "game_data";

    private GameData currentData;

    public GameData CurrentData => currentData;

    public int Money => currentData != null ? currentData.money : 0;
    public int Day => currentData != null ? currentData.day : 1;
    public int ElectricityLevel => currentData != null ? Mathf.Max(1, currentData.electricityLevel) : 1;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    /// <summary>
    /// Memuat data game dari game_data.json di persistentDataPath atau Resources.
    /// </summary>
    public GameData LoadData()
    {
        string jsonText = "";

#if UNITY_EDITOR
        // Di Unity Editor, prioritaskan membaca langsung dari file project Assets/Resources/game_data.json
        string editorResourcesPath = Path.Combine(Application.dataPath, "Resources", FILE_NAME);
        if (File.Exists(editorResourcesPath))
        {
            jsonText = File.ReadAllText(editorResourcesPath);
            Debug.Log($"[GameSaveSystem] Loading game data directly from project: {editorResourcesPath}");
        }
#endif

        if (string.IsNullOrEmpty(jsonText))
        {
            string savePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

            if (File.Exists(savePath))
            {
                jsonText = File.ReadAllText(savePath);
                Debug.Log($"[GameSaveSystem] Loading game data from persistent path: {savePath}");
            }
            else
            {
                TextAsset resourceAsset = Resources.Load<TextAsset>(RESOURCE_PATH);
                if (resourceAsset != null)
                {
                    jsonText = resourceAsset.text;
                    Debug.Log($"[GameSaveSystem] Loading game data from Resources/{RESOURCE_PATH}");
                }
            }
        }

        if (!string.IsNullOrEmpty(jsonText))
        {
            currentData = JsonUtility.FromJson<GameData>(jsonText);
        }

        if (currentData == null)
        {
            currentData = GetDefaultData();
            SaveData(currentData);
        }

        if (currentData.electricityLevel < 1)
        {
            currentData.electricityLevel = 1;
        }

        if (currentData.purchasedItemIds == null)
        {
            currentData.purchasedItemIds = new List<string>();
        }

        OnDataLoaded?.Invoke();
        OnMoneyChanged?.Invoke(currentData.money);
        OnDayChanged?.Invoke(currentData.day);
        OnElectricityLevelChanged?.Invoke(currentData.electricityLevel);

        return currentData;
    }

    /// <summary>
    /// Menyimpan data game ke file game_data.json.
    /// </summary>
    public void SaveData(GameData data)
    {
        currentData = data;
        string jsonString = JsonUtility.ToJson(currentData, true);

        // Save to persistentDataPath
        string persistentPath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        File.WriteAllText(persistentPath, jsonString);
        Debug.Log($"[GameSaveSystem] Saved game data to persistent path: {persistentPath}");

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
        Debug.Log($"[GameSaveSystem] Saved game data to Resources: {resourcesPath}");
#endif
    }

    /// <summary>
    /// Menambah jumlah uang dan secara otomatis menyimpan data.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (currentData == null) LoadData();
        currentData.money += amount;
        SaveData(currentData);
        OnMoneyChanged?.Invoke(currentData.money);
    }

    /// <summary>
    /// Mengurangi jumlah uang jika mencukupi. Mengembalikan true jika berhasil.
    /// </summary>
    public bool TrySpendMoney(int amount)
    {
        if (currentData == null) LoadData();
        if (currentData.money < amount) return false;

        currentData.money -= amount;
        SaveData(currentData);
        OnMoneyChanged?.Invoke(currentData.money);
        return true;
    }

    /// <summary>
    /// Mengeset jumlah uang secara langsung.
    /// </summary>
    public void SetMoney(int newMoney)
    {
        if (currentData == null) LoadData();
        currentData.money = newMoney;
        SaveData(currentData);
        OnMoneyChanged?.Invoke(currentData.money);
    }

    /// <summary>
    /// Menambah hari sebanyak 1 (Advance Day).
    /// </summary>
    public void AdvanceDay()
    {
        if (currentData == null) LoadData();
        currentData.day++;
        SaveData(currentData);
        OnDayChanged?.Invoke(currentData.day);
    }

    /// <summary>
    /// Mengeset hari secara langsung.
    /// </summary>
    public void SetDay(int newDay)
    {
        if (currentData == null) LoadData();
        currentData.day = newDay;
        SaveData(currentData);
        OnDayChanged?.Invoke(currentData.day);
    }

    /// <summary>
    /// Mengeset level listrik secara langsung.
    /// </summary>
    public void SetElectricityLevel(int newLevel)
    {
        if (currentData == null) LoadData();
        currentData.electricityLevel = Mathf.Max(1, newLevel);
        SaveData(currentData);
        OnElectricityLevelChanged?.Invoke(currentData.electricityLevel);
    }

    /// <summary>
    /// Menambah level listrik sejumlah amount (default +1).
    /// </summary>
    public void AddElectricityLevel(int amount = 1)
    {
        if (currentData == null) LoadData();
        currentData.electricityLevel = Mathf.Max(1, currentData.electricityLevel + amount);
        SaveData(currentData);
        OnElectricityLevelChanged?.Invoke(currentData.electricityLevel);
    }

    /// <summary>
    /// Reset data ke default (Money = 1000, Day = 1, ElectricityLevel = 1).
    /// </summary>
    public void ResetToDefault()
    {
        currentData = GetDefaultData();
        SaveData(currentData);
        OnMoneyChanged?.Invoke(currentData.money);
        OnDayChanged?.Invoke(currentData.day);
        OnElectricityLevelChanged?.Invoke(currentData.electricityLevel);
    }

    /// <summary>
    /// Check if a shop item ID has already been purchased.
    /// </summary>
    public bool IsItemPurchased(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (currentData == null) LoadData();
        if (currentData.purchasedItemIds == null)
        {
            currentData.purchasedItemIds = new List<string>();
        }
        return currentData.purchasedItemIds.Contains(itemId);
    }

    /// <summary>
    /// Record a shop item ID as purchased and save data.
    /// </summary>
    public void AddPurchasedItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (currentData == null) LoadData();
        if (currentData.purchasedItemIds == null)
        {
            currentData.purchasedItemIds = new List<string>();
        }

        if (!currentData.purchasedItemIds.Contains(itemId))
        {
            currentData.purchasedItemIds.Add(itemId);
            SaveData(currentData);
        }
    }

    public static GameData GetDefaultData()
    {
        return new GameData(1000, 1, 1);
    }
}
