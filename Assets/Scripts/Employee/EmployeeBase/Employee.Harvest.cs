using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus sistem Harvest : mulai proses harvest pada monster,
/// hitung durasi final, dan perintah tingkat tinggi GoHarvest.
/// </summary>
public partial class Employee
{
    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Coba mulai proses harvest pada monster target. Durasi FINAL dihitung lewat
    /// CalculateHarvestDuration() sebelum dioper ke MonsterBase. Return false kalau target
    /// null, growth-nya belum Overgrowth (belum &gt;100%), atau sedang harvest lain.
    ///
    /// PENTING: return true di sini artinya proses harvest BERHASIL DIMULAI, bukan berarti
    /// sudah selesai -- selesainya dilaporkan lewat MonsterBase.OnHarvestFinished (mirror FeedMonster/TryResearch).
    /// </summary>
    public virtual bool TryHarvest(MonsterBase target)
    {
        if (target == null)
            return false;
 
        float finalHarvestDuration = CalculateHarvestDuration(target);
 
        bool success = target.TryHarvest(finalHarvestDuration, this);
 
        if (success)
        {
            // Simpan durasi final ke progress bar (Employee.ProgressBar.cs) -- start time-nya
            // baru benar-benar dipatok saat state berubah ke Harvesting (dipanggil task sesudah ini).
            SetActionDuration(finalHarvestDuration);
 
            Debug.Log($"[Employee] {employeeName} mulai harvest {target.MonsterName} (durasi : {finalHarvestDuration}s).");
        }
        else
        {
            Debug.Log($"[Employee] {employeeName} gagal harvest {target.MonsterName} " +
                      $"(growth belum Overgrowth / sedang harvest lain).");
        }
 
        return success;
    }

    /// <summary>
    /// Menghitung durasi harvest FINAL (detik) untuk target monster, dari sudut pandang
    /// employee ini. Sama pola dengan CalculateFeedDuration/CalculateResearchDuration --
    /// override di sini kalau nanti mau ada multiplier per jenis employee/skill/level.
    ///
    /// Default: tidak ada modifikasi, sama persis dengan HarvestDuration bawaan monster.
    /// </summary>
    protected virtual float CalculateHarvestDuration(MonsterBase target)
    {
        float baseDuration = target.HarvestDuration;

        // Harvest = keahlian Botanist. Yang lain kena penalti.
        float duration = division != EmployeeDivision.Botanist
            ? baseDuration * offDivisionMultiplier
            : baseDuration;

        return duration * GetDivisionAssignmentWorkMultiplier();
    }

    //────────────────────────────────────────────────────────
    // High-level Commands
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Perintah lengkap: jalan ke unit target, lalu coba harvest begitu sampai.
    /// Cuma akan berhasil kalau growth monster-nya Overgrowth (&gt;100%) begitu employee sampai --
    /// kalau belum, task gagal (onFail) dan job dibatalkan (lihat OnTaskFail di Employee).
    ///
    /// Disusun lewat task queue (sama seperti GoFeed/GoResearch) supaya job ini tidak ketimpa
    /// diam-diam oleh perintah lain, dan otomatis batal kalau monster sudah tidak ada lagi di
    /// unit tersebut begitu employee sampai.
    /// </summary>
    public void GoHarvest(ContainmentUnit unit)
    {
        if (unit == null || !unit.HasMonster)
        {
            Debug.Log($"[Employee] {employeeName} batal harvest: unit tidak valid / tidak ada monster.");
            return;
        }

        MonsterBase capturedMonster = unit.Monster;

        ClearTasksAndInterrupt();

        EnqueueTask(new MoveToTask(
            () => capturedMonster.transform.position,
            () => unit != null && unit.HasMonster && unit.Monster == capturedMonster));

        EnqueueTask(new HarvestMonsterTask(unit, capturedMonster));

        BackToDivision();

        Debug.Log($"[Employee] {employeeName} menerima job: harvest {capturedMonster?.MonsterName}.");
    }
}