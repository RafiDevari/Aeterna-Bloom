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
    [SerializeField] private Button killBeeButton;

    private ContainmentUnit targetUnit;
    private MonsterBase targetMonster;

    private readonly List<ResearchEntryRow> spawnedRows = new List<ResearchEntryRow>();

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (killBeeButton != null)
            killBeeButton.onClick.AddListener(OnKillBeeClicked);
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

        Lebah activeLebah = FindLebahInUnit(unit);
        if (killBeeButton != null)
        {
            killBeeButton.gameObject.SetActive(activeLebah != null);
        }

        base.Open();
    }

    private Lebah FindLebahInUnit(ContainmentUnit unit)
    {
        if (unit == null) return null;

        Lebah lebah = unit.GetComponentInChildren<Lebah>();
        if (lebah != null && !lebah.IsDead) return lebah;

        Lebah[] allBees = Object.FindObjectsByType<Lebah>(FindObjectsSortMode.None);
        foreach (var l in allBees)
        {
            if (l != null && !l.IsDead && (l.TargetUnit == unit || Vector3.Distance(l.transform.position, unit.transform.position) < 2.5f))
            {
                return l;
            }
        }
        return null;
    }

    private void OnKillBeeClicked()
    {
        Lebah activeLebah = FindLebahInUnit(targetUnit);
        if (activeLebah == null || activeLebah.IsDead) return;

        Lebah capturedLebah = activeLebah;
        Close();

        if (EmployeeSelectPopup.Instance != null)
        {
            EmployeeSelectPopup.Instance.Open(selectedEmp =>
            {
                if (selectedEmp != null && capturedLebah != null && !capturedLebah.IsDead)
                {
                    selectedEmp.EnqueueTask(new KillPestTask(capturedLebah));
                    Debug.Log($"[MonsterInfoPopup] {selectedEmp.EmployeeName} ditugaskan untuk membunuh Lebah pada {targetMonster?.MonsterName}.");
                }
            }, typeof(EmployeeSecurity));
        }
    }

    private void OnGUI()
    {
        if (!IsOpen || targetUnit == null) return;

        Lebah activeLebah = FindLebahInUnit(targetUnit);
        if (activeLebah != null && killBeeButton == null)
        {
            float w = 200f;
            float h = 40f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 80f;

            if (GUI.Button(new Rect(x, y, w, h), "KILL BEE (BASMI LEBAH)"))
            {
                OnKillBeeClicked();
            }
        }
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