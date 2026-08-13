using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Step Tutorial 5: Memberi Nutrisi Kalium pada Tanaman Monster.
/// Alur:
/// 1. Jeda 5 detik setelah Tutorial 4 selesai.
/// 2. Klik Kiri Containment Unit.
/// 3. Klik tombol "NUTRISI" pada ContainmentPopup.
/// 4. Klik tombol "KALIUM" pada NutrisiPopup.
/// 5. Pilih Employee dari EmployeeSelectPopup.
/// 6. Hilangkan layar hitam saat employee berjalan & memberi nutrisi.
/// 7. Setelah selesai, paksa growth menjadi 40% dan tampilkan 1 NOTE interaktif:
///    ">30% Growing State, >100% BISA DI-HARVEST".
/// 8. Saat Note di-klik, tutorial 5 selesai (dan lanjut ke tutorial berikutnya setelah jeda 5 detik).
/// </summary>
public class NutritionTutorialStep : MonoBehaviour
{
    public enum TutorialPhase
    {
        Inactive,
        WaitingDelay,
        Phase1_ClickContainmentUnit,
        Phase2_ClickNutrisiButton,
        Phase3_ClickKaliumButton,
        Phase4_SelectEmployee,
        Phase5_WaitingNutritionProcess,
        Phase6_GrowthNote,
        Completed
    }

    [Header("Settings")]
    [SerializeField] private float delayAfterTutorial4 = 5.0f;
    [SerializeField] private bool autoStartAfterTutorial4 = true;

    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;
    public bool IsActive => CurrentPhase != TutorialPhase.Inactive && (CurrentPhase != TutorialPhase.Completed || overlayAlpha > 0.001f);
    public bool IsCompleted => CurrentPhase == TutorialPhase.Completed;

    private ContainmentUnit targetUnit;
    private Rect currentSpotlightRect;
    private Rect targetSpotlightRect;

    private float overlayAlpha = 0f;
    private float targetAlpha = 0.85f;
    private float fadeSpeed = 3.5f;
    private float noteClickCooldown = 0f;

    // Textures & Styles
    private Texture2D darkOverlayTex;
    private Texture2D panelBgTex;
    private Texture2D whiteTex;
    private GUIStyle headerStyle;
    private GUIStyle instructionStyle;
    private GUIStyle noteTitleStyle;
    private GUIStyle noteBodyStyle;
    private GUIStyle noteButtonStyle;
    private GUIStyle arrowStyle;
    private bool stylesInitialized = false;

    private ResearchTutorialStep step4;

    private void OnEnable()
    {
        ContainmentUnit.OnAnyUnitClicked += OnUnitClicked;
        ContainmentPopup.OnPopupOpened += OnPopupOpened;
        ContainmentPopup.OnNutrisiButtonClicked += OnNutrisiButtonClicked;
        NutrisiPopup.OnNutritionSelected += OnNutritionSelected;
        EmployeeSelectPopup.OnAnyEmployeeSelectedFromPopup += OnEmployeeSelectedFromPopup;
        MonsterBase.OnAnyMonsterFeedFinished += OnMonsterFeedFinished;
    }

    private void OnDisable()
    {
        ContainmentUnit.OnAnyUnitClicked -= OnUnitClicked;
        ContainmentPopup.OnPopupOpened -= OnPopupOpened;
        ContainmentPopup.OnNutrisiButtonClicked -= OnNutrisiButtonClicked;
        NutrisiPopup.OnNutritionSelected -= OnNutritionSelected;
        EmployeeSelectPopup.OnAnyEmployeeSelectedFromPopup -= OnEmployeeSelectedFromPopup;
        MonsterBase.OnAnyMonsterFeedFinished -= OnMonsterFeedFinished;
    }

    private void Start()
    {
        step4 = FindFirstObjectByType<ResearchTutorialStep>();
        if (autoStartAfterTutorial4)
        {
            StartCoroutine(RoutineCheckAndStart());
        }
    }

