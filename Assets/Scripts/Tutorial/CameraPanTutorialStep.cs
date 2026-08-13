using UnityEngine;
using System.Collections;

/// <summary>
/// Step Tutorial 1: Geser Kamera.
/// Menampilkan overlay agak hitam dengan gambar mouse di tengah (tombol klik kanan disorot).
/// User harus melakukan klik kanan dan drag kamera untuk menyelesaikan step ini.
/// </summary>
public class CameraPanTutorialStep : MonoBehaviour
{
    [Header("Tutorial Settings")]
    [SerializeField] private bool autoStartOnEnable = true;
    [Tooltip("Jarak akumulasi geseran mouse/kamera (dalam pixel/unit) yang dibutuhkan untuk menyelesaikan tutorial.")]
    [SerializeField] private float requiredDragDistance = 35f;

    public bool IsActive { get; private set; } = false;
    public bool IsCompleted { get; private set; } = false;

    private float accumulatedDragDistance = 0f;
    private Vector3 lastMousePos;
    private Vector3 lastCamPos;
    private float overlayAlpha = 0f;
    private float targetAlpha = 0.85f;
    private float fadeSpeed = 3.5f;

    private string currentHintMessage = "Tahan Klik Kanan & Geser Mouse Sekarang";
    private float wrongClickTimer = 0f;

    // Generated Textures & Styles
    private Texture2D mouseRightClickTex;
    private Texture2D darkOverlayTex;
    private Texture2D panelBgTex;
    private Texture2D whiteTex;
    private GUIStyle headerStyle;
    private GUIStyle instructionStyle;
    private GUIStyle hintStyle;
    private GUIStyle statusStyle;

    private bool stylesInitialized = false;

    private void OnEnable()
    {
        if (autoStartOnEnable && !IsCompleted)
        {
            StartStep();
        }
    }

    /// <summary>
    /// Memulai tutorial geser kamera.
    /// </summary>
    public void StartStep()
    {
        IsActive = true;
        IsCompleted = false;
        accumulatedDragDistance = 0f;
        overlayAlpha = 0f;
        targetAlpha = 0.85f;
        currentHintMessage = "Tahan Klik Kanan & Geser Mouse Sekarang";

        if (Camera.main != null)
        {
            lastCamPos = Camera.main.transform.position;
        }
        lastMousePos = Input.mousePosition;
    }

