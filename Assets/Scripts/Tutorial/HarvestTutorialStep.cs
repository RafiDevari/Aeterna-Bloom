using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Step Tutorial 6: Harvest Tanaman Monster (Overgrowth 130%).
/// Alur:
/// 1. Jeda 5 detik setelah Tutorial 5 selesai.
/// 2. Paksa Growth tanaman monster menjadi 130% (OvergrowthState = true).
/// 3. Klik Kiri Containment Unit.
/// 4. Klik tombol "HARVEST" pada ContainmentPopup.
/// 5. Pilih Employee dari EmployeeSelectPopup.
/// 6. Hilangkan layar hitam (transparan penuh) saat menunggu employee berjalan & melakukan harvest.
/// 7. Selesai setelah proses harvest rampung.
/// </summary>
public class HarvestTutorialStep : MonoBehaviour
{
    public enum TutorialPhase
    {
        Inactive,
        WaitingDelay,
        Phase1_ClickContainmentUnit,
        Phase2_ClickHarvestButton,
        Phase3_SelectEmployee,
        Phase4_WaitingHarvestProcess,
        Completed
    }

    [Header("Settings")]
    [SerializeField] private float delayAfterTutorial5 = 5.0f;
    [SerializeField] private bool autoStartAfterTutorial5 = true;

    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;
    public bool IsActive => CurrentPhase != TutorialPhase.Inactive && (CurrentPhase != TutorialPhase.Completed || overlayAlpha > 0.001f);
    public bool IsCompleted => CurrentPhase == TutorialPhase.Completed;

    private ContainmentUnit targetUnit;
    private Rect currentSpotlightRect;
    private Rect targetSpotlightRect;

    private float overlayAlpha = 0f;
    private float targetAlpha = 0.85f;
    private float fadeSpeed = 3.5f;

    // Textures & Styles
    private Texture2D darkOverlayTex;
    private Texture2D panelBgTex;
    private Texture2D whiteTex;
    private GUIStyle headerStyle;
    private GUIStyle instructionStyle;
    private GUIStyle hintStyle;
    private GUIStyle arrowStyle;
    private bool stylesInitialized = false;

    private NutritionTutorialStep step5;

    private void OnEnable()
    {
        ContainmentUnit.OnAnyUnitClicked += OnUnitClicked;
        ContainmentPopup.OnPopupOpened += OnPopupOpened;
        ContainmentPopup.OnHarvestButtonClicked += OnHarvestButtonClicked;
        EmployeeSelectPopup.OnAnyEmployeeSelectedFromPopup += OnEmployeeSelectedFromPopup;
        MonsterBase.OnAnyMonsterHarvestFinished += OnMonsterHarvestFinished;
    }

    private void OnDisable()
    {
        ContainmentUnit.OnAnyUnitClicked -= OnUnitClicked;
        ContainmentPopup.OnPopupOpened -= OnPopupOpened;
        ContainmentPopup.OnHarvestButtonClicked -= OnHarvestButtonClicked;
        EmployeeSelectPopup.OnAnyEmployeeSelectedFromPopup -= OnEmployeeSelectedFromPopup;
        MonsterBase.OnAnyMonsterHarvestFinished -= OnMonsterHarvestFinished;
    }

    private void Start()
    {
        step5 = FindFirstObjectByType<NutritionTutorialStep>();
        if (autoStartAfterTutorial5)
        {
            StartCoroutine(RoutineCheckAndStart());
        }
    }

    private IEnumerator RoutineCheckAndStart()
    {
        // 1. Wait until Tutorial 5 finishes
        if (step5 != null)
        {
            while (!step5.IsCompleted)
            {
                yield return null;
            }
        }

        // 2. Jeda 5 detik setelah Tutorial 5
        CurrentPhase = TutorialPhase.WaitingDelay;
        FacilityHUD.ShowBroadcast("TUTORIAL 6: HARVEST TANAMAN MONSTER AKAN DIMULAI DALAM 5 DETIK...", "TUTORIAL", 4.5f);

        yield return new WaitForSeconds(delayAfterTutorial5);

        // 3. Start Phase 1
        StartPhase1();
    }

