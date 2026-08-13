using UnityEngine;

public class Gallery : MonoBehaviour
{
    public PlantSlot plantSlotPrefab;
    public Transform galleryGrid;

    public PlantVisual[] plantVisuals;

    private void Start()
    {
        LoadGallery();
    }

    private void LoadGallery()
    {
        // Load JSON
        TextAsset jsonFile = Resources.Load<TextAsset>("plant_list");

        if (jsonFile == null)
        {
            Debug.LogError("Could not find Resources/plant_list.json");
            return;
        }

        PlantList plantList = JsonUtility.FromJson<PlantList>(jsonFile.text);

        // Create one slot for every plant
        foreach (PlantData plantData in plantList.plants)
        {
            PlantSlot slot = Instantiate(plantSlotPrefab, galleryGrid);

            Sprite sprite = GetPlantSprite(plantData.plantId);

            slot.Setup(plantData, sprite);
        }
    }

    private Sprite GetPlantSprite(string plantId)
    {
        foreach (PlantVisual plantVisual in plantVisuals)
        {
            if (plantVisual.plantId == plantId)
            {
                return plantVisual.sprite;
            }
        }

        Debug.LogWarning("No sprite found for plant: " + plantId);

        return null;
    }
}

[System.Serializable]
public class PlantVisual
{
    public string plantId;
    public Sprite sprite;
}