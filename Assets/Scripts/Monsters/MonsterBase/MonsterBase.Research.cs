using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Syarat GrowthState (atau custom) supaya sebuah ResearchEntry BISA diselesaikan.
/// Dicek terhadap MonsterBase.CurrentGrowthState, kecuali Custom (dicek lewat method virtual
/// CheckCustomResearchCondition, bisa berupa event flag, suhu ruangan, mood, atau apapun --
/// TIDAK harus "rahasia", cuma nama generik buat "kondisi di luar GrowthState").
/// </summary>
public enum ResearchCondition
{
    Any,            // Bisa kapan saja, tidak terikat GrowthState
    Growing,        // GrowthState harus persis Growing
    Overgrowth,     // GrowthState harus persis Overgrowth
    Mutated,        // GrowthState harus persis Mutated
    Custom          // Kondisi c    ustom (event/state apapun di luar GrowthState) -- override lewat CheckCustomResearchCondition
}

/// <summary>
/// Cara sebuah ResearchEntry diselesaikan begitu ResearchCondition-nya terpenuhi.
/// </summary>
public enum ResearchTrigger
{
    Manual, // Perlu aksi eksplisit lewat TryResearch()/TryResearchNext() selagi kondisi terpenuhi
    Auto    // Otomatis selesai sendiri begitu kondisi terpenuhi, tanpa aksi research apapun
}

/*
 * Konvensi level (contoh dari desain awal, BEBAS disesuaikan tiap prefab lewat Inspector,
 * "level" sendiri cuma label urutan/pengelompokan, tidak dipakai langsung dalam logika) :
 *
 *   1-3  : Any          + Manual  -> bisa di-research kapan saja
 *   4-6  : Growing      + Manual  -> butuh growth persis Growing
 *   7-8  : Overgrowth    + Manual  -> butuh persis Overgrowth
 *   9    : Mutated       + Auto    -> langsung ke-unlock begitu Mutated, TANPA aksi research
 *   10   : Mutated       + Manual  -> butuh aksi research SELAGI Mutated
 *   11+  : Custom        + Manual/Auto -> syarat bebas lewat CheckCustomResearchCondition, mis. :
 *            - auto-unlock begitu sebuah event/fungsi tertentu pernah ke-trigger (mis. efek mood-zero)
 *            - auto-unlock begitu suhu ruangan >= entry.customValue (ambang diatur per-entry di Inspector)
 *            - kombinasi apapun, termasuk yang benar-benar "rahasia"/tersembunyi dari player
 */

/// <summary>
/// Satu entry hasil research, dikonfigurasi lewat Inspector, beda-beda tiap prefab child class
/// (sama seperti GrowthStateSprite).
/// </summary>
[System.Serializable]
public struct ResearchEntry
{
    [Tooltip("Label level buat pengelompokan/urutan di Inspector (mis. 1-11). TIDAK dipakai langsung " +
             "dalam logika unlock -- yang menentukan kapan entry ini bisa diselesaikan adalah " +
             "'condition' & 'trigger' di bawah.")]
    public int level;

    [Tooltip("ID unik entry ini dalam monster ini. Dipakai untuk TryResearch(id) dan referensi save data.")]
    public string id;

    [TextArea(2, 4)]
    [Tooltip("Teks hasil research yang ditampilkan ke player, mis. 'Tanaman ini bereaksi negatif terhadap panas di atas 30°C'.")]
    public string resultText;

    [Tooltip("Syarat GrowthState (atau Custom) supaya entry ini bisa diselesaikan.")]
    public ResearchCondition condition;

    [Tooltip("Manual = perlu aksi research selagi syarat terpenuhi. Auto = langsung selesai sendiri begitu syarat terpenuhi.")]
    public ResearchTrigger trigger;

    [Tooltip("Nilai ambang opsional buat condition=Custom (mis. suhu ruangan minimal). Diabaikan " +
             "untuk condition selain Custom -- interpretasinya sepenuhnya di kode CheckCustomResearchCondition.")]
    public float customValue;
}

/// <summary>
/// Bagian MonsterBase yang mengurus sistem Research : daftar entry, syarat unlock per entry,
/// dan aksi research (manual maupun auto).
/// </summary>
public partial class MonsterBase
{
    //────────────────────────────────────────────────────────
    // Research
    //────────────────────────────────────────────────────────

    [Header("Research")]
    [Tooltip("Daftar hasil research yang bisa didapat dari monster ini. Beda-beda tiap prefab child class. " +
             "'level' cuma label pengelompokan -- yang menentukan kapan entry bisa diselesaikan adalah " +
             "'condition' & 'trigger' di tiap entry.")]
    [SerializeField] private ResearchEntry[] researchEntries;

    private readonly HashSet<string> completedResearchIds = new HashSet<string>();

    //────────────────────────────────────────────────────────
    // Events
    //────────────────────────────────────────────────────────

    public System.Action<ResearchEntry> OnResearchCompleted;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    /// <summary>Daftar mentah semua ResearchEntry yang dikonfigurasi lewat Inspector untuk monster ini.</summary>
    public IReadOnlyList<ResearchEntry> ResearchEntries => researchEntries;

    //────────────────────────────────────────────────────────
    // Condition Evaluation
    //────────────────────────────────────────────────────────