    private void Update()
    {
        if (!IsActive) return;

        // Smooth transition alpha
        if (!IsCompleted)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            CheckUserDragInput();
        }
        else
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, 0f, Time.deltaTime * fadeSpeed);
            if (overlayAlpha <= 0.005f)
            {
                IsActive = false;
            }
        }

        // Timer untuk pesan peringatan jika salah klik
        if (wrongClickTimer > 0f)
        {
            wrongClickTimer -= Time.deltaTime;
            if (wrongClickTimer <= 0f && !IsCompleted)
            {
                currentHintMessage = "Tahan Klik Kanan & Geser Mouse Sekarang";
            }
        }
    }

    private void CheckUserDragInput()
    {
        // Deteksi jika user menekan Klik Kiri bukan Klik Kanan
        if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
        {
            currentHintMessage = "⚠️ Gunakan KLIK KANAN (Tombol Kanan Mouse), bukan Klik Kiri!";
            wrongClickTimer = 2.5f;
        }

        // Deteksi Klik Kanan (MouseButton 1)
        if (Input.GetMouseButton(1))
        {
            Vector3 currentMousePos = Input.mousePosition;
            float deltaMouse = Vector3.Distance(currentMousePos, lastMousePos);

            // Akumulasi jarak pergerakan mouse saat klik kanan ditekan
            accumulatedDragDistance += deltaMouse;

            // Tambahkan juga jika posisi kamera sesungguhnya berpindah
            if (Camera.main != null)
            {
                float camMovement = Vector3.Distance(Camera.main.transform.position, lastCamPos);
                accumulatedDragDistance += camMovement * 40f;
            }

            if (wrongClickTimer <= 0f)
            {
                currentHintMessage = ">> SEDANG MENGGESER KAMERA... <<";
            }

            if (accumulatedDragDistance >= requiredDragDistance)
            {
                CompleteStep();
            }
        }

        lastMousePos = Input.mousePosition;
        if (Camera.main != null)
        {
            lastCamPos = Camera.main.transform.position;
        }
    }

    private void CompleteStep()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        currentHintMessage = "✓ KAMERA BERHASIL DIGESER!";

        // Tampilkan broadcast konfirmasi via FacilityHUD
        FacilityHUD.ShowBroadcast("BAGUS! KAMERA BERHASIL DIGESER. GUNAKAN KLIK KANAN KAPAN PUN UNTUK NAVIGASI.", "TUTORIAL", 6f);
    }

    private void OnGUI()
    {
        if (!IsActive || overlayAlpha <= 0.001f) return;

        InitStylesIfNeeded();

        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, overlayAlpha);

        // 1. Full Screen Darkened Overlay (Scene Agak Hitam)
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkOverlayTex);

        // 2. Central Tutorial Window Box
        float boxWidth = 520f;
        float boxHeight = 450f;
        float boxX = (Screen.width - boxWidth) * 0.5f;
        float boxY = (Screen.height - boxHeight) * 0.5f;

        // Panel Background & Golden Border
        GUI.DrawTexture(new Rect(boxX, boxY, boxWidth, boxHeight), panelBgTex);
        DrawBorder(new Rect(boxX, boxY, boxWidth, boxHeight), new Color(0.92f, 0.68f, 0.15f, overlayAlpha), 3);

        // Header Title
        GUI.Label(new Rect(boxX + 20, boxY + 22, boxWidth - 40, 36), "TUTORIAL 1: GESER KAMERA", headerStyle);

        // 3. Mouse Image with Right-Click Highlighted (In the Center)
        float mouseImgWidth = 190f;
        float mouseImgHeight = 190f;
        float mouseX = boxX + (boxWidth - mouseImgWidth) * 0.5f;
        float mouseY = boxY + 70f;

        // Dynamic pulse animation on the right mouse button highlight
        float pulse = Mathf.PingPong(Time.time * 3.5f, 0.25f);
        GUI.color = new Color(1f, 1f + pulse, 1f, overlayAlpha);
        GUI.DrawTexture(new Rect(mouseX, mouseY, mouseImgWidth, mouseImgHeight), mouseRightClickTex);
        GUI.color = new Color(1f, 1f, 1f, overlayAlpha);

        // Draw animated drag arrow indicators (◄ ►)
        DrawDragArrows(mouseX, mouseY, mouseImgWidth, mouseImgHeight, overlayAlpha);

        // 4. Main Instruction Text
        string mainInstruction = IsCompleted
            ? "✓ BERHASIL! KAMERA TELAH DIGESER"
            : "KLIK KANAN & DRAG MOUSE\nUNTUK MENGGESER KAMERA";

        GUI.Label(new Rect(boxX + 20, boxY + 270f, boxWidth - 40, 55), mainInstruction, instructionStyle);

        // 5. Progress Bar & Interactive Hint
        float progressPct = Mathf.Clamp01(accumulatedDragDistance / requiredDragDistance);

        float pbW = 340f;
        float pbH = 14f;
        float pbX = boxX + (boxWidth - pbW) * 0.5f;
        float pbY = boxY + 348f;

        // Bar frame
        GUI.DrawTexture(new Rect(pbX, pbY, pbW, pbH), darkOverlayTex);
        Color barBorderColor = IsCompleted ? new Color(0.3f, 0.85f, 0.4f, overlayAlpha) : new Color(0.85f, 0.65f, 0.2f, overlayAlpha);
        DrawBorder(new Rect(pbX, pbY, pbW, pbH), barBorderColor, 1);

        // Bar fill
        if (progressPct > 0f)
        {
            Color fillColor = IsCompleted ? new Color(0.2f, 0.85f, 0.35f, 0.95f) : new Color(0.95f, 0.72f, 0.15f, 0.95f);
            GUI.DrawTexture(new Rect(pbX + 1, pbY + 1, (pbW - 2) * progressPct, pbH - 2), GetSolidTex(fillColor));
        }

        // Status Hint Text below progress bar
        GUI.Label(new Rect(boxX + 15, boxY + 372f, boxWidth - 30, 32), currentHintMessage, hintStyle);

        if (IsCompleted)
        {
            GUI.Label(new Rect(boxX + 15, boxY + 405f, boxWidth - 30, 25), "[ MELANJUTKAN TUTORIAL... ]", statusStyle);
        }

        GUI.color = oldColor;
    }

    private void DrawDragArrows(float mouseX, float mouseY, float w, float h, float alpha)
    {
        float offset = Mathf.Sin(Time.time * 5f) * 8f;
        Color arrowColor = IsCompleted ? new Color(0.3f, 0.9f, 0.4f, alpha * 0.9f) : new Color(1f, 0.82f, 0.2f, alpha * 0.9f);

        GUIStyle arrowStyle = new GUIStyle(headerStyle)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold
        };

        Color oldCol = GUI.color;
        GUI.color = arrowColor;

        // Left Arrow
        GUI.Label(new Rect(mouseX - 45f - offset, mouseY + h * 0.38f, 40, 40), "◄", arrowStyle);
        // Right Arrow
        GUI.Label(new Rect(mouseX + w + 10f + offset, mouseY + h * 0.38f, 40, 40), "►", arrowStyle);

        GUI.color = oldCol;
    }

    private void DrawBorder(Rect rect, Color color, int thickness)
    {
        Texture2D tex = GetSolidTex(color);
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
        // Right
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
        if (stylesInitialized && mouseRightClickTex != null) return;

        // 1. Dark Overlay Texture (0.85 opacity dark black)
        darkOverlayTex = new Texture2D(1, 1);
        darkOverlayTex.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.07f, 0.88f));
        darkOverlayTex.Apply();

        // 2. Panel Background Texture
        panelBgTex = new Texture2D(1, 1);
        panelBgTex.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 0.96f));
        panelBgTex.Apply();

        // 3. Generate Procedural Mouse Right-Click Texture (256x256)
        mouseRightClickTex = GenerateMouseRightClickTexture();

        // 4. GUI Styles
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 22;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(0.96f, 0.78f, 0.2f, 1f);

        instructionStyle = new GUIStyle();
        instructionStyle.fontSize = 18;
        instructionStyle.fontStyle = FontStyle.Bold;
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        instructionStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle();
        hintStyle.fontSize = 14;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 1f);

        statusStyle = new GUIStyle();
        statusStyle.fontSize = 13;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.normal.textColor = new Color(0.3f, 0.85f, 0.4f, 1f);

        stylesInitialized = true;
    }

    /// <summary>
    /// Menghasilkan Texture2D murni berukuran 256x256 berupa gambar mouse 
    /// dengan Tombol Klik Kanan yang disorot terang (Amber/Gold Glowing) dan berlabel "KLIK KANAN".
    /// </summary>
    private Texture2D GenerateMouseRightClickTexture()
    {
        int w = 256;
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(0, 0, 0, 0);
        Color bodyColor = new Color(0.14f, 0.16f, 0.20f, 1f);
        Color leftBtnColor = new Color(0.20f, 0.23f, 0.28f, 1f);
        Color rightBtnColor = new Color(0.98f, 0.68f, 0.12f, 1f); // Bright Amber/Gold for Right Click
        Color rightBtnHighlight = new Color(1f, 0.88f, 0.35f, 1f);
        Color borderCol = new Color(0.45f, 0.48f, 0.55f, 1f);
        Color wheelCol = new Color(0.65f, 0.68f, 0.75f, 1f);
        Color textCol = new Color(0.1f, 0.1f, 0.1f, 1f);

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

        float cx = 128f;
        float cy = 128f;
        float bodyRadiusX = 65f;
        float bodyRadiusY = 95f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / bodyRadiusX;
                float dy = (y - cy) / bodyRadiusY;
                float distSq = dx * dx + dy * dy;

                if (distSq <= 1f)
                {
                    // In mouse body
                    bool isBorder = (distSq >= 0.88f);
                    bool isTopHalf = (y >= 120);
                    bool isRightSide = (x > 128);
                    bool isCenterSplit = (x >= 126 && x <= 130 && y >= 115);
                    bool isHorizontalSplit = (y >= 115 && y <= 119);
                    bool isScrollWheel = (x >= 122 && x <= 134 && y >= 140 && y <= 180);

                    Color c = bodyColor;

                    if (isTopHalf)
                    {
                        if (isRightSide)
                        {
                            // RIGHT CLICK BUTTON (Highlighted)
                            c = rightBtnColor;
                            // Inner glow/gradient on right button
                            if (distSq < 0.7f && y > 135)
                            {
                                c = Color.Lerp(rightBtnColor, rightBtnHighlight, 0.4f);
                            }
                        }
                        else
                        {
                            // LEFT CLICK BUTTON (Dimmed)
                            c = leftBtnColor;
                        }
                    }

                    if (isHorizontalSplit || isCenterSplit)
                    {
                        c = borderCol;
                    }

                    if (isScrollWheel)
                    {
                        c = wheelCol;
                    }

                    if (isBorder)
                    {
                        c = borderCol;
                    }

                    pixels[y * w + x] = c;
                }
            }
        }

        tex.SetPixels(pixels);

        // Gambarkan tanda/simbol "R" atau "KLIK KANAN" pada tombol kanan mouse secara piksel murni
        DrawRightClickSymbolOnTexture(tex, 158, 148, rightBtnHighlight);

        tex.Apply();
        return tex;
    }

    private void DrawRightClickSymbolOnTexture(Texture2D tex, int startX, int startY, Color col)
    {
        // Gambar lingkaran putih/kuning terang dengan simbol klik di tombol kanan
        int radius = 18;
        Color centerHighlight = new Color(1f, 1f, 1f, 0.95f);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int px = startX + x;
                    int py = startY + y;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        if (x * x + y * y <= (radius - 3) * (radius - 3))
                        {
                            tex.SetPixel(px, py, centerHighlight);
                        }
                        else
                        {
                            tex.SetPixel(px, py, col);
                        }
                    }
                }
            }
        }
    }
}
