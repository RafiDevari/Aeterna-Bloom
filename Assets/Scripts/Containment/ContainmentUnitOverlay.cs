using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles drawing a small World Space HUD panel above the ContainmentUnit.
/// Displays growth and mood of the plant (monster) residing inside.
/// Matches its width with the containment unit width, and disables raycasting so clicks pass through.
/// </summary>
public class ContainmentUnitOverlay : MonoBehaviour
{
    private ContainmentUnit unit;
    private GameObject canvasGo;
    private RectTransform canvasRect;

    // UI elements
    private Image growthBarFill;
    private TextMeshProUGUI growthText;
    private Image moodBarFill;
    private TextMeshProUGUI moodText;

    private MonsterBase currentMonster;
    private float unitWidth = 2.0f;
    private bool uiInitialized = false;

    private void Start()
    {
        unit = GetComponent<ContainmentUnit>();
        if (unit == null)
        {
            Debug.LogError($"[ContainmentUnitOverlay] ContainmentUnit component not found on {gameObject.name}!");
            return;
        }

        // Try to get unit width from sprite renderer or box collider
        if (unit.UnitRenderer != null && unit.UnitRenderer.sprite != null)
        {
            unitWidth = unit.UnitRenderer.sprite.bounds.size.x;
        }
        else if (GetComponent<BoxCollider2D>() != null)
        {
            unitWidth = GetComponent<BoxCollider2D>().size.x;
        }

        // Setup the UI GameObjects
        InitializeUI();

        // Subscribe to unit events
        unit.OnMonsterAssigned += BindMonster;
        unit.OnMonsterRemoved += UnbindMonster;

        // If a monster is already assigned at start, bind it immediately
        if (unit.Monster != null)
        {
            BindMonster(unit.Monster);
        }
        else
        {
            if (canvasGo != null) canvasGo.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnMonsterAssigned -= BindMonster;
            unit.OnMonsterRemoved -= UnbindMonster;
        }
        UnbindMonster();
    }

    private void LateUpdate()
    {
        if (canvasRect != null)
        {
            // Prevent mirroring if parent scale is flipped (negative X or Y)
            Vector3 parentLossy = transform.lossyScale;
            float signX = parentLossy.x < 0 ? -1f : 1f;
            float signY = parentLossy.y < 0 ? -1f : 1f;
            canvasRect.localScale = new Vector3(0.01f * signX, 0.01f * signY, 1f);
        }
    }

    private void InitializeUI()
    {
        if (uiInitialized) return;

        // Create Canvas Game Object
        canvasGo = new GameObject("ContainmentUnitOverlayCanvas");
        canvasGo.transform.SetParent(transform, false);

        // Add Canvas Component
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Match sorting layer and order
        if (unit.UnitRenderer != null)
        {
            canvas.sortingLayerID = unit.UnitRenderer.sortingLayerID;
            canvas.sortingOrder = unit.UnitRenderer.sortingOrder + 10;
        }
        else
        {
            canvas.sortingOrder = 10;
        }

        // Add CanvasScaler for world space TMPro scaling
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        // Set RectTransform properties (pivot bottom-center)
        canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0.5f, 0f);
        canvasRect.anchorMax = new Vector2(0.5f, 0f);
        canvasRect.pivot = new Vector2(0.5f, 0f);
    
        float canvasWidth = Mathf.Max(unitWidth * 100f, 280f);
        float canvasHeight = 180f;
        canvasRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        // Position canvas above the unit sprite
        float topY = 1.5f;
        if (unit.UnitRenderer != null && unit.UnitRenderer.sprite != null)
        {
            topY = unit.UnitRenderer.sprite.bounds.max.y;
        }
        canvasRect.localPosition = new Vector3(0f, topY + 0.1f, 0f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

        // --- Create UI Elements ---

        // 1. Outer Border Panel
        GameObject borderGo = new GameObject("BorderPanel");
        borderGo.transform.SetParent(canvasGo.transform, false);
        RectTransform borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        Image borderImage = borderGo.AddComponent<Image>();
        borderImage.color = new Color(0.2f, 0.25f, 0.35f, 0.85f); // Slate-700
        borderImage.raycastTarget = false;

        // 2. Inner Background Panel
        GameObject bgGo = new GameObject("BackgroundPanel");
        bgGo.transform.SetParent(borderGo.transform, false);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(-2f, -2f); // 1px margin all around for border
        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.08f, 0.15f, 0.95f); // Slate-950 dark cyber theme
        bgImage.raycastTarget = false;

