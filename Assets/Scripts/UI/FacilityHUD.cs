using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Menampilkan HUD Energy dan Electricity gaya Lobotomy Corporation di ujung kiri atas.
/// </summary>
public class FacilityHUD : MonoBehaviour
{
    private static FacilityHUD instance;
    public static FacilityHUD Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FacilityHUD>();
                if (instance == null)
                {
                    GameObject hudObj = new GameObject("FacilityHUD");
                    instance = hudObj.AddComponent<FacilityHUD>();
                }
            }
            return instance;
        }
    }

    [Header("Colors & Theme")]
    [SerializeField] private Color outerFrameBgColor = new Color(0.08f, 0.06f, 0.06f, 0.95f);
    [SerializeField] private Color borderGoldColor = new Color(0.85f, 0.58f, 0.22f, 1f);
    [SerializeField] private Color redBarBorderColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color redBarFillColor = new Color(0.88f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color elecBarFillColor = new Color(0.92f, 0.35f, 0.15f, 1f);
    [SerializeField] private Color redTextColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color roomPanelBgColor = new Color(0.05f, 0.05f, 0.15f, 0.85f);

    [Header("Broadcast Settings")]
    [SerializeField] private float broadcastWidth = 850f;
    [SerializeField] private float broadcastHeight = 76f;
    [SerializeField] private int broadcastSenderFontSize = 22;
    [SerializeField] private int broadcastMessageFontSize = 20;

    private GUIStyle digitalValueStyle;
    private GUIStyle badgeStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle iconStyle;
    private GUIStyle broadcastSenderStyle;
    private GUIStyle broadcastMessageStyle;

    private List<BroadcastMessage> activeBroadcasts = new List<BroadcastMessage>();

    private Texture2D whiteTex;
    private bool stylesInitialized;
    private bool isShowingReport = false;
    private bool isShowingGameOverReport = false;

    private Texture2D GetWhiteTexture()
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }
        return whiteTex;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        for (int i = activeBroadcasts.Count - 1; i >= 0; i--)
        {
            activeBroadcasts[i].Duration -= Time.deltaTime;
            if (activeBroadcasts[i].Duration <= 0f)
            {
                activeBroadcasts.RemoveAt(i);
            }
        }
    }

    public void AddBroadcast(string message, string sender = "System", float duration = 10f)
    {
        activeBroadcasts.Insert(0, new BroadcastMessage
        {
            Sender = sender,
            Message = message,
            Duration = duration
        });
    }

    public static void ShowBroadcast(string message, string sender = "System", float duration = 10f)
    {
        if (Instance != null)
        {
            Instance.AddBroadcast(message, sender, duration);
        }
        else
        {
            Debug.LogWarning($"[FacilityHUD] Cannot show broadcast because HUD instance is null. Sender: {sender}, Message: {message}");
        }
    }

    private void OnDisable()
    {
        if (whiteTex != null)
        {
            Destroy(whiteTex);
            whiteTex = null;
        }
    }

    private void OnGUI()
    {
        InitStyles();

        // Draw Broadcasts in the center left of the screen (larger size) - rendered even if Facility is null (e.g., in Tutorial scene)
        if (activeBroadcasts.Count > 0)
        {
            float broadcastSpacing = 10f;
            int count = activeBroadcasts.Count;
            float totalHeight = count * broadcastHeight + (count - 1) * broadcastSpacing;
            float startY = (Screen.height - totalHeight) / 2f;
            float bx = 16f;

            for (int i = 0; i < count; i++)
            {
                float by = startY + i * (broadcastHeight + broadcastSpacing);
                DrawBroadcastPanel(activeBroadcasts[i], bx, by, broadcastWidth, broadcastHeight);
            }
        }

        Facility fac = Facility.Instance;
        if (fac == null)
            return;

        float hudX = 12f;
        float hudY = 12f;
        float hudWidth = 480f;
        float hudHeight = 98f;

        // Draw Lobotomy Corp Energy & Electricity Bar HUD at top-left
        DrawLobotomyTopLeftHUD(fac, hudX, hudY, hudWidth, hudHeight);

        // Draw Overload Countdown at Top Center
        if (fac.OverloadTimer > 0f && !fac.IsBlackout)
        {
            float timeLeft = Mathf.Max(0f, fac.OverloadToleranceDuration - fac.OverloadTimer);
            DrawOverloadCountdown(timeLeft);
        }

        // Draw Room Panels below top-left HUD (no offset needed as broadcasts are center-right)
        float roomY = hudY + hudHeight + 15f;
        const float roomPanelWidth = 220f;

        for (int i = 0; i < fac.Rooms.Count; i++)
        {
            DrawRoomPanel(
                fac.Rooms[i],
                hudX + i * (roomPanelWidth + 8),
                roomY,
                roomPanelWidth,
                80f);
        }

        DrawSelectionHint();

        // 1. Lose Condition: Semua employee telah gugur
        if (fac.IsAllEmployeesDead)
        {
            if (isShowingGameOverReport)
            {
                DrawGameOverReportPopup(fac);
            }
            else
            {
                DrawGameOverPopup(fac);
            }
        }
        // 2. Win Condition: Target energi terpenuhi
        else if (fac.Energy >= fac.MaxEnergy && fac.MaxEnergy > 0)
        {
            if (isShowingReport)
            {
                DrawDayReportPopup(fac);
            }
            else
            {
                DrawDayCompletePopup();
            }
        }
    }

    private void DrawLobotomyTopLeftHUD(Facility fac, float x, float y, float width, float height)
    {
        // 1. Draw Outer Dark Stepped Frame Container
        DrawRect(new Rect(x, y, width, height), outerFrameBgColor);

        // Outer Gold/Bronze Border Outline
        DrawThickOutline(new Rect(x, y, width, height), borderGoldColor, 2);

        // Decorative notched corner accents
        DrawRect(new Rect(x - 2, y + 8, 4, height - 16), borderGoldColor);
        DrawRect(new Rect(x + width - 2, y + 8, 4, height - 16), borderGoldColor);

        // Decorative horizontal gold lines (Lobotomy Stepped Frame trim)
        DrawRect(new Rect(x + 6, y + 3, width - 12, 1), borderGoldColor * 0.8f);
        DrawRect(new Rect(x + 6, y + height - 4, width - 12, 1), borderGoldColor * 0.8f);

        const float badgeWidth = 125f;
        const float gearBoxWidth = 40f;

        // ==========================================
        // TOP ROW: ENERGY (LABEL BADGE + PROGRESS BAR + GEAR)
        // ==========================================
        float row1Y = y + 10f;
        float bar1Width = width - badgeWidth - gearBoxWidth - 36f;

        // Badge Label: ⚡ ENERGY
        DrawIconBadge(new Rect(x + 12f, row1Y, badgeWidth, 34f), "⚡ ENERGY", redBarBorderColor);

        // Energy Progress Bar (Original code used 100 as max energy)
        float maxEnergyVal = (fac.MaxEnergy > 0 && fac.MaxEnergy != 1000f) ? fac.MaxEnergy : 100f;
        DrawLobotomyBar(
            new Rect(x + 12f + badgeWidth + 4f, row1Y, bar1Width, 34f),
            fac.Energy,
            maxEnergyVal,
            redBarFillColor,
            redBarBorderColor,
            redTextColor
        );

        // Gear Box Ornament
        float gearX = x + 12f + badgeWidth + 4f + bar1Width + 6f;
        DrawGearOrnament(new Rect(gearX, row1Y - 2f, 38f, 38f));

        // ==========================================
        // BOTTOM ROW: ELECTRICITY (LABEL BADGE + PROGRESS BAR)
        // ==========================================
        float row2Y = y + 52f;
        float bar2Width = width - badgeWidth - 28f;

        // Badge Label: 🔌 ELECTRICITY
        DrawIconBadge(new Rect(x + 12f, row2Y, badgeWidth, 34f), "🔌 ELECTRICITY", borderGoldColor);

        if (fac.IsBlackout)
        {
            // Blackout state bar with pulsing warning
            float pulse = Mathf.PingPong(Time.time * 5f, 1f);
            Color alertColor = Color.Lerp(dangerColor, new Color(0.4f, 0f, 0f, 1f), pulse);

            DrawBlackoutBar(
                new Rect(x + 12f + badgeWidth + 4f, row2Y, bar2Width, 34f),
                alertColor
            );
        }
        else
        {
            // Electricity Progress Bar
            DrawLobotomyBar(
                new Rect(x + 12f + badgeWidth + 4f, row2Y, bar2Width, 34f),
                fac.Electricity,
                fac.MaxElectricity,
                elecBarFillColor,
                redBarBorderColor,
                redTextColor
            );
        }
    }

    private void DrawIconBadge(Rect badgeRect, string title, Color borderColor)
    {
        // Dark background
        DrawRect(badgeRect, new Color(0.04f, 0.03f, 0.03f, 0.95f));
        // Double outline
        DrawThickOutline(badgeRect, borderColor, 2);

        // Shadow text
        GUIStyle shadowStyle = new GUIStyle(badgeStyle)
        {
            normal = { textColor = new Color(0f, 0f, 0f, 0.9f) }
        };
        GUI.Label(new Rect(badgeRect.x + 1, badgeRect.y + 1, badgeRect.width, badgeRect.height), title, shadowStyle);

        // Main text
        GUI.Label(badgeRect, title, badgeStyle);
    }

    private void DrawLobotomyBar(
        Rect barRect,
        float currentVal,
        float maxVal,
        Color fillColor,
        Color borderColor,
        Color textColor)
    {
        // Black inner container
        DrawRect(barRect, new Color(0.04f, 0.03f, 0.03f, 0.95f));

        // Outer Red Border Box
        DrawThickOutline(barRect, borderColor, 2);

        // Inner subtle glow stroke
        Rect innerRect = new Rect(barRect.x + 3, barRect.y + 3, barRect.width - 6, barRect.height - 6);
        DrawThickOutline(innerRect, new Color(borderColor.r, borderColor.g, borderColor.b, 0.35f), 1);

        // Fill Progress Bar
        float fillPct = Mathf.Clamp01(maxVal > 0 ? currentVal / maxVal : 0f);
        float fillWidth = (barRect.width - 8) * fillPct;
        if (fillWidth > 1f)
        {
            Rect fillRect = new Rect(barRect.x + 4, barRect.y + 4, fillWidth, barRect.height - 8);
            DrawRect(fillRect, fillColor);

            // Highlight gradient line at top of fill
            DrawRect(new Rect(fillRect.x, fillRect.y, fillRect.width, 2), new Color(1f, 0.85f, 0.85f, 0.4f));
        }

        // Display string with values & percentage, e.g. "36 / 100 (36%)"
        int pctInt = Mathf.RoundToInt(fillPct * 100f);
        string displayText = $"{Mathf.RoundToInt(currentVal)} / {Mathf.RoundToInt(maxVal)}  ({pctInt}%)";

        // Draw shadow text for high contrast readability
        GUIStyle shadowStyle = new GUIStyle(digitalValueStyle)
        {
            normal = { textColor = new Color(0f, 0f, 0f, 0.95f) }
        };
        GUI.Label(new Rect(barRect.x + 1, barRect.y + 2, barRect.width, barRect.height), displayText, shadowStyle);

        // Draw main digital text
        GUIStyle mainStyle = new GUIStyle(digitalValueStyle)
        {
            normal = { textColor = textColor }
        };
        GUI.Label(barRect, displayText, mainStyle);
    }

    private void DrawBlackoutBar(Rect barRect, Color alertColor)
    {
        DrawRect(barRect, new Color(0.08f, 0.02f, 0.02f, 0.95f));
        DrawThickOutline(barRect, alertColor, 2);

        GUIStyle alertStyle = new GUIStyle(digitalValueStyle)
        {
            fontSize = 12,
            normal = { textColor = alertColor }
        };

        GUI.Label(barRect, "⚠️ SYSTEM BLACKOUT - POWER OVERLOAD ⚠️", alertStyle);
    }

    private void DrawOverloadCountdown(float timeLeft)
    {
        float w = 350f;
        float h = 60f;
        float x = (Screen.width - w) / 2f;
        float y = 20f;

        Rect panelRect = new Rect(x, y, w, h);
        
        float pulse = Mathf.PingPong(Time.time * 4f, 1f);
        Color warningColor = Color.Lerp(new Color(0.8f, 0.2f, 0.1f, 1f), dangerColor, pulse);

        DrawRect(panelRect, new Color(0.08f, 0.02f, 0.02f, 0.95f));
        DrawThickOutline(panelRect, warningColor, 2);

        GUIStyle style = new GUIStyle(digitalValueStyle)
        {
            fontSize = 18,
            normal = { textColor = warningColor }
        };

        GUI.Label(panelRect, $"⚠️ POWER OVERLOAD ⚠️\nBlackout in {timeLeft:F1}s", style);
    }

    private void DrawGearOrnament(Rect gearRect)
    {
        // Dark background square
        DrawRect(gearRect, new Color(0.06f, 0.04f, 0.04f, 0.95f));
        // Gold border box
        DrawThickOutline(gearRect, borderGoldColor, 2);

        // Inner circle/box detail
        Rect innerBox = new Rect(gearRect.x + 5, gearRect.y + 5, gearRect.width - 10, gearRect.height - 10);
        DrawRect(innerBox, new Color(0.12f, 0.08f, 0.05f, 1f));
        DrawThickOutline(innerBox, new Color(0.9f, 0.3f, 0.2f, 0.8f), 1);

        // Gear center circle placeholder/icon
        GUI.Label(gearRect, "⚙️", iconStyle);
    }

    private void DrawRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, GetWhiteTexture());
        GUI.color = Color.white;
    }

    private void DrawThickOutline(Rect rect, Color color, int thickness)
    {
        DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawRoomPanel(
        Room room,
        float x,
        float y,
        float w,
        float h)
    {
        DrawRect(new Rect(x, y, w, h), roomPanelBgColor);
        DrawThickOutline(new Rect(x, y, w, h), new Color(0.2f, 0.4f, 0.6f, 0.5f), 1);

        GUILayout.BeginArea(new Rect(x + 6, y + 6, w - 12, h - 12));

        GUILayout.Label($"🏠 {room.RoomName}", titleStyle);

        GUI.color = room.Temperature > 35 ? dangerColor : new Color(0.4f, 0.85f, 1f);
        GUILayout.Label($"Temperature : {room.Temperature:F1}°C", labelStyle);
        GUI.color = Color.white;

        string info = room.GetHUDInfo();
        if (!string.IsNullOrEmpty(info))
        {
            GUILayout.Label(info, labelStyle);
        }

        GUILayout.EndArea();
    }

    private void DrawSelectionHint()
    {
        float h = 28f;
        DrawRect(new Rect(0, Screen.height - h, Screen.width, h), new Color(0f, 0f, 0f, 0.7f));

        GUI.Label(
            new Rect(8, Screen.height - h + 6, Screen.width, 20),
            "Right Click : Select Employee   |   Left Click : Move Employee   |   Click Containment : Inspect Monster",
            labelStyle);
    }

    private void DrawDayCompletePopup()
    {
        float w = 350f;
        float h = 180f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        Rect popupRect = new Rect(x, y, w, h);
        
        DrawRect(popupRect, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        DrawThickOutline(popupRect, borderGoldColor, 3);

        GUIStyle popupTitleStyle = new GUIStyle(digitalValueStyle)
        {
            fontSize = 20,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
        };
        GUI.Label(new Rect(x, y + 40f, w, 30f), "Energi sudah penuh", popupTitleStyle);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        
        float btnW = 200f;
        float btnH = 45f;
        float btnX = x + (w - btnW) / 2f;
        float btnY = y + 100f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Selesaikan Hari", buttonStyle))
        {
            Debug.Log("Tombol 'Selesaikan Hari' ditekan.");
            isShowingReport = true;
        }
    }

    private void DrawDayReportPopup(Facility fac)
    {
        float w = 600f;
        float h = 450f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        Rect popupRect = new Rect(x, y, w, h);
        
        DrawRect(popupRect, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        DrawThickOutline(popupRect, borderGoldColor, 3);

        GUIStyle popupTitleStyle = new GUIStyle(digitalValueStyle)
        {
            fontSize = 24,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
        };
        GUI.Label(new Rect(x, y + 20f, w, 30f), "LAPORAN HARIAN", popupTitleStyle);

        // Perhitungan Uang
        float maxEnergy = fac.MaxEnergy;
        float totalEnergy = fac.Energy;
        float surplusEnergy = Mathf.Max(0f, totalEnergy - maxEnergy);
        float money = (maxEnergy * 2f) + (surplusEnergy * 1.1f);

        GUIStyle textStyle = new GUIStyle(labelStyle)
        {
            fontSize = 18,
            alignment = TextAnchor.UpperLeft
        };
        
        float contentX = x + 30f;
        float contentY = y + 80f;
        
        GUI.Label(new Rect(contentX, contentY, w - 60f, 30f), $"Uang yang didapat: {money:F1}", textStyle);
        
        contentY += 40f;
        GUI.Label(new Rect(contentX, contentY, w - 60f, 30f), "Daftar Karyawan Mati:", textStyle);
        
        // Tempat untuk list karyawan mati (placeholder)
        DrawRect(new Rect(contentX, contentY + 30f, w - 60f, 180f), new Color(0.05f, 0.05f, 0.05f, 0.8f));
        
        if (fac.DeadEmployeesReport.Count == 0)
        {
            GUI.Label(new Rect(contentX + 10f, contentY + 40f, w - 80f, 30f), "- (Belum ada data karyawan mati)", labelStyle);
        }
        else
        {
            float listY = contentY + 40f;
            foreach (var record in fac.DeadEmployeesReport)
            {
                GUI.Label(new Rect(contentX + 10f, listY, w - 80f, 20f), $"- {record.EmployeeName} ({record.CauseOfDeath})", labelStyle);
                listY += 20f;
                if (listY > contentY + 190f) break; // Jangan gambar kalau kepanjangan
            }
        }
        
        // Tombol-tombol di bawah
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        
        float btnW = 150f;
        float btnH = 45f;
        float totalBtnW = (btnW * 3) + 40f; // 3 tombol, margin 20f
        float startBtnX = x + (w - totalBtnW) / 2f;
        float btnY = y + h - 70f;
        
        if (GUI.Button(new Rect(startBtnX, btnY, btnW, btnH), "Next Day", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Next Day ditekan");
            UnityEngine.SceneManagement.SceneManager.LoadScene("EmployeeAssignment");
        }
        
        if (GUI.Button(new Rect(startBtnX + btnW + 20f, btnY, btnW, btnH), "Restart", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Restart ditekan");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        if (GUI.Button(new Rect(startBtnX + (btnW * 2) + 40f, btnY, btnW, btnH), "Main Menu", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Main Menu ditekan");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void DrawGameOverPopup(Facility fac)
    {
        float w = 450f;
        float h = 210f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        Rect popupRect = new Rect(x, y, w, h);
        
        // Dark crimson background & bright red outline
        DrawRect(popupRect, new Color(0.12f, 0.04f, 0.04f, 0.96f));
        DrawThickOutline(popupRect, new Color(0.9f, 0.2f, 0.2f, 1f), 3);

        GUIStyle popupTitleStyle = new GUIStyle(digitalValueStyle)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.35f, 0.35f, 1f) }
        };
        GUI.Label(new Rect(x, y + 25f, w, 30f), "SEMUA KARYAWAN TELAH GUGUR", popupTitleStyle);

        GUIStyle subtitleStyle = new GUIStyle(labelStyle)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
        };
        GUI.Label(new Rect(x + 20f, y + 65f, w - 40f, 30f), "Tidak ada karyawan tersisa untuk mengelola fasilitas.", subtitleStyle);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        
        float btnW = 220f;
        float btnH = 45f;
        float btnX = x + (w - btnW) / 2f;
        float btnY = y + 130f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Lihat Laporan Gugur", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Tombol 'Lihat Laporan Gugur' ditekan.");
            isShowingGameOverReport = true;
        }
    }

    private void DrawGameOverReportPopup(Facility fac)
    {
        float w = 600f;
        float h = 460f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        Rect popupRect = new Rect(x, y, w, h);
        
        DrawRect(popupRect, new Color(0.12f, 0.04f, 0.04f, 0.96f));
        DrawThickOutline(popupRect, new Color(0.9f, 0.2f, 0.2f, 1f), 3);

        GUIStyle popupTitleStyle = new GUIStyle(digitalValueStyle)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.35f, 0.35f, 1f) }
        };
        GUI.Label(new Rect(x, y + 20f, w, 30f), "LAPORAN KEGAGALAN FASILITAS", popupTitleStyle);

        GUIStyle textStyle = new GUIStyle(labelStyle)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
        };
        
        float contentX = x + 30f;
        float contentY = y + 70f;
        
        GUI.Label(new Rect(contentX, contentY, w - 60f, 25f), $"Energy Terkumpul: {fac.Energy:F1} / {fac.MaxEnergy:F1}", textStyle);
        contentY += 30f;
        GUI.Label(new Rect(contentX, contentY, w - 60f, 25f), $"Total Karyawan Gugur: {fac.DeadEmployeesReport.Count} orang", textStyle);
        
        contentY += 35f;
        GUI.Label(new Rect(contentX, contentY, w - 60f, 25f), "Daftar Karyawan yang Gugur:", textStyle);
        
        // List container
        DrawRect(new Rect(contentX, contentY + 28f, w - 60f, 170f), new Color(0.05f, 0.02f, 0.02f, 0.85f));
        
        if (fac.DeadEmployeesReport.Count == 0)
        {
            GUI.Label(new Rect(contentX + 10f, contentY + 38f, w - 80f, 30f), "- (Semua karyawan gugur)", labelStyle);
        }
        else
        {
            float listY = contentY + 38f;
            foreach (var record in fac.DeadEmployeesReport)
            {
                GUI.Label(new Rect(contentX + 10f, listY, w - 80f, 20f), $"💀 {record.EmployeeName} - Penyebab: {record.CauseOfDeath}", labelStyle);
                listY += 22f;
                if (listY > contentY + 180f) break;
            }
        }
        
        // Tombol-tombol di bawah
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        
        float btnW = 160f;
        float btnH = 45f;
        float totalBtnW = (btnW * 2) + 20f; // 2 tombol: Restart & Main Menu
        float startBtnX = x + (w - totalBtnW) / 2f;
        float btnY = y + h - 65f;
        
        if (GUI.Button(new Rect(startBtnX, btnY, btnW, btnH), "🔄 Ulangi Hari", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Ulangi Hari (Restart) ditekan");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        if (GUI.Button(new Rect(startBtnX + btnW + 20f, btnY, btnW, btnH), "🏠 Main Menu", buttonStyle))
        {
            Debug.Log("[FacilityHUD] Main Menu ditekan");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized)
            return;

        stylesInitialized = true;

        badgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.4f) }
        };

        digitalValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        iconStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        broadcastSenderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = broadcastSenderFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        broadcastMessageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = broadcastMessageFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
    }

    private void DrawBroadcastPanel(BroadcastMessage msg, float x, float y, float w, float h)
    {
        float alpha = 1f;
        if (msg.Duration < 1.0f)
        {
            alpha = msg.Duration;
        }

        Color originalColor = GUI.color;
        GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * alpha);

        // Draw outer box
        // Dark frame background
        DrawRect(new Rect(x, y, w, h), new Color(0.06f, 0.04f, 0.04f, 0.95f));
        // Golden/Bronze outline
        DrawThickOutline(new Rect(x, y, w, h), borderGoldColor, 3);

        // Draw Sender Badge
        float pad = 8f;
        float badgeW = 160f;
        float badgeH = h - (pad * 2f);
        Rect badgeRect = new Rect(x + pad, y + pad, badgeW, badgeH);
        
        // Draw sender badge background
        DrawRect(badgeRect, new Color(0.04f, 0.03f, 0.03f, 0.95f));
        
        // Select badge border/text color based on sender (can override)
        Color badgeColor = borderGoldColor;
        if (msg.Sender == "System")
        {
            badgeColor = new Color(1f, 0.3f, 0.3f, 1f); // Neon red for System
        }
        else
        {
            badgeColor = new Color(0.3f, 0.8f, 1f, 1f); // Neon blue for others/custom
        }
        
        DrawThickOutline(badgeRect, badgeColor, 2);

        // Sender text
        broadcastSenderStyle.normal.textColor = badgeColor;
        GUI.Label(badgeRect, msg.Sender, broadcastSenderStyle);

        // Draw Message
        float msgX = x + badgeW + (pad * 2f);
        float msgW = w - badgeW - (pad * 3f);
        Rect msgRect = new Rect(msgX, y + pad, msgW, badgeH);
        broadcastMessageStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        GUI.Label(msgRect, msg.Message, broadcastMessageStyle);

        GUI.color = originalColor;
    }
}

public class BroadcastMessage
{
    public string Sender;
    public string Message;
    public float Duration;
}