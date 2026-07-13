using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup full-screen ala "Observation Info" Lobotomy Corporation.
/// VERSI SEKARANG (sengaja disederhanakan dulu) : cuma nampilin nama monster + daftar
/// research. Entry yang sudah completed nampilin resultText-nya, yang belum completed
/// tampil "Not Unlock" (lihat ResearchEntryRow).
///
/// PENTING soal placement GameObject: sama seperti ContainmentPopup, script ini harus
/// nempel di GameObject yang SELALU AKTIF, bukan di popupRoot yang di-toggle aktif/nonaktif
/// (lihat komentar di PopupBase.cs untuk alasannya).
///
/// Setup UI yang disaranin (full-screen 1 layar, gaya Lob Corp) :
///   Canvas (fullscreen)
///    └─ InfoPopupRoot   (di-toggle oleh PopupBase; Image background gelap semi-transparan)
///         ├─ Panel_Header
///         │    └─ NameText
///         ├─ ScrollView_Entries
///         │    └─ Viewport
///         │         └─ Content          <- entriesContent (VerticalLayoutGroup + ContentSizeFitter)
///         └─ CloseButton
///
/// Portrait/growth state/mood/suhu belum dipasang di versi ini -- tinggal tambah lagi
/// field-nya + panggil di RefreshHeader() kalau nanti dibutuhkan.
///
/// ASUMSI yang perlu dicek ulang terhadap kode kalian yang sebenarnya :
///   - Cara ambil MonsterBase dari ContainmentUnit diasumsikan lewat GetComponentInChildren
///     sebagai fallback aman -- ganti ke properti asli (mis. unit.Monster) kalau sudah ada.
/// </summary>
public class MonsterInfoPopup : PopupBase
{
    public static MonsterInfoPopup Instance { get; private set; }

    [Header("Header")]
    [SerializeField] private TMP_Text nameText;

    [Header("Entries")]
    [Tooltip("Parent transform tempat row entry di-instantiate (Content dari ScrollView).")]
    [SerializeField] private Transform entriesContent;
    [Tooltip("Prefab row (harus punya component ResearchEntryRow).")]
    [SerializeField] private ResearchEntryRow entryRowPrefab;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private ContainmentUnit targetUnit;
    private MonsterBase targetMonster;

    private readonly List<ResearchEntryRow> spawnedRows = new List<ResearchEntryRow>();

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (targetMonster != null)
            targetMonster.OnResearchCompleted -= HandleResearchCompleted;
    }

    /// <summary>
    /// Buka info popup untuk unit tertentu. Dipanggil dari ContainmentPopup.OnInfoClicked,
    /// atau dari mana saja yang punya referensi ContainmentUnit.
    /// </summary>
    public void Open(ContainmentUnit unit)
    {
        targetUnit = unit;
        targetMonster = unit != null ? unit.GetComponentInChildren<MonsterBase>() : null;

        if (targetMonster == null)
        {
            Debug.LogError("[MonsterInfoPopup] Tidak menemukan MonsterBase pada unit yang dibuka.");
            return;
        }

        targetMonster.OnResearchCompleted += HandleResearchCompleted;

        RefreshHeader();
        RefreshEntries();

        base.Open();
    }

    protected override void OnClosed()
    {
        if (targetMonster != null)
            targetMonster.OnResearchCompleted -= HandleResearchCompleted;

        targetUnit = null;
        targetMonster = null;

        ClearRows();
    }

    /// <summary>Entry baru selesai (manual maupun auto) selagi popup ini kebuka -> refresh live.</summary>
    private void HandleResearchCompleted(ResearchEntry _)
    {
        RefreshEntries();
    }

    //────────────────────────────────────────────────────────
    // Header
    //────────────────────────────────────────────────────────

    private void RefreshHeader()
    {
        if (nameText != null)
            nameText.text = targetMonster.MonsterName;
    }

    //────────────────────────────────────────────────────────
    // Entries
    //────────────────────────────────────────────────────────

    private void RefreshEntries()
    {
        ClearRows();

        if (targetMonster == null || entriesContent == null || entryRowPrefab == null)
            return;

        var ordered = targetMonster.ResearchEntries
            .OrderBy(e => e.level)
            .ToList();

        foreach (var entry in ordered)
        {
            bool completed = targetMonster.IsResearchCompleted(entry.id);
            bool researchable = !completed
                && entry.trigger == ResearchTrigger.Manual
                && targetMonster.CanResearch(entry.id);

            var row = Instantiate(entryRowPrefab, entriesContent);
            row.SetData(entry, completed, researchable);

            spawnedRows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedRows.Clear();
    }
}