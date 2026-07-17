using UnityEngine;

/// <summary>
/// Menampilkan informasi Facility dan seluruh Room.
/// </summary>
public class FacilityHUD : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private Color panelColor = new Color(0, 0, 0, 0.75f);
    [SerializeField] private Color energyColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color tempColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color elecColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    private GUIStyle labelStyle;
    private GUIStyle titleStyle;

    private bool stylesInitialized;

    private void OnGUI()
    {
        InitStyles();

        Facility fac = Facility.Instance;

        if (fac == null)
            return;

        const float pad = 10f;
        const float panelWidth = 230f;
        float panelHeight = fac.IsBlackout ? 245f : 200f;

        DrawFacilityPanel(fac, pad, pad, panelWidth, panelHeight);

        float roomY = pad + panelHeight + 10f;

        for (int i = 0; i < fac.Rooms.Count; i++)
        {
            DrawRoomPanel(
                fac.Rooms[i],
                pad + i * (panelWidth + 8),
                roomY,
                panelWidth,
                80f);
        }

        DrawSelectionHint();
    }

    //====================================================

    private void DrawFacilityPanel(
        Facility fac,
        float x,
        float y,
        float w,
        float h)
    {
        GUI.color = panelColor;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(x + 8, y + 8, w - 16, h - 16));

        GUILayout.Label("◈ FACILITY STATUS", titleStyle);

        GUILayout.Space(5);

        if (fac.IsBlackout)
        {
            GUI.color = dangerColor;
            GUILayout.Box("⚠️ SYSTEM BLACKOUT ⚠️\nPower usage exceeded 100%!", GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(5);
        }

        DrawBar("⚡ Energy", fac.Energy, 100, energyColor);

        DrawTemperature(
            "🌡 Default Room Temp",
            fac.DefaultRoomTemperature);

        DrawBar(
            "🔌 Electricity",
            fac.Electricity,
            fac.MaxElectricity,
            elecColor);

        GUILayout.Space(5);

        GUILayout.Label(
            $"Rooms : {fac.Rooms.Count}",
            labelStyle);

        GUILayout.EndArea();
    }

    //====================================================

    private void DrawRoomPanel(
        Room room,
        float x,
        float y,
        float w,
        float h)
    {
        GUI.color = new Color(0.05f, 0.05f, 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(x + 6, y + 6, w - 12, h - 12));

        GUILayout.Label($"🏠 {room.RoomName}", titleStyle);

        GUI.color =
            room.Temperature > 35
            ? dangerColor
            : tempColor;

        GUILayout.Label(
            $"Temperature : {room.Temperature:F1}°C",
            labelStyle);

        GUI.color = Color.white;

        string info = room.GetHUDInfo();

        if (!string.IsNullOrEmpty(info))
        {
            GUILayout.Label(info, labelStyle);
        }

        GUILayout.EndArea();
    }

    //====================================================

    private void DrawBar(
        string title,
        float value,
        float max,
        Color color)
    {
        GUILayout.Label(title, labelStyle);

        Rect rect =
            GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));

        GUI.color = Color.gray;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color =
            value / max < 0.25f
            ? dangerColor
            : color;

        GUI.DrawTexture(
            new Rect(
                rect.x,
                rect.y,
                rect.width * Mathf.Clamp01(value / max),
                rect.height),
            Texture2D.whiteTexture);

        GUI.color = Color.white;

        GUILayout.Label($"{value:F1} / {max}", labelStyle);

        GUILayout.Space(2);
    }

    private void DrawTemperature(string title, float temp)
    {
        GUI.color = temp > 40
            ? dangerColor
            : tempColor;

        GUILayout.Label(
            $"{title} : {temp:F1}°C",
            labelStyle);

        GUI.color = Color.white;

        GUILayout.Space(2);
    }

    //====================================================

    private void DrawSelectionHint()
    {
        float h = 28f;

        GUI.color = new Color(0, 0, 0, 0.6f);

        GUI.DrawTexture(
            new Rect(0, Screen.height - h, Screen.width, h),
            Texture2D.whiteTexture);

        GUI.color = Color.white;

        GUI.Label(
            new Rect(
                8,
                Screen.height - h + 6,
                Screen.width,
                20),
            "Right Click : Select Employee   |   Left Click : Move Employee   |   Click Containment : Inspect Monster",
            labelStyle);
    }

    //====================================================

    private void InitStyles()
    {
        if (stylesInitialized)
            return;

        stylesInitialized = true;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
    }
}