    private IEnumerator RoutineCheckAndStart()
    {
        // 1. Wait until Tutorial 4 finishes
        if (step4 != null)
        {
            while (!step4.IsCompleted)
            {
                yield return null;
            }
        }

        // 2. Jeda 5 detik setelah Tutorial 4
        CurrentPhase = TutorialPhase.WaitingDelay;
        FacilityHUD.ShowBroadcast("TUTORIAL 5: PEMBERIAN NUTRISI KALIUM AKAN DIMULAI DALAM 5 DETIK...", "TUTORIAL", 4.5f);

        yield return new WaitForSeconds(delayAfterTutorial4);

        // 3. Start Phase 1
        StartPhase1();
    }

    public void StartPhase1()
    {
        targetUnit = FindFirstObjectByType<ContainmentUnit>();
        CurrentPhase = TutorialPhase.Phase1_ClickContainmentUnit;
        overlayAlpha = 0f;
        targetAlpha = 0.85f;

        if (targetUnit != null && Camera.main != null)
        {
            StartCoroutine(RoutinePanCameraTo(targetUnit.transform.position));
        }

        FacilityHUD.ShowBroadcast("KLIK KIRI CONTAINMENT UNIT UNTUK MEMBUKA MENU NUTRISI.", "TUTORIAL", 6f);
    }

