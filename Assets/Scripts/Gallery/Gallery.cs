using UnityEngine;

public class Gallery : MonoBehaviour
{
    public PlantSlot plantSlotPrefab;
    public Transform galleryGrid;

    public PlantDetails plantDetails;

    public PlantVisual[] plantVisuals;

    private void Start()
    {
        LoadGallery();

        plantDetails.gameObject.SetActive(false);
    }

    private void LoadGallery()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("plant_list");

        if (jsonFile == null)
        {
            Debug.LogError("Could not find Resources/plant_list.json");
            return;
        }

        PlantList plantList =
            JsonUtility.FromJson<PlantList>(jsonFile.text);

        foreach (PlantData plantData in plantList.plants)
        {
            PlantSlot slot =
                Instantiate(plantSlotPrefab, galleryGrid);

            PlantVisual visual =
                GetPlantVisual(plantData.plantId);

            if (visual == null)
            {
                Debug.LogWarning(
                    "No visual found for plant: " +
                    plantData.plantId
                );

                continue;
            }

            slot.gallery = this;

            slot.Setup(
                plantData,
                visual.growingSprite
            );
        }
    }

    private PlantVisual GetPlantVisual(string plantId)
    {
        foreach (PlantVisual plantVisual in plantVisuals)
        {
            if (plantVisual.plantId == plantId)
            {
                return plantVisual;
            }
        }

        Debug.LogWarning(
            "No visual found for plant: " + plantId
        );

        return null;
    }

    public void OpenPlantDetails(PlantData plantData)
    {
        PlantVisual plantVisual =
            GetPlantVisual(plantData.plantId);

        if (plantVisual == null)
        {
            Debug.LogWarning(
                "No visual found for plant: " +
                plantData.plantId
            );

            return;
        }

        plantDetails.Open(
            plantData,
            plantVisual
        );
    }

    public void ClosePlantDetails()
    {
        plantDetails.Close();
    }
}

[System.Serializable]
public class PlantVisual
{
    public string plantId;

    public Sprite growingSprite;
    public Sprite overgrowthSprite;
    public Sprite mutatedSprite;
}