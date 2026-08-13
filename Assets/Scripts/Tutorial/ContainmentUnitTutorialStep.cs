using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Step Tutorial 2: Klik Containment Unit & Klik Tombol Info.
/// Memberikan jeda 5 detik setelah Tutorial 1 (Geser Kamera) selesai.
/// Menampilkan efek layar gelap dengan lubang sorotan (Cutout Spotlight) 
/// hanya pada bagian yang harus ditekan oleh player.
/// </summary>
public class ContainmentUnitTutorialStep : MonoBehaviour
{
    public enum TutorialPhase
    {
        Inactive,
        WaitingDelay,
        Phase1_ClickContainmentUnit,
        Phase2_ClickInfoButton,
        Completed
    }

    [Header("Settings")]
    [SerializeField] private float delayAfterTutorial1 = 5.0f;
    [SerializeField] private bool autoStartAfterTutorial1 = true;

    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;
    public bool IsActive => CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit || CurrentPhase == TutorialPhase.Phase2_ClickInfoButton || CurrentPhase == TutorialPhase.WaitingDelay || (CurrentPhase == TutorialPhase.Completed && overlayAlpha > 0.001f);
    public bool IsCompleted => CurrentPhase == TutorialPhase.Completed;

    private ContainmentUnit targetUnit;
    private Rect currentSpotlightRect;
    private Rect targetSpotlightRect;
    
    private float overlayAlpha = 0f;
    private float targetAlpha = 0.85f;
    private float fadeSpeed = 3.5f;

    // GUI Textures & Styles
    private Texture2D darkOverlayTex;
    private Texture2D panelBgTex;
    private Texture2D whiteTex;
    private GUIStyle headerStyle;
    private GUIStyle instructionStyle;
    private GUIStyle hintStyle;
    private GUIStyle arrowStyle;
    private bool stylesInitialized = false;

    private CameraPanTutorialStep tutorial1Step;

    private void OnEnable()
    {
        ContainmentUnit.OnAnyUnitClicked += OnUnitClicked;
        ContainmentPopup.OnPopupOpened += OnPopupOpened;
        ContainmentPopup.OnInfoButtonClicked += OnInfoClicked;
        MonsterInfoPopup.OnMonsterInfoOpened += OnMonsterInfoOpened;
    }

    private void OnDisable()
    {
        ContainmentUnit.OnAnyUnitClicked -= OnUnitClicked;
        ContainmentPopup.OnPopupOpened -= OnPopupOpened;
        ContainmentPopup.OnInfoButtonClicked -= OnInfoClicked;
        MonsterInfoPopup.OnMonsterInfoOpened -= OnMonsterInfoOpened;
    }

    private void Start()
    {
        tutorial1Step = FindFirstObjectByType<CameraPanTutorialStep>();
        if (autoStartAfterTutorial1)
        {
            StartCoroutine(RoutineCheckAndStart());
        }
    }

    private IEnumerator RoutineCheckAndStart()
    {
        // 1. Wait until Tutorial 1 is finished
        if (tutorial1Step != null)
        {
            while (!tutorial1Step.IsCompleted)
            {
                yield return null;
            }
        }

        // 2. Jeda 5 detik setelah tutorial 1 siap
        CurrentPhase = TutorialPhase.WaitingDelay;
        FacilityHUD.ShowBroadcast("TUTORIAL 2: INFORMASI MONSTER AKAN DIMULAI DALAM 5 DETIK...", "TUTORIAL", 4.5f);
        
        yield return new WaitForSeconds(delayAfterTutorial1);

        // 3. Mulai Tutorial 2 Phase 1
        StartPhase1();
    }

    public void StartPhase1()
    {
        targetUnit = FindFirstObjectByType<ContainmentUnit>();
        CurrentPhase = TutorialPhase.Phase1_ClickContainmentUnit;
        overlayAlpha = 0f;
        targetAlpha = 0.85f;

        FacilityHUD.ShowBroadcast("KLIK KIRI CONTAINMENT UNIT UNTUK MEMBUKA MENU MONSTER.", "TUTORIAL", 6f);
    }