    private void Update()
    {
        if (CurrentPhase == TutorialPhase.Inactive || CurrentPhase == TutorialPhase.WaitingDelay) return;

        if (CurrentPhase == TutorialPhase.Phase6_GrowthNote)
        {
            noteClickCooldown -= Time.deltaTime;
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

            // Deteksi klik mouse untuk menutup Note
            if (noteClickCooldown <= 0f && Input.GetMouseButtonDown(0))
            {
                CompleteTutorial();
            }
            return;
        }

        if (CurrentPhase != TutorialPhase.Completed && CurrentPhase != TutorialPhase.Phase5_WaitingNutritionProcess)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            UpdateSpotlightRect();
        }
        else
        {
            // Hilangkan layar hitam transparan saat menunggu employee berjalan & memberi nutrisi
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

            case TutorialPhase.Phase2_ClickNutrisiButton:
                Button nutBtn = ContainmentPopup.Instance != null ? ContainmentPopup.Instance.NutrisiButton : null;
                targetSpotlightRect = CalculateButtonScreenRect(nutBtn);
                break;

            case TutorialPhase.Phase3_ClickKaliumButton:
                Button kalBtn = NutrisiPopup.Instance != null ? NutrisiPopup.Instance.KaliumButton : null;
                targetSpotlightRect = CalculateButtonScreenRect(kalBtn);
                break;

            case TutorialPhase.Phase4_SelectEmployee:
                if (EmployeeSelectPopup.Instance != null && EmployeeSelectPopup.Instance.IsOpen)
                {
                    targetSpotlightRect = CalculatePopupCenterRect();
                }
                break;

            case TutorialPhase.Phase5_WaitingNutritionProcess:
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
            CurrentPhase = TutorialPhase.Phase2_ClickNutrisiButton;
        }
    }

    private void OnPopupOpened(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            targetUnit = unit;
            CurrentPhase = TutorialPhase.Phase2_ClickNutrisiButton;
        }
    }

    private void OnNutrisiButtonClicked()
    {
        if (CurrentPhase == TutorialPhase.Phase2_ClickNutrisiButton)
        {
            CurrentPhase = TutorialPhase.Phase3_ClickKaliumButton;
            FacilityHUD.ShowBroadcast("KLIK TOMBOL 'KALIUM' UNTUK MEMILIH NUTRISI KALIUM.", "TUTORIAL", 6f);
        }
    }

    private void OnNutritionSelected(FoodType food)
    {
        if (CurrentPhase == TutorialPhase.Phase3_ClickKaliumButton)
        {
            CurrentPhase = TutorialPhase.Phase4_SelectEmployee;
            FacilityHUD.ShowBroadcast("PILIH SALAH SATU EMPLOYEE UNTUK MENUGASKAN PEMBERIAN NUTRISI.", "TUTORIAL", 6f);
        }
    }

    private void OnEmployeeSelectedFromPopup(Employee emp)
    {
        if (CurrentPhase == TutorialPhase.Phase4_SelectEmployee)
        {
            CurrentPhase = TutorialPhase.Phase5_WaitingNutritionProcess;
            FacilityHUD.ShowBroadcast("EMPLOYEE SEDANG MEMBERI NUTRISI KALIUM... TUNGGU HINGGA SELESAI.", "TUTORIAL", 8f);
        }
    }

    private void OnMonsterFeedFinished(MonsterBase monster)
    {
        if (CurrentPhase == TutorialPhase.Phase5_WaitingNutritionProcess)
        {
            // Paksa growth tanaman menjadi 40% setelah pemberian nutrisi selesai
            ContainmentUnit unit = targetUnit != null ? targetUnit : FindFirstObjectByType<ContainmentUnit>();
            if (unit != null && unit.HasMonster)
            {
                unit.Monster.SetGrowth(0.4f);
                Debug.Log("[NutritionTutorialStep] Nutrisi Selesai: Growth tanaman dipaksa menjadi 40%.");
            }

            // Pindah ke Phase 6: Tampilkan Note Penjelasan Growth
            CurrentPhase = TutorialPhase.Phase6_GrowthNote;
            noteClickCooldown = 0.4f; // Cooldown 0.4 detik agar tidak langsung ter-klik tidak sengaja
        }
    }

    private void CompleteTutorial()
    {
        if (CurrentPhase == TutorialPhase.Completed) return;

        CurrentPhase = TutorialPhase.Completed;
        FacilityHUD.ShowBroadcast("CATATAN PERTUMBUHAN DIPAHAMI! MEMPERSIAPKAN TUTORIAL HARVEST...", "TUTORIAL", 5f);
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

        // 1. Jika dalam Phase 6 (Growth Note), tampilkan Kartu Note di Tengah Layar
        if (CurrentPhase == TutorialPhase.Phase6_GrowthNote)
        {
            // Dark Background
            GUI.color = new Color(1f, 1f, 1f, overlayAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkOverlayTex);

            // Centered Note Card
            float cardW = 540f;
            float cardH = 340f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;

            GUI.DrawTexture(new Rect(cardX, cardY, cardW, cardH), panelBgTex);
            DrawBorder(new Rect(cardX, cardY, cardW, cardH), new Color(0.95f, 0.72f, 0.15f, overlayAlpha), 3);

            // Title
            GUI.Label(new Rect(cardX + 20, cardY + 22, cardW - 40, 36), "📌 CATATAN INFORMASI PERTUMBUHAN", noteTitleStyle);

            // Note Content
            string noteBody = 
                "<color=#38bdf8><b>1. Growth > 30%</b></color> : Tanaman masuk ke <b>Growing State (Tumbuh)</b>.\n\n" +
                "<color=#f59e0b><b>2. Growth > 100%</b></color> : Tanaman mengalami <b>Overgrowth & BISA DI-HARVEST!</b>\n\n" +
                "<color=#a3e635><i>( Growth tanaman saat ini telah dipaksa menjadi 40% - Growing State )</i></color>";

            GUI.Label(new Rect(cardX + 35, cardY + 75, cardW - 70, 160), noteBody, noteBodyStyle);

            // Click Button to Continue
            float btnW = 320f;
            float btnH = 48f;
            float btnX = cardX + (cardW - btnW) * 0.5f;
            float btnY = cardY + 255f;

            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "KLIK UNTUK MELANJUTKAN ➔", noteButtonStyle))
            {
                CompleteTutorial();
            }

            GUI.color = oldCol;
            return;
        }

        // 2. Cutout Spotlight Overlay biasa (Phase 1..4)
        DrawCutoutSpotlight(currentSpotlightRect, overlayAlpha);

        // 3. Header Instruction Banner
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
            case TutorialPhase.Phase1_ClickContainmentUnit: return "TUTORIAL 5: NUTRISI KALIUM (LANGKAH 1/5)";
            case TutorialPhase.Phase2_ClickNutrisiButton: return "TUTORIAL 5: NUTRISI KALIUM (LANGKAH 2/5)";
            case TutorialPhase.Phase3_ClickKaliumButton: return "TUTORIAL 5: NUTRISI KALIUM (LANGKAH 3/5)";
            case TutorialPhase.Phase4_SelectEmployee: return "TUTORIAL 5: NUTRISI KALIUM (LANGKAH 4/5)";
            case TutorialPhase.Phase5_WaitingNutritionProcess: return "TUTORIAL 5: PROSES NUTRISI (LANGKAH 5/5)";
            case TutorialPhase.Phase6_GrowthNote: return "TUTORIAL 5: CATATAN PERTUMBUHAN";
            default: return "TUTORIAL 5: SELESAI!";
        }
    }

    private string GetInstructionText()
    {
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_ClickContainmentUnit: return "Klik Kiri pada Containment Unit di scene";
            case TutorialPhase.Phase2_ClickNutrisiButton: return "Klik tombol 'NUTRISI' pada menu";
            case TutorialPhase.Phase3_ClickKaliumButton: return "Klik tombol 'KALIUM' pada pilihan nutrisi";
            case TutorialPhase.Phase4_SelectEmployee: return "Pilih salah satu Employee untuk memberi nutrisi";
            case TutorialPhase.Phase5_WaitingNutritionProcess: return "Employee sedang memberi nutrisi Kalium... Harap tunggu";
            case TutorialPhase.Phase6_GrowthNote: return "Baca Catatan Pertumbuhan lalu Klik untuk melanjutkan";
            default: return "✓ Pemberian Nutrisi Kalium Berhasil!";
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
        else if (CurrentPhase == TutorialPhase.Phase2_ClickNutrisiButton)
        {
            float arrowX = holeRect.x - 190f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;
            GUI.Label(new Rect(arrowX, arrowY, 180f, 40f), "NUTRISI ➜", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase3_ClickKaliumButton)
        {
            float arrowX = holeRect.x - 190f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;
            GUI.Label(new Rect(arrowX, arrowY, 180f, 40f), "KALIUM ➜", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase4_SelectEmployee)
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

        noteTitleStyle = new GUIStyle();
        noteTitleStyle.fontSize = 21;
        noteTitleStyle.fontStyle = FontStyle.Bold;
        noteTitleStyle.alignment = TextAnchor.MiddleCenter;
        noteTitleStyle.normal.textColor = new Color(0.96f, 0.78f, 0.2f, 1f);

        noteBodyStyle = new GUIStyle();
        noteBodyStyle.fontSize = 16;
        noteBodyStyle.alignment = TextAnchor.UpperLeft;
        noteBodyStyle.normal.textColor = Color.white;
        noteBodyStyle.richText = true;
        noteBodyStyle.wordWrap = true;

        noteButtonStyle = new GUIStyle(GUI.skin.button);
        noteButtonStyle.fontSize = 16;
        noteButtonStyle.fontStyle = FontStyle.Bold;
        noteButtonStyle.alignment = TextAnchor.MiddleCenter;
        noteButtonStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        arrowStyle = new GUIStyle();
        arrowStyle.fontSize = 18;
        arrowStyle.fontStyle = FontStyle.Bold;
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = new Color(1f, 0.88f, 0.25f, 1f);

        stylesInitialized = true;
    }
}
