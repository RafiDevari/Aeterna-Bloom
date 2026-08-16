using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantDetails : MonoBehaviour
{
    [Header("UI")]
    public Image plantImage;
    public GameObject lockIcon;

    public GameObject leftArrow;
    public GameObject rightArrow;

    public TMP_Text plantName;
    public TMP_Text description;

    [Header("Locked Appearance")]
    public Color lockedColor = Color.black;

    private PlantData currentPlant;
    private PlantVisual currentVisual;

    private int currentState;

    // 0 = Growing
    // 1 = Overgrowth
    // 2 = Mutated

    public void Open(PlantData plantData, PlantVisual plantVisual)
    {
        currentPlant = plantData;
        currentVisual = plantVisual;

        currentState = 0;

        plantName.text = plantData.plantId;

        description.text = plantData.plantDescription;

        gameObject.SetActive(true);

        UpdateState();
    }

    public void NextState()
    {
        if (currentState >= 2)
            return;

        // Do not allow access to mutated if it has not been unlocked.
        if (currentState == 1 && !currentPlant.isMutated)
            return;

        currentState++;

        UpdateState();
    }

    public void PreviousState()
    {
        if (currentState <= 0)
            return;

        currentState--;

        UpdateState();
    }

    private void UpdateState()
    {
        // Reset lock appearance
        lockIcon.SetActive(false);

        // Reset plant color
        plantImage.color = Color.white;

        // -------------------------
        // GROWING
        // -------------------------

        if (currentState == 0)
        {
            plantImage.sprite = currentVisual.growingSprite;

            leftArrow.SetActive(false);
            rightArrow.SetActive(true);
        }

        // -------------------------
        // OVERGROWTH
        // -------------------------

        else if (currentState == 1)
        {
            plantImage.sprite = currentVisual.overgrowthSprite;

            leftArrow.SetActive(true);

            // Only show right arrow if mutated exists/unlocked
            rightArrow.SetActive(currentPlant.isMutated);
        }

        // -------------------------
        // MUTATED
        // -------------------------

        else if (currentState == 2)
        {
            leftArrow.SetActive(true);
            rightArrow.SetActive(false);

            if (currentPlant.isMutated)
            {
                plantImage.sprite = currentVisual.mutatedSprite;
            }
            else
            {
                // Mutated is locked
                plantImage.sprite = currentVisual.mutatedSprite;

                plantImage.color = lockedColor;

                lockIcon.SetActive(true);
            }
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}