        // --- ROW 1: Growth ---
        GameObject growthRow = new GameObject("GrowthRow");
        growthRow.transform.SetParent(bgGo.transform, false);
        RectTransform growthRowRect = growthRow.AddComponent<RectTransform>();
        growthRowRect.anchorMin = new Vector2(0f, 0.5f);
        growthRowRect.anchorMax = new Vector2(1f, 1f);
        growthRowRect.anchoredPosition = new Vector3(0f, -10f, 0f);
        growthRowRect.sizeDelta = new Vector2(-30f, -20f); // padding left/right 15px, top/bottom 10px

        // Growth Label
        GameObject growthLabelGo = new GameObject("GrowthLabel");
        growthLabelGo.transform.SetParent(growthRow.transform, false);
        RectTransform growthLabelRect = growthLabelGo.AddComponent<RectTransform>();
        growthLabelRect.anchorMin = new Vector2(0f, 0.5f);
        growthLabelRect.anchorMax = new Vector2(0f, 0.5f);
        growthLabelRect.pivot = new Vector2(0f, 0.5f);
        growthLabelRect.anchoredPosition = new Vector3(0f, 18f, 0f);
        growthLabelRect.sizeDelta = new Vector2(160f, 36f);
        TextMeshProUGUI growthLabelText = growthLabelGo.AddComponent<TextMeshProUGUI>();
        growthLabelText.text = "GROWTH";
        growthLabelText.fontSize = 28f;
        growthLabelText.fontStyle = FontStyles.Bold;
        growthLabelText.color = new Color(0.58f, 0.64f, 0.72f); // slate-400
        growthLabelText.alignment = TextAlignmentOptions.Left;
        growthLabelText.raycastTarget = false;

        // Growth Value Text
        GameObject growthValGo = new GameObject("GrowthValue");
        growthValGo.transform.SetParent(growthRow.transform, false);
        RectTransform growthValRect = growthValGo.AddComponent<RectTransform>();
        growthValRect.anchorMin = new Vector2(1f, 0.5f);
        growthValRect.anchorMax = new Vector2(1f, 0.5f);
        growthValRect.pivot = new Vector2(1f, 0.5f);
        growthValRect.anchoredPosition = new Vector3(0f, 18f, 0f);
        growthValRect.sizeDelta = new Vector2(80f, 36f);
        growthText = growthValGo.AddComponent<TextMeshProUGUI>();
        growthText.text = "0%";
        growthText.fontSize = 28f;
        growthText.fontStyle = FontStyles.Bold;
        growthText.color = new Color(0.06f, 0.73f, 0.51f); // emerald-500
        growthText.alignment = TextAlignmentOptions.Right;
        growthText.raycastTarget = false;

        // Growth Bar Background
        GameObject growthBarBgGo = new GameObject("GrowthBarBg");
        growthBarBgGo.transform.SetParent(growthRow.transform, false);
        RectTransform growthBarBgRect = growthBarBgGo.AddComponent<RectTransform>();
        growthBarBgRect.anchorMin = new Vector2(0f, 0f);
        growthBarBgRect.anchorMax = new Vector2(1f, 0f);
        growthBarBgRect.pivot = new Vector2(0.5f, 0f);
        growthBarBgRect.anchoredPosition = new Vector3(0f, 6f, 0f);
        growthBarBgRect.sizeDelta = new Vector2(0f, 14f);
        Image growthBarBgImg = growthBarBgGo.AddComponent<Image>();
        growthBarBgImg.color = new Color(0.12f, 0.16f, 0.23f); // slate-800
        growthBarBgImg.raycastTarget = false;

