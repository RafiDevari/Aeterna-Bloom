using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Step Tutorial 3: Pindahkan & Kembalikan Employee.
/// Membawa alur:
/// 1. Klik Kiri pada Employee.
/// 2. Klik tombol "ORDER TO" di popup employee.
/// 3. Kamera meluncur (pan) ke ruangan lain secara acak, layar gelap dengan sorotan pada ruangan tujuan. User klik ruangan itu.
/// 4. Klik Kiri pada Employee lagi.
/// 5. Klik tombol "BACK TO DIVISION" (Return) untuk mengembalikan employee.
/// </summary>
public class EmployeeMoveTutorialStep : MonoBehaviour
{
    public enum TutorialPhase
    {
        Inactive,
        WaitingDelay,
        Phase1_SelectEmployee,
        Phase2_ClickOrderTo,
        Phase3_ClickTargetRoom,
        Phase4_SelectEmployeeAgain,
        Phase5_ClickReturnButton,
        Completed
    }

    [Header("Settings")]
    [SerializeField] private float delayAfterTutorial2 = 5.0f;
    [SerializeField] private bool autoStartAfterTutorial2 = true;

    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;
    public bool IsActive => CurrentPhase != TutorialPhase.Inactive && (CurrentPhase != TutorialPhase.Completed || overlayAlpha > 0.001f);
    public bool IsCompleted => CurrentPhase == TutorialPhase.Completed;

    private Employee targetEmployee;
    private Room targetRoom;

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

    private ContainmentUnitTutorialStep step2;

    private void OnEnable()
    {
        Employee.OnAnyEmployeeClicked += OnEmployeeClicked;
        EmployeePopup.OnOrderToButtonClicked += OnOrderToClicked;
        EmployeeOrderController.OnOrderExecuted += OnOrderExecuted;
        EmployeePopup.OnReturnButtonClicked += OnReturnClicked;
    }

    private void OnDisable()
    {
        Employee.OnAnyEmployeeClicked -= OnEmployeeClicked;
        EmployeePopup.OnOrderToButtonClicked -= OnOrderToClicked;
        EmployeeOrderController.OnOrderExecuted -= OnOrderExecuted;
        EmployeePopup.OnReturnButtonClicked -= OnReturnClicked;
    }

    private void Start()
    {
        step2 = FindFirstObjectByType<ContainmentUnitTutorialStep>();
        if (autoStartAfterTutorial2)
        {
            StartCoroutine(RoutineCheckAndStart());
        }
    }

    private IEnumerator RoutineCheckAndStart()
    {
        // 1. Wait until Tutorial 2 finishes
        if (step2 != null)
        {
            while (!step2.IsCompleted)
            {
                yield return null;
            }
        }

        // 2. Jeda 5 detik setelah Tutorial 2
        CurrentPhase = TutorialPhase.WaitingDelay;
        FacilityHUD.ShowBroadcast("TUTORIAL 3: PERINTAH PERGERAKAN EMPLOYEE AKAN DIMULAI DALAM 5 DETIK...", "TUTORIAL", 4.5f);

        yield return new WaitForSeconds(delayAfterTutorial2);

        // 3. Start Phase 1
        StartPhase1();
    }

    public void StartPhase1()
    {
        targetEmployee = FindFirstObjectByType<Employee>();
        CurrentPhase = TutorialPhase.Phase1_SelectEmployee;
        overlayAlpha = 0f;
        targetAlpha = 0.85f;

        if (targetEmployee != null)
        {
            StartCoroutine(RoutinePanCameraTo(targetEmployee.transform.position));
        }

        FacilityHUD.ShowBroadcast("KLIK KIRI EMPLOYEE UNTUK MEMBUKA KARTU AKSI MEMINDAHKAN.", "TUTORIAL", 6f);
    }

    private void Update()
    {
        if (CurrentPhase == TutorialPhase.Inactive || CurrentPhase == TutorialPhase.WaitingDelay) return;

        if (CurrentPhase != TutorialPhase.Completed)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            UpdateSpotlightRect();
        }
        else
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
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_SelectEmployee:
            case TutorialPhase.Phase4_SelectEmployeeAgain:
                if (targetEmployee == null) targetEmployee = FindFirstObjectByType<Employee>();
                targetSpotlightRect = CalculateWorldObjectScreenRect(targetEmployee != null ? targetEmployee.gameObject : null);
                if (currentSpotlightRect.width <= 1f) currentSpotlightRect = targetSpotlightRect;
                break;

