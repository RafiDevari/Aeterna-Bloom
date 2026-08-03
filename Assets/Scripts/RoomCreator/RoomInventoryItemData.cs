using UnityEngine;

/// <summary>
/// Data item inventaris room yang dimiliki user.
/// </summary>
[System.Serializable]
public class RoomInventoryItemData
{
    public string displayName;
    public int count;
    public GameObject roomPrefab;

    public RoomInventoryItemData() { }

    public RoomInventoryItemData(string name, int initialCount, GameObject prefab)
    {
        displayName = name;
        count = initialCount;
        roomPrefab = prefab;
    }
}