    private void Update()
    {
        if (CurrentPhase == TutorialPhase.Inactive || CurrentPhase == TutorialPhase.WaitingDelay)
            return;

        // Smooth fade-in and smooth spotlight rect interpolation
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit || CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            UpdateSpotlightRect();
        }
        else if (CurrentPhase == TutorialPhase.Completed)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, 0f, Time.deltaTime * fadeSpeed);
        }

        // Smoothly interpolate spotlight position to target position
        currentSpotlightRect.x = Mathf.Lerp(currentSpotlightRect.x, targetSpotlightRect.x, Time.deltaTime * 10f);
        currentSpotlightRect.y = Mathf.Lerp(currentSpotlightRect.y, targetSpotlightRect.y, Time.deltaTime * 10f);
        currentSpotlightRect.width = Mathf.Lerp(currentSpotlightRect.width, targetSpotlightRect.width, Time.deltaTime * 10f);
        currentSpotlightRect.height = Mathf.Lerp(currentSpotlightRect.height, targetSpotlightRect.height, Time.deltaTime * 10f);
    }

    private void UpdateSpotlightRect()
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            if (targetUnit == null)
            {
                targetUnit = FindFirstObjectByType<ContainmentUnit>();
            }

            targetSpotlightRect = CalculateUnitScreenRect(targetUnit);

            if (currentSpotlightRect.width <= 1f)
            {
                currentSpotlightRect = targetSpotlightRect;
            }
        }
        else if (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
        {
            Button infoBtn = null;
            if (ContainmentPopup.Instance != null)
            {
                infoBtn = ContainmentPopup.Instance.InfoButton;
            }

            targetSpotlightRect = CalculateButtonScreenRect(infoBtn);
        }
    }

    // ── Event Callbacks ───────────────────────────────────────────────────────
    private void OnUnitClicked(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            CurrentPhase = TutorialPhase.Phase2_ClickInfoButton;
            FacilityHUD.ShowBroadcast("BAGUS! SEKARANG KLIK TOMBOL 'INFO' UNTUK MEMBUKA DATA MONSTER.", "TUTORIAL", 6f);
        }
    }

    private void OnPopupOpened(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
        {
            CurrentPhase = TutorialPhase.Phase2_ClickInfoButton;
        }
    }

    private void OnInfoClicked()
    {
        if (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
        {
            CompleteTutorial();
        }
    }

    private void OnMonsterInfoOpened(ContainmentUnit unit)
    {
        if (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        if (CurrentPhase == TutorialPhase.Completed) return;

        CurrentPhase = TutorialPhase.Completed;
        FacilityHUD.ShowBroadcast("SELESAI! ANDA TELAH BERHASIL MEMBUKA DATA OBSERVASI MONSTER.", "TUTORIAL", 7f);
    }

    // ── Spotlight Rect Math ───────────────────────────────────────────────────
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

        // GUI Y is inverted (0 at top)
        float guiY = Screen.height - maxY;

        return new Rect(minX, guiY, w, h);
    }

    private Rect CalculateButtonScreenRect(Button btn)
    {
        if (btn == null)
        {
            // Default position if button reference missing
            return new Rect(Screen.width * 0.42f, Screen.height * 0.45f, 160f, 50f);
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null)
        {
            return new Rect(Screen.width * 0.42f, Screen.height * 0.45f, 160f, 50f);
        }

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = corners[0].x - 12f;
        float maxX = corners[2].x + 12f;
        float minY = corners[0].y - 12f;
        float maxY = corners[2].y + 12f;

        float w = Mathf.Max(maxX - minX, 80f);
        float h = Mathf.Max(maxY - minY, 40f);

        float guiY = Screen.height - maxY;

        return new Rect(minX, guiY, w, h);
    }

    // ── OnGUI Spotlight Rendering ─────────────────────────────────────────────
    private void OnGUI()
    {
        if (overlayAlpha <= 0.001f) return;
        if (CurrentPhase != TutorialPhase.Phase1_ClickContainmentUnit && 
            CurrentPhase != TutorialPhase.Phase2_ClickInfoButton && 
            CurrentPhase != TutorialPhase.Completed)
            return;

        InitStylesIfNeeded();

        Color oldCol = GUI.color;

        // 1. Draw Spotlight Cutout (4 dark panels leaving the target hole bright & clear)
        DrawCutoutSpotlight(currentSpotlightRect, overlayAlpha);

        // 2. Draw Instruction Banner at the Top
        float bannerW = 600f;
        float bannerH = 95f;
        float bannerX = (Screen.width - bannerW) * 0.5f;
        float bannerY = 25f;

        GUI.color = new Color(1f, 1f, 1f, overlayAlpha);
        GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, bannerH), panelBgTex);
        DrawBorder(new Rect(bannerX, bannerY, bannerW, bannerH), new Color(0.92f, 0.68f, 0.15f, overlayAlpha), 2);

        string headerText = (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
            ? "TUTORIAL 2: INFORMASI MONSTER (LANGKAH 1/2)"
            : (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
                ? "TUTORIAL 2: INFORMASI MONSTER (LANGKAH 2/2)"
                : "TUTORIAL 2: SELESAI!";

        string instructionText = (CurrentPhase == TutorialPhase.Phase1_ClickContainmentUnit)
            ? "Klik Kiri pada Containment Unit yang ada di scene"
            : (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
                ? "Klik tombol 'INFO' pada menu yang muncul"
                : "✓ Halaman Informasi Monster Berhasil Dibuka";

        GUI.Label(new Rect(bannerX + 15, bannerY + 12, bannerW - 30, 28), headerText, headerStyle);
        GUI.Label(new Rect(bannerX + 15, bannerY + 45, bannerW - 30, 36), instructionText, instructionStyle);

        GUI.color = oldCol;
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
            // Glowing Animated Border around the target cutout hole
            float pulse = Mathf.PingPong(Time.time * 4f, 0.25f);
            Color glowColor = new Color(1f, 0.8f + pulse, 0.15f, alpha);
            DrawBorder(holeRect, glowColor, 3);

            // Draw Pointer Arrow pointing at the hole
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
            // Arrow pointing DOWN above the Containment Unit
            float arrowX = holeRect.center.x - 120f;
            float arrowY = holeRect.y - 55f - pulseOffset;

            GUI.Label(new Rect(arrowX, arrowY, 240f, 45f), "▼ KLIK KIRI DI SINI", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase2_ClickInfoButton)
        {
            // Arrow pointing RIGHT to the left of the Info Button
            float arrowX = holeRect.x - 190f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;

            GUI.Label(new Rect(arrowX, arrowY, 180f, 40f), "KLIK 'INFO' ➜", arrowStyle);
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