    public void StartPhase1()
    {
        targetUnit = FindFirstObjectByType<ContainmentUnit>();

        // Paksa growth tanaman menjadi 130% (1.3f -> Overgrowth)
        if (targetUnit != null && targetUnit.HasMonster)
        {
            targetUnit.Monster.SetGrowth(1.3f);
            Debug.Log("[HarvestTutorialStep] Growth tanaman dipaksa menjadi 130% (1.3f - Overgrowth).");
        }

        CurrentPhase = TutorialPhase.Phase1_ClickContainmentUnit;
        overlayAlpha = 0f;
        targetAlpha = 0.85f;

        if (targetUnit != null && Camera.main != null)
        {
            StartCoroutine(RoutinePanCameraTo(targetUnit.transform.position));
        }

        FacilityHUD.ShowBroadcast("TANAMAN OVERGROWTH (130%)! KLIK CONTAINMENT UNIT UNTUK MEMANEN.", "TUTORIAL", 6f);
    }

    private void Update()
    {
        if (CurrentPhase == TutorialPhase.Inactive || CurrentPhase == TutorialPhase.WaitingDelay) return;

        if (CurrentPhase != TutorialPhase.Completed && CurrentPhase != TutorialPhase.Phase4_WaitingHarvestProcess)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            UpdateSpotlightRect();
        }
        else
        {
            // Hilangkan layar hitam transparan saat menunggu employee berjalan & memanen (Harvest)
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, 0f, Time.deltaTime * fadeSpeed);
        }

        // Interpolasi posisi spotlight
        currentSpotlightRect.x = Mathf.Lerp(currentSpotlightRect.x, targetSpotlightRect.x, Time.deltaTime * 10f);
        currentSpotlightRect.y = Mathf.Lerp(currentSpotlightRect.y, targetSpotlightRect.y, Time.deltaTime * 10f);
        currentSpotlightRect.width = Mathf.Lerp(currentSpotlightRect.width, targetSpotlightRect.width, Time.deltaTime * 10f);
        currentSpotlightRect.height = Mathf.Lerp(currentSpotlightRect.height, targetSpotlightRect.height, Time.deltaTime * 10f);
    }

    private void UpdateSpotlightRect()
    {
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_ClickContainmentUnit:
                if (targetUnit == null) targetUnit = FindFirstObjectByType<ContainmentUnit>();
                targetSpotlightRect = CalculateUnitScreenRect(targetUnit);
                if (currentSpotlightRect.width <= 1f) currentSpotlightRect = targetSpotlightRect;
                break;

            case TutorialPhase.Phase2_ClickHarvestButton:
                Button harvBtn = ContainmentPopup.Instance != null ? ContainmentPopup.Instance.HarvestButton : null;
                targetSpotlightRect = CalculateButtonScreenRect(harvBtn);
                break;

            case TutorialPhase.Phase3_SelectEmployee:
                if (EmployeeSelectPopup.Instance != null && EmployeeSelectPopup.Instance.IsOpen)
                {
                    targetSpotlightRect = CalculatePopupCenterRect();
                }
                break;

            case TutorialPhase.Phase4_WaitingHarvestProcess:
                if (targetUnit == null) targetUnit = FindFirstObjectByType<ContainmentUnit>();
                targetSpotlightRect = CalculateUnitScreenRect(targetUnit);
                break;
        }
    }

    private IEnumerator RoutinePanCameraTo(Vector3 worldTarget, float duration = 1.0f)
    {
        if (Camera.main == null) yield break;

        Vector3 startPos = Camera.main.transform.position;
        Vector3 targetPos = new Vector3(worldTarget.x, worldTarget.y, startPos.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Camera.main.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Camera.main.transform.position = targetPos;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────
    private void OnUnitClicked(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            targetUnit = unit;
            CurrentPhase = TutorialPhase.Phase2_ClickHarvestButton;
        }
    }

    private void OnPopupOpened(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            targetUnit = unit;
            CurrentPhase = TutorialPhase.Phase2_ClickHarvestButton;
        }
    }

    private void OnHarvestButtonClicked()
    {
        if (CurrentPhase == TutorialPhase.Phase2_ClickHarvestButton)
        {
            CurrentPhase = TutorialPhase.Phase3_SelectEmployee;
            FacilityHUD.ShowBroadcast("PILIH SALAH SATU EMPLOYEE UNTUK MEMANEN TANAMAN.", "TUTORIAL", 6f);
        }
    }

    private void OnEmployeeSelectedFromPopup(Employee emp)
    {
        if (CurrentPhase == TutorialPhase.Phase3_SelectEmployee)
        {
            CurrentPhase = TutorialPhase.Phase4_WaitingHarvestProcess;
            FacilityHUD.ShowBroadcast("EMPLOYEE SEDANG MEMANEN (HARVEST)... TUNGGU HINGGA SELESAI.", "TUTORIAL", 8f);
        }
    }

    private void OnMonsterHarvestFinished(MonsterBase monster)
    {
        if (CurrentPhase == TutorialPhase.Phase4_WaitingHarvestProcess)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        if (CurrentPhase == TutorialPhase.Completed) return;

        CurrentPhase = TutorialPhase.Completed;
        FacilityHUD.ShowBroadcast("BAGUS SKALI! ANDA TELAH BERHASIL MEMANEN (HARVEST) TANAMAN MONSTER OVERGROWTH.", "TUTORIAL", 7f);
    }

    // ── Rect Calculations ─────────────────────────────────────────────────────
    private Rect CalculateUnitScreenRect(ContainmentUnit unit)
    {
        if (unit == null || Camera.main == null)
        {
            return new Rect(Screen.width * 0.35f, Screen.height * 0.35f, Screen.width * 0.3f, Screen.height * 0.3f);
        }

        Collider2D col = unit.GetComponent<Collider2D>();
        SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();

        Vector3 minWorld, maxWorld;
        if (col != null)
        {
            minWorld = col.bounds.min;
            maxWorld = col.bounds.max;
        }
        else if (sr != null)
        {
            minWorld = sr.bounds.min;
            maxWorld = sr.bounds.max;
        }
        else
        {
            Vector3 pos = unit.transform.position;
            minWorld = pos - new Vector3(1.2f, 1.2f, 0f);
            maxWorld = pos + new Vector3(1.2f, 1.2f, 0f);
        }

        Vector3 screenMin = Camera.main.WorldToScreenPoint(minWorld);
        Vector3 screenMax = Camera.main.WorldToScreenPoint(maxWorld);

        float minX = Mathf.Min(screenMin.x, screenMax.x) - 25f;
        float maxX = Mathf.Max(screenMin.x, screenMax.x) + 25f;
        float minY = Mathf.Min(screenMin.y, screenMax.y) - 25f;
        float maxY = Mathf.Max(screenMin.y, screenMax.y) + 25f;

        float w = Mathf.Max(maxX - minX, 100f);
        float h = Mathf.Max(maxY - minY, 100f);

        float guiY = Screen.height - maxY;

        return new Rect(minX, guiY, w, h);
    }

    private Rect CalculateButtonScreenRect(Button btn)
    {
        if (btn == null)
        {
            return new Rect(Screen.width * 0.42f, Screen.height * 0.45f, 160f, 45f);
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null)
        {
            return new Rect(Screen.width * 0.42f, Screen.height * 0.45f, 160f, 45f);
        }

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = corners[0].x - 10f;
        float maxX = corners[2].x + 10f;
        float minY = corners[0].y - 10f;
        float maxY = corners[2].y + 10f;

        float w = Mathf.Max(maxX - minX, 80f);
        float h = Mathf.Max(maxY - minY, 40f);

        float guiY = Screen.height - maxY;

        return new Rect(minX, guiY, w, h);
    }

    private Rect CalculatePopupCenterRect()
    {
        float w = 380f;
        float h = 360f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        return new Rect(x, y, w, h);
    }

    // ── OnGUI Rendering ───────────────────────────────────────────────────────
    private void OnGUI()
    {
        if (overlayAlpha <= 0.001f) return;
        if (!IsActive && CurrentPhase != TutorialPhase.Completed) return;

        InitStylesIfNeeded();

        Color oldCol = GUI.color;

        // 1. Draw Spotlight Cutout Overlay
        DrawCutoutSpotlight(currentSpotlightRect, overlayAlpha);

        // 2. Header Instruction Banner
        float bannerW = 640f;
        float bannerH = 95f;
        float bannerX = (Screen.width - bannerW) * 0.5f;
        float bannerY = 25f;

        GUI.color = new Color(1f, 1f, 1f, overlayAlpha);
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, bannerH), panelBgTex);
        DrawBorder(new Rect(bannerX, bannerY, bannerW, bannerH), new Color(0.92f, 0.68f, 0.15f, overlayAlpha), 2);

        string headerText = GetHeaderText();
        string instructionText = GetInstructionText();

        GUI.Label(new Rect(bannerX + 15, bannerY + 12, bannerW - 30, 28), headerText, headerStyle);
        GUI.Label(new Rect(bannerX + 15, bannerY + 45, bannerW - 30, 36), instructionText, instructionStyle);

        GUI.color = oldCol;
    }

    private string GetHeaderText()
    {
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_ClickContainmentUnit: return "TUTORIAL 6: HARVEST TANAMAN (LANGKAH 1/4)";
            case TutorialPhase.Phase2_ClickHarvestButton: return "TUTORIAL 6: HARVEST TANAMAN (LANGKAH 2/4)";
            case TutorialPhase.Phase3_SelectEmployee: return "TUTORIAL 6: HARVEST TANAMAN (LANGKAH 3/4)";
            case TutorialPhase.Phase4_WaitingHarvestProcess: return "TUTORIAL 6: PROSES HARVEST (LANGKAH 4/4)";
            default: return "TUTORIAL 6: SELESAI!";
        }
    }

    private string GetInstructionText()
    {
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_ClickContainmentUnit: return "Klik Kiri pada Containment Unit (Growth: 130%)";
            case TutorialPhase.Phase2_ClickHarvestButton: return "Klik tombol 'HARVEST' pada menu";
            case TutorialPhase.Phase3_SelectEmployee: return "Pilih salah satu Employee untuk memanen";
            case TutorialPhase.Phase4_WaitingHarvestProcess: return "Employee sedang memanen (Harvest)... Harap tunggu";
            default: return "✓ Memanen (Harvest) Tanaman Monster Berhasil!";
        }
    }

    private void DrawCutoutSpotlight(Rect holeRect, float alpha)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        // Top Panel
        if (holeRect.y > 0)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, holeRect.y), darkOverlayTex);
        }

        // Bottom Panel
        float bottomY = holeRect.yMax;
        if (bottomY < Screen.height)
        {
            GUI.DrawTexture(new Rect(0, bottomY, Screen.width, Screen.height - bottomY), darkOverlayTex);
        }

        // Left Panel
        if (holeRect.x > 0)
        {
            GUI.DrawTexture(new Rect(0, holeRect.y, holeRect.x, holeRect.height), darkOverlayTex);
        }

        // Right Panel
        float rightX = holeRect.xMax;
        if (rightX < Screen.width)
        {
            GUI.DrawTexture(new Rect(rightX, holeRect.y, Screen.width - rightX, holeRect.height), darkOverlayTex);
        }

        if (CurrentPhase != TutorialPhase.Completed)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 0.25f);
            Color glowColor = new Color(1f, 0.8f + pulse, 0.15f, alpha);
            DrawBorder(holeRect, glowColor, 3);

            DrawSpotlightArrow(holeRect, alpha);
        }

        GUI.color = oldColor;
    }

    private void DrawSpotlightArrow(Rect holeRect, float alpha)
    {
        float pulseOffset = Mathf.Sin(Time.time * 6f) * 10f;
        Color arrowColor = new Color(1f, 0.88f, 0.25f, alpha);

        Color oldCol = GUI.color;
        GUI.color = arrowColor;

        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            float arrowX = holeRect.center.x - 120f;
            float arrowY = holeRect.y - 55f - pulseOffset;
            GUI.Label(new Rect(arrowX, arrowY, 240f, 45f), "▼ KLIK CONTAINMENT UNIT", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase2_ClickHarvestButton)
        {
            float arrowX = holeRect.x - 190f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;
            GUI.Label(new Rect(arrowX, arrowY, 180f, 40f), "HARVEST ➜", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase3_SelectEmployee)
        {
            float arrowX = holeRect.center.x - 120f;
            float arrowY = holeRect.y - 50f - pulseOffset;
            GUI.Label(new Rect(arrowX, arrowY, 240f, 40f), "▲ PILIH EMPLOYEE", arrowStyle);
        }

        GUI.color = oldCol;
    }

    private void DrawBorder(Rect rect, Color color, int thickness)
    {
        Texture2D tex = GetSolidTex(color);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);
    }

    private Texture2D GetSolidTex(Color col)
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }
        return whiteTex;
    }

    private void InitStylesIfNeeded()
    {
        if (stylesInitialized) return;

        darkOverlayTex = new Texture2D(1, 1);
        darkOverlayTex.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.88f));
        darkOverlayTex.Apply();

        panelBgTex = new Texture2D(1, 1);
        panelBgTex.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 0.96f));
        panelBgTex.Apply();

        headerStyle = new GUIStyle();
        headerStyle.fontSize = 20;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(0.96f, 0.78f, 0.2f, 1f);

        instructionStyle = new GUIStyle();
        instructionStyle.fontSize = 17;
        instructionStyle.fontStyle = FontStyle.Bold;
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        instructionStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle();
        hintStyle.fontSize = 14;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 1f);

        arrowStyle = new GUIStyle();
        arrowStyle.fontSize = 18;
        arrowStyle.fontStyle = FontStyle.Bold;
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = new Color(1f, 0.88f, 0.25f, 1f);

        stylesInitialized = true;
    }
}