        // Growth Bar Fill
        GameObject growthBarFillGo = new GameObject("GrowthBarFill");
        growthBarFillGo.transform.SetParent(growthBarBgGo.transform, false);
        RectTransform growthBarFillRect = growthBarFillGo.AddComponent<RectTransform>();
        growthBarFillRect.anchorMin = Vector2.zero;
        growthBarFillRect.anchorMax = new Vector2(0f, 1f);
        growthBarFillRect.pivot = new Vector2(0f, 0.5f);
        growthBarFillRect.sizeDelta = Vector2.zero;
        growthBarFillRect.anchoredPosition = Vector2.zero;
        growthBarFill = growthBarFillGo.AddComponent<Image>();
        growthBarFill.color = new Color(0.06f, 0.73f, 0.51f); // emerald-500
        growthBarFill.raycastTarget = false;

        // --- ROW 2: Mood ---
        GameObject moodRow = new GameObject("MoodRow");
        moodRow.transform.SetParent(bgGo.transform, false);
        RectTransform moodRowRect = moodRow.AddComponent<RectTransform>();
        moodRowRect.anchorMin = new Vector2(0f, 0f);
        moodRowRect.anchorMax = new Vector2(1f, 0.5f);
        moodRowRect.anchoredPosition = new Vector3(0f, 10f, 0f);
        moodRowRect.sizeDelta = new Vector2(-30f, -20f);

        // Mood Label
        GameObject moodLabelGo = new GameObject("MoodLabel");
        moodLabelGo.transform.SetParent(moodRow.transform, false);
        RectTransform moodLabelRect = moodLabelGo.AddComponent<RectTransform>();
        moodLabelRect.anchorMin = new Vector2(0f, 0.5f);
        moodLabelRect.anchorMax = new Vector2(0f, 0.5f);
        moodLabelRect.pivot = new Vector2(0f, 0.5f);
        moodLabelRect.anchoredPosition = new Vector3(0f, 18f, 0f);
        moodLabelRect.sizeDelta = new Vector2(160f, 36f);
        TextMeshProUGUI moodLabelText = moodLabelGo.AddComponent<TextMeshProUGUI>();
        moodLabelText.text = "MOOD";
        moodLabelText.fontSize = 28f;
        moodLabelText.fontStyle = FontStyles.Bold;
        moodLabelText.color = new Color(0.58f, 0.64f, 0.72f);
        moodLabelText.alignment = TextAlignmentOptions.Left;
        moodLabelText.raycastTarget = false;

        // Mood Value Text
        GameObject moodValGo = new GameObject("MoodValue");
        moodValGo.transform.SetParent(moodRow.transform, false);
        RectTransform moodValRect = moodValGo.AddComponent<RectTransform>();
        moodValRect.anchorMin = new Vector2(1f, 0.5f);
        moodValRect.anchorMax = new Vector2(1f, 0.5f);
        moodValRect.pivot = new Vector2(1f, 0.5f);
        moodValRect.anchoredPosition = new Vector3(0f, 18f, 0f);
        moodValRect.sizeDelta = new Vector2(80f, 36f);
        moodText = moodValGo.AddComponent<TextMeshProUGUI>();
        moodText.text = "0/0";
        moodText.fontSize = 28f;
        moodText.fontStyle = FontStyles.Bold;
        moodText.color = new Color(0.98f, 0.45f, 0.09f); // orange-500
        moodText.alignment = TextAlignmentOptions.Right;
        moodText.raycastTarget = false;

        // Mood Bar Background
        GameObject moodBarBgGo = new GameObject("MoodBarBg");
        moodBarBgGo.transform.SetParent(moodRow.transform, false);
        RectTransform moodBarBgRect = moodBarBgGo.AddComponent<RectTransform>();
        moodBarBgRect.anchorMin = new Vector2(0f, 0f);
        moodBarBgRect.anchorMax = new Vector2(1f, 0f);
        moodBarBgRect.pivot = new Vector2(0.5f, 0f);
        moodBarBgRect.anchoredPosition = new Vector3(0f, 2f, 0f);
        moodBarBgRect.sizeDelta = new Vector2(0f, 14f);
        Image moodBarBgImg = moodBarBgGo.AddComponent<Image>();
        moodBarBgImg.color = new Color(0.12f, 0.16f, 0.23f);
        moodBarBgImg.raycastTarget = false;

