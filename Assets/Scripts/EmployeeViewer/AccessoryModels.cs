using System.Collections.Generic;

[System.Serializable]
public class AccessoryItemData
{
    public string id;
    public string name;
    public string spritePath;
    public bool isUnlocked;
}

[System.Serializable]
public class AccessoryListSaveData
{
    public List<AccessoryItemData> suits = new List<AccessoryItemData>();
    public List<AccessoryItemData> hairs = new List<AccessoryItemData>();
}
