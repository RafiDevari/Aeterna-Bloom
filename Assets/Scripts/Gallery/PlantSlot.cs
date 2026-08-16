using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantSlot : MonoBehaviour
{
    [Header("Border")]
    public GameObject border;
    public GameObject goldBorder;

    [Header("Plant")]
    public Image plant;
    public Image plantBlack;

    [Header("Locked UI")]
    public GameObject lockIcon;
    public TMP_Text plantQuestion;

    [Header("Unlocked UI")]
    public TMP_Text plantName;

    [Header("Details")]
    public Button button;
    public Gallery gallery;

    private PlantData currentPlantData;

    public void Setup(PlantData plantData, Sprite plantSprite)
    {
        // Store the plant data so we know which plant
        // was clicked later.
        currentPlantData = plantData;

        // -------------------------
        // UNLOCKED / LOCKED
        // -------------------------

        if (plantData.isUnlocked)
        {
            // Show normal plant
            plant.gameObject.SetActive(true);

            // Hide black silhouette
            plantBlack.gameObject.SetActive(false);

            // Hide lock
            lockIcon.SetActive(false);

            // Show plant name
            plantName.gameObject.SetActive(true);
            plantName.text = plantData.plantId;

            // Hide ???
            plantQuestion.gameObject.SetActive(false);

            // Enable clicking
            button.interactable = true;
        }
        else
        {
            // Hide normal plant
            plant.gameObject.SetActive(false);

            // Show black silhouette
            plantBlack.gameObject.SetActive(true);

            // Show lock
            lockIcon.SetActive(true);

            // Hide plant name
            plantName.gameObject.SetActive(false);

            // Show ???
            plantQuestion.gameObject.SetActive(true);

            // Disable clicking
            button.interactable = false;
        }

        // -------------------------
        // MUTATION BORDER
        // -------------------------

        if (plantData.isMutated)
        {
            goldBorder.SetActive(true);
            border.SetActive(false);
        }
        else
        {
            goldBorder.SetActive(false);
            border.SetActive(true);
        }

        // -------------------------
        // PLANT SPRITE
        // -------------------------

        plant.sprite = plantSprite;
        plantBlack.sprite = plantSprite;

        // -------------------------
        // BUTTON CLICK
        // -------------------------

        // Remove any old listeners first.
        button.onClick.RemoveAllListeners();

        // Only unlocked plants can open details.
        if (plantData.isUnlocked)
        {
            button.onClick.AddListener(OpenDetails);
        }
    }

    private void OpenDetails()
    {
        gallery.OpenPlantDetails(currentPlantData);
    }
}