        // Mood Bar Fill
        GameObject moodBarFillGo = new GameObject("MoodBarFill");
        moodBarFillGo.transform.SetParent(moodBarBgGo.transform, false);
        RectTransform moodBarFillRect = moodBarFillGo.AddComponent<RectTransform>();
        moodBarFillRect.anchorMin = Vector2.zero;
        moodBarFillRect.anchorMax = new Vector2(0f, 1f);
        moodBarFillRect.pivot = new Vector2(0f, 0.5f);
        moodBarFillRect.sizeDelta = Vector2.zero;
        moodBarFillRect.anchoredPosition = Vector2.zero;
        moodBarFill = moodBarFillGo.AddComponent<Image>();
        moodBarFill.color = new Color(0.98f, 0.45f, 0.09f); // orange-500
        moodBarFill.raycastTarget = false;

        uiInitialized = true;
    }

    private void BindMonster(MonsterBase monster)
    {
        UnbindMonster();

        if (monster == null) return;

        currentMonster = monster;
        currentMonster.OnGrowthChanged += HandleGrowthChanged;
        currentMonster.OnMoodChanged += HandleMoodChanged;

        if (canvasGo != null) canvasGo.SetActive(true);

        UpdateGrowth(currentMonster.Growth);
        UpdateMood(currentMonster.Mood);
    }

    private void UnbindMonster()
    {
        if (currentMonster != null)
        {
            currentMonster.OnGrowthChanged -= HandleGrowthChanged;
            currentMonster.OnMoodChanged -= HandleMoodChanged;
            currentMonster = null;
        }

        if (canvasGo != null) canvasGo.SetActive(false);
    }

    private void HandleGrowthChanged(float growth)
    {
        UpdateGrowth(growth);
    }

    private void HandleMoodChanged(int mood)
    {
        UpdateMood(mood);
    }

    private void UpdateGrowth(float growthVal)
    {
        if (growthText == null || growthBarFill == null) return;

        // Display text in percentage (e.g. 75%, 150%)
        growthText.text = $"{growthVal * 100f:F0}%";

        // Progress bar ratio: fill standard 0 to 1, clamp at 1
        float fillRatio = Mathf.Clamp01(growthVal);
        growthBarFill.rectTransform.anchorMax = new Vector2(fillRatio, 1f);

        // Rich Aesthetics: Color shifts depending on growth state
        // Growth < 1.0f: Growing (Emerald Green)
        // 1.0f <= Growth < 2.0f: Overgrowth (Neon Cyan)
        // Growth >= 2.0f: Mutated (Neon Purple)
        if (growthVal < 1.0f)
        {
            Color emerald = new Color(0.06f, 0.73f, 0.51f);
            growthBarFill.color = emerald;
            growthText.color = emerald;
        }
        else if (growthVal < 2.0f)
        {
            Color cyan = new Color(0.02f, 0.71f, 0.83f);
            growthBarFill.color = cyan;
            growthText.color = cyan;
        }
        else
        {
            Color neonPurple = new Color(0.66f, 0.33f, 0.97f);
            growthBarFill.color = neonPurple;
            growthText.color = neonPurple;
        }
    }

    private void UpdateMood(int moodVal)
    {
        if (moodText == null || moodBarFill == null || currentMonster == null) return;

        int maxMood = currentMonster.MaxMood;
        moodText.text = $"{moodVal}/{maxMood}";

        float fillRatio = maxMood > 0 ? (float)moodVal / maxMood : 0f;
        moodBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fillRatio), 1f);

        // Rich Aesthetics: Color shifts depending on mood level
        // Poor mood (<= 1): Red
        // Okay mood (2-3): Orange/Amber
        // High mood (>= 4): Blue/Green
        if (moodVal <= 1)
        {
            Color crimson = new Color(0.94f, 0.17f, 0.29f); // Red
            moodBarFill.color = crimson;
            moodText.color = crimson;
        }
        else if (moodVal <= 3)
        {
            Color orange = new Color(0.98f, 0.45f, 0.09f); // Orange
            moodBarFill.color = orange;
            moodText.color = orange;
        }
        else
        {
            Color skyBlue = new Color(0.22f, 0.6f, 0.95f); // Sky Blue
            moodBarFill.color = skyBlue;
            moodText.color = skyBlue;
        }
    }
}
