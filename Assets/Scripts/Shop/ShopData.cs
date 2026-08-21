using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopItemData
{
    public string id;
    public string title;
    public string category; // "Seed", "Room", "Accessory", "Upgrade"
    public int price;
    public string description;
    public string spritePath;
    public int day; // Minimum day required to unlock
}

[Serializable]
public class ShopDatabase
{
    public List<ShopItemData> items = new List<ShopItemData>();

    public static ShopDatabase LoadFromResources(string resourcePath = "shop_items")
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
        {
            return JsonUtility.FromJson<ShopDatabase>(textAsset.text);
        }

        Debug.LogWarning($"[ShopDatabase] Failed to load shop items from Resources/{resourcePath}. Returning empty database.");
        return new ShopDatabase();
    }

    public List<ShopItemData> GetItemsByCategory(string category)
    {
        if (items == null) return new List<ShopItemData>();

        return items.FindAll(item => 
            string.Equals(item.category, category, StringComparison.OrdinalIgnoreCase));
    }
}
