using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Satu baris log observasi di MonsterInfoPopup, gaya "Observation Level" Lobotomy Corp :
/// terkunci ("Not Unlock") kalau belum completed, kebuka isinya begitu completed, dan ada indikator
/// glow/highlight kalau entry ini Manual & lagi bisa di-research sekarang.
///
/// Ditempel di prefab row yang di-instantiate dynamic oleh MonsterInfoPopup (bukan di scene).
/// Kalau project pakai TextMeshPro, tinggal ganti tipe field Text -> TMP_Text.
/// </summary>
public class ResearchEntryRow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image lockIcon;       // aktif kalau entry BELUM completed
    [SerializeField] private Image availableGlow;   // aktif kalau entry siap di-research sekarang

    private const string LOCKED_PLACEHOLDER = "Not Unlock";

    public void SetData(ResearchEntry entry, bool completed, bool researchable)
    {
        if (levelText != null)
            levelText.text = $"Lv. {entry.level}";

        if (resultText != null)
            resultText.text = completed ? entry.resultText : LOCKED_PLACEHOLDER;

        if (lockIcon != null)
            lockIcon.enabled = !completed;

        if (availableGlow != null)
            availableGlow.enabled = !completed && researchable;
    }
}