    /// <summary>Cek syarat condition sebuah entry terhadap CurrentGrowthState (atau Custom hook).</summary>
    private bool EvaluateResearchCondition(ResearchEntry entry)
    {
        switch (entry.condition)
        {
            case ResearchCondition.Any:
                return true;

            case ResearchCondition.Growing:
                return CurrentGrowthState == GrowthState.Growing;

            case ResearchCondition.Overgrowth:
                return CurrentGrowthState == GrowthState.Overgrowth;

            case ResearchCondition.Mutated:
                return CurrentGrowthState == GrowthState.Mutated;

            case ResearchCondition.Custom:
                return CheckCustomResearchCondition(entry);

            default:
                return false;
        }
    }

    private bool TryFindResearchEntry(string id, out ResearchEntry entry)
    {
        foreach (var e in researchEntries ?? System.Array.Empty<ResearchEntry>())
        {
            if (e.id == id)
            {
                entry = e;
                return true;
            }
        }

        entry = default;
        return false;
    }

    //────────────────────────────────────────────────────────
    // Completion
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Cek semua entry bertipe trigger Auto, selesaikan otomatis yang syaratnya sudah terpenuhi
    /// tapi belum selesai. Dipanggil tiap frame dari Update() (MonsterBase.cs) dan sekali di Awake,
    /// jadi otomatis mendukung kondisi apapun (GrowthState maupun Custom) tanpa perlu instrumentasi
    /// manual tambahan di child class.
    /// </summary>
    protected void CheckAutoResearch()
    {
        foreach (var entry in researchEntries ?? System.Array.Empty<ResearchEntry>())
        {
            if (entry.trigger != ResearchTrigger.Auto) continue;
            if (IsResearchCompleted(entry.id)) continue;
            if (!EvaluateResearchCondition(entry)) continue;

            CompleteResearch(entry.id);
        }
    }

    private void CompleteResearch(string id)
    {
        if (completedResearchIds.Contains(id))
            return;

        if (!TryFindResearchEntry(id, out var entry))
            return;

        completedResearchIds.Add(id);

        OnResearchCompleted?.Invoke(entry);

        Debug.Log($"[{MonsterName}] Research selesai (level {entry.level}) : {entry.resultText}");
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Override di child class untuk syarat ResearchCondition.Custom -- event flag, suhu ruangan
    /// (lewat entry.customValue), mood, atau kondisi apapun di luar GrowthState. Dispatch berdasarkan
    /// entry.id kalau ada lebih dari satu Custom entry (lihat contoh di MonsterTest1234).
    /// </summary>
    protected virtual bool CheckCustomResearchCondition(ResearchEntry entry) => false;

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>Sudah pernah diselesaikan atau belum.</summary>
    public bool IsResearchCompleted(string id) => completedResearchIds.Contains(id);

    /// <summary>True kalau entry ini trigger Manual, syaratnya terpenuhi SEKARANG, dan belum selesai.</summary>
    public bool CanResearch(string id)
    {
        if (!TryFindResearchEntry(id, out var entry))
            return false;

        return entry.trigger == ResearchTrigger.Manual
            && !IsResearchCompleted(id)
            && EvaluateResearchCondition(entry);
    }

    /// <summary>
    /// Coba selesaikan satu research entry spesifik lewat aksi manual (mis. dipanggil dari Employee).
    /// Return true kalau berhasil diselesaikan sekarang.
    /// </summary>
    public bool TryResearch(string id)
    {
        if (!CanResearch(id))
        {
            Debug.Log($"[{MonsterName}] Research '{id}' gagal : syarat belum terpenuhi atau sudah selesai.");
            return false;
        }

        CompleteResearch(id);
        return true;
    }

    /// <summary>
    /// Cari & selesaikan entry Manual pertama (urut dari level terkecil) yang syaratnya sudah
    /// terpenuhi dan belum selesai. Berguna kalau pemanggil tidak peduli entry spesifik yang mana,
    /// cuma mau "research aja yang bisa sekarang".
    /// </summary>
    public bool TryResearchNext()
    {
        ResearchEntry? next = null;

        foreach (var entry in researchEntries ?? System.Array.Empty<ResearchEntry>())
        {
            if (entry.trigger != ResearchTrigger.Manual) continue;
            if (IsResearchCompleted(entry.id)) continue;
            if (!EvaluateResearchCondition(entry)) continue;

            if (next == null || entry.level < next.Value.level)
                next = entry;
        }

        if (next == null)
        {
            Debug.Log($"[{MonsterName}] Tidak ada research yang bisa dilakukan sekarang.");
            return false;
        }

        CompleteResearch(next.Value.id);
        return true;
    }

    /// <summary>Entry Manual yang syaratnya terpenuhi sekarang & belum selesai -- siap di-research.</summary>
    public List<ResearchEntry> GetResearchable()
    {
        var list = new List<ResearchEntry>();

        foreach (var entry in researchEntries ?? System.Array.Empty<ResearchEntry>())
        {
            if (entry.trigger == ResearchTrigger.Manual
                && !IsResearchCompleted(entry.id)
                && EvaluateResearchCondition(entry))
            {
                list.Add(entry);
            }
        }

        return list;
    }

    /// <summary>Semua entry yang sudah selesai (baik lewat Manual maupun Auto).</summary>
    public List<ResearchEntry> GetCompletedResearch()
    {
        var list = new List<ResearchEntry>();

        foreach (var entry in researchEntries ?? System.Array.Empty<ResearchEntry>())
        {
            if (IsResearchCompleted(entry.id))
                list.Add(entry);
        }

        return list;
    }
}