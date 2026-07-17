using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus sistem Research : mulai aksi research pada monster,
/// hitung durasi final, dan perintah tingkat tinggi GoResearch.
/// </summary>
public partial class Employee
{
    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Coba mulai satu aksi research pada monster target.
    /// - researchId null/kosong -> coba research APA SAJA yang available sekarang (TryResearchNext).
    /// - researchId diisi        -> coba entry spesifik itu (TryResearch(id)).
    /// Durasi FINAL dihitung lewat CalculateResearchDuration() sebelum dioper ke MonsterBase.
    /// Return false kalau target null, atau tidak ada research yang syaratnya terpenuhi sekarang
    /// (termasuk kalau monster sedang dalam proses research lain).
    ///
    /// PENTING: return true di sini artinya proses research BERHASIL DIMULAI, bukan berarti
    /// sudah selesai -- selesainya dilaporkan lewat MonsterBase.OnResearchFinished (mirror FeedMonster).
    /// </summary>
    public virtual bool TryResearch(MonsterBase target, string researchId = null)
    {
        if (target == null)
            return false;

        float finalResearchDuration = CalculateResearchDuration(target);

        bool success = string.IsNullOrEmpty(researchId)
            ? target.TryResearchNext(finalResearchDuration)
            : target.TryResearch(researchId, finalResearchDuration);

        if (success)
        {
            // Simpan durasi final ke progress bar (Employee.ProgressBar.cs) -- start time-nya
            // baru benar-benar dipatok saat state berubah ke Researching (dipanggil task sesudah ini).
            SetActionDuration(finalResearchDuration);

            Debug.Log($"[Employee] {employeeName} mulai research pada {target.MonsterName} (durasi : {finalResearchDuration}s).");
        }
        else
        {
            Debug.Log($"[Employee] {employeeName} gagal research pada {target.MonsterName} " +
                      $"(syarat belum terpenuhi / sudah selesai / sedang research lain / tidak ada yang available sekarang).");
        }

        return success;
    }

    /// <summary>
    /// Menghitung durasi research FINAL (detik) untuk target monster, dari sudut pandang
    /// employee ini yang sedang melakukan research.
    ///
    /// Sengaja dipisah dari MonsterBase.ResearchDuration (sama seperti CalculateFeedDuration)
    /// supaya monster tidak perlu tahu siapa yang meneliti. Faktor "siapa yang bekerja"
    /// (jenis employee, level, skill, buff, dsb) nantinya tinggal ditambahkan di sini lewat
    /// override, tanpa menyentuh MonsterBase maupun subclass Employee lain.
    ///
    /// Default: tidak ada modifikasi, sama persis dengan ResearchDuration bawaan monster.
    /// </summary>
    protected virtual float CalculateResearchDuration(MonsterBase target)
    {
        float baseDuration = target.ResearchDuration;

        // Research = keahlian Researcher. Yang lain kena penalti.
        return division != EmployeeDivision.Researcher
            ? baseDuration * offDivisionMultiplier
            : baseDuration;
    }

    //────────────────────────────────────────────────────────
    // High-level Commands
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Perintah lengkap: jalan ke unit target, lalu coba research begitu sampai.
    /// - researchId null/kosong -> research apa saja yang available (TryResearchNext) begitu sampai.
    /// - researchId diisi        -> coba entry spesifik itu begitu sampai.
    ///
    /// Disusun lewat task queue (sama seperti GoFeed) supaya job ini tidak ketimpa diam-diam
    /// oleh perintah lain, dan otomatis batal (onFail) kalau pas sampai ternyata monster
    /// sudah tidak ada lagi di unit tersebut.
    /// </summary>
    public void GoResearch(ContainmentUnit unit, string researchId = null)
    {
        if (unit == null || !unit.HasMonster)
        {
            Debug.Log($"[Employee] {employeeName} batal research: unit tidak valid / tidak ada monster.");
            return;
        }

        MonsterBase capturedMonster = unit.Monster;

        ClearTasksAndInterrupt();

        EnqueueTask(new MoveToTask(
            () => capturedMonster.transform.position,
            () => unit != null && unit.HasMonster && unit.Monster == capturedMonster));

        EnqueueTask(new ResearchMonsterTask(unit, capturedMonster, researchId));

        BackToDivision();

        Debug.Log($"[Employee] {employeeName} menerima job: research {capturedMonster?.MonsterName}.");
    }
}