            case TutorialPhase.Phase2_ClickOrderTo:
                Button orderBtn = EmployeePopup.Instance != null ? EmployeePopup.Instance.OrderToButton : null;
                targetSpotlightRect = CalculateButtonScreenRect(orderBtn);
                break;

            case TutorialPhase.Phase3_ClickTargetRoom:
                if (targetRoom == null) PickTargetRoom();
                targetSpotlightRect = CalculateWorldObjectScreenRect(targetRoom != null ? targetRoom.gameObject : null);
                break;

            case TutorialPhase.Phase5_ClickReturnButton:
                Button returnBtn = EmployeePopup.Instance != null ? EmployeePopup.Instance.BackToDivisionButton : null;
                targetSpotlightRect = CalculateButtonScreenRect(returnBtn);
                break;
        }
    }

    private void PickTargetRoom()
    {
        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        if (allRooms == null || allRooms.Length == 0) return;

        Room empRoom = targetEmployee != null ? targetEmployee.CurrentRoom : null;
        List<Room> otherRooms = new List<Room>();

        foreach (Room r in allRooms)
        {
            if (r != null && r != empRoom)
            {
                otherRooms.Add(r);
            }
        }

        if (otherRooms.Count > 0)
        {
            targetRoom = otherRooms[Random.Range(0, otherRooms.Count)];
        }
        else
        {
            targetRoom = allRooms[0];
        }

        if (targetRoom != null)
        {
            // Pan kamera secara halus ke ruangan lain secara acak
            StartCoroutine(RoutinePanCameraTo(targetRoom.transform.position));
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
    private void OnEmployeeClicked(Employee emp)
    {
        if (CurrentPhase == TutorialPhase.Phase1_SelectEmployee)
        {
            targetEmployee = emp;
            CurrentPhase = TutorialPhase.Phase2_ClickOrderTo;
            FacilityHUD.ShowBroadcast("SEKARANG KLIK TOMBOL 'ORDER TO' UNTUK MEMILIH RUANGAN TUJUAN.", "TUTORIAL", 6f);
        }
        else if (CurrentPhase == TutorialPhase.Phase4_SelectEmployeeAgain)
        {
            targetEmployee = emp;
            CurrentPhase = TutorialPhase.Phase5_ClickReturnButton;
            FacilityHUD.ShowBroadcast("KLIK TOMBOL 'BACK TO DIVISION' UNTUK MENGEMBALIKAN EMPLOYEE.", "TUTORIAL", 6f);
        }
    }

    private void OnOrderToClicked()
    {
        if (CurrentPhase == TutorialPhase.Phase2_ClickOrderTo)
        {
            CurrentPhase = TutorialPhase.Phase3_ClickTargetRoom;
            PickTargetRoom();
            FacilityHUD.ShowBroadcast("KLIK KIRI RUANGAN INI UNTUK MEMERINTAHKAN EMPLOYEE BERJALAN KE SANA.", "TUTORIAL", 6f);
        }
    }

    private void OnOrderExecuted(Employee emp, Room room)
    {
        if (CurrentPhase == TutorialPhase.Phase3_ClickTargetRoom)
        {
            CurrentPhase = TutorialPhase.Phase4_SelectEmployeeAgain;
            if (emp != null)
            {
                StartCoroutine(RoutinePanCameraTo(emp.transform.position));
            }
            FacilityHUD.ShowBroadcast("BAGUS! SEKARANG KLIK KIRI EMPLOYEE LAGI UNTUK MENGEMBALIKANNYA.", "TUTORIAL", 6f);
        }
    }

    private void OnReturnClicked()
    {
        if (CurrentPhase == TutorialPhase.Phase5_ClickReturnButton)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        if (CurrentPhase == TutorialPhase.Completed) return;

        CurrentPhase = TutorialPhase.Completed;
        FacilityHUD.ShowBroadcast("BAGUS SKALI! KAMU TELAH BERHASIL MEMINDAHKAN & MENGEMBALIKAN EMPLOYEE.", "TUTORIAL", 7f);
    }

    // ── Rect Calculations ─────────────────────────────────────────────────────
    private Rect CalculateWorldObjectScreenRect(GameObject go)
    {
        if (go == null || Camera.main == null)
        {
            return new Rect(Screen.width * 0.4f, Screen.height * 0.4f, 120f, 120f);
        }

        Collider2D col = go.GetComponent<Collider2D>();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

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
            Vector3 pos = go.transform.position;
            minWorld = pos - new Vector3(0.8f, 1.2f, 0f);
            maxWorld = pos + new Vector3(0.8f, 1.2f, 0f);
        }

        Vector3 screenMin = Camera.main.WorldToScreenPoint(minWorld);
        Vector3 screenMax = Camera.main.WorldToScreenPoint(maxWorld);

        float minX = Mathf.Min(screenMin.x, screenMax.x) - 20f;
        float maxX = Mathf.Max(screenMin.x, screenMax.x) + 20f;
        float minY = Mathf.Min(screenMin.y, screenMax.y) - 20f;
        float maxY = Mathf.Max(screenMin.y, screenMax.y) + 20f;

        float w = Mathf.Max(maxX - minX, 80f);
        float h = Mathf.Max(maxY - minY, 80f);

        float guiY = Screen.height - maxY;

        return new Rect(minX, guiY, w, h);
    }

    private Rect CalculateButtonScreenRect(Button btn)
    {
        if (btn == null)
        {
            return new Rect(Screen.width * 0.42f, Screen.height * 0.48f, 160f, 45f);
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null)
        {
            return new Rect(Screen.width * 0.42f, Screen.height * 0.48f, 160f, 45f);
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
        float bannerW = 620f;
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
            case TutorialPhase.Phase1_SelectEmployee: return "TUTORIAL 3: PINDAHKAN EMPLOYEE (LANGKAH 1/4)";
            case TutorialPhase.Phase2_ClickOrderTo: return "TUTORIAL 3: PINDAHKAN EMPLOYEE (LANGKAH 2/4)";
            case TutorialPhase.Phase3_ClickTargetRoom: return "TUTORIAL 3: PINDAHKAN EMPLOYEE (LANGKAH 3/4)";
            case TutorialPhase.Phase4_SelectEmployeeAgain: return "TUTORIAL 3: KEMBALIKAN EMPLOYEE (LANGKAH 4/4)";
            case TutorialPhase.Phase5_ClickReturnButton: return "TUTORIAL 3: KEMBALIKAN EMPLOYEE (LANGKAH 4/4)";
            default: return "TUTORIAL 3: SELESAI!";
        }
    }

    private string GetInstructionText()
    {
        switch (CurrentPhase)
        {
            case TutorialPhase.Phase1_SelectEmployee: return "Klik Kiri pada Employee di scene";
            case TutorialPhase.Phase2_ClickOrderTo: return "Klik tombol 'ORDER TO' pada menu";
            case TutorialPhase.Phase3_ClickTargetRoom: return "Klik Kiri pada Ruangan ini untuk memindahkan Employee";
            case TutorialPhase.Phase4_SelectEmployeeAgain: return "Klik Kiri pada Employee lagi";
            case TutorialPhase.Phase5_ClickReturnButton: return "Klik tombol 'BACK TO DIVISION' untuk mengembalikan Employee";
            default: return "✓ Perintah Pergerakan & Return Employee Berhasil!";
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

        if (CurrentPhase == TutorialPhase.Phase1_SelectEmployee || CurrentPhase == TutorialPhase.Phase4_SelectEmployeeAgain || CurrentPhase == TutorialPhase.Phase3_ClickTargetRoom)
        {
            // Arrow pointing DOWN above object
            float arrowX = holeRect.center.x - 120f;
            float arrowY = holeRect.y - 55f - pulseOffset;

            string txt = (CurrentPhase == TutorialPhase.Phase3_ClickTargetRoom) ? "▼ KLIK RUANGAN DI SINI" : "▼ KLIK KIRI EMPLOYEE";
            GUI.Label(new Rect(arrowX, arrowY, 240f, 45f), txt, arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase2_ClickOrderTo)
        {
            // Arrow pointing to ORDER TO button
            float arrowX = holeRect.x - 190f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;
            GUI.Label(new Rect(arrowX, arrowY, 180f, 40f), "ORDER TO ➜", arrowStyle);
        }
        else if (CurrentPhase == TutorialPhase.Phase5_ClickReturnButton)
        {
            // Arrow pointing to RETURN button
            float arrowX = holeRect.x - 210f - pulseOffset;
            float arrowY = holeRect.center.y - 20f;
            GUI.Label(new Rect(arrowX, arrowY, 200f, 40f), "RETURN / BACK ➜", arrowStyle);
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
