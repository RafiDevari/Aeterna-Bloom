using UnityEngine;

//==============================================================
// Task: coba selesaikan satu research entry pada monster target, dengan
// validasi ulang (monster/unit bisa berubah selama employee dalam perjalanan) --
// pola sama seperti FeedMonsterTask.
//
// Beda dengan FeedMonsterTask: TryResearch()/TryResearchNext() di MonsterBase
// SYNCHRONOUS (tidak ada durasi/event selesai seperti OnFeedFinished), jadi
// onComplete/onFail langsung dipanggil dalam Start(), tidak perlu subscribe
// event apapun atau nunggu tick berikutnya.
//
// - researchId null/kosong -> coba TryResearchNext() (research apa saja yang available)
// - researchId diisi        -> coba TryResearch(id) buat entry spesifik itu
//==============================================================
public class ResearchMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;
    private readonly string researchId;

    public ResearchMonsterTask(ContainmentUnit unit, MonsterBase targetMonster, string researchId = null)
    {
        this.unit = unit;
        this.targetMonster = targetMonster;
        this.researchId = researchId;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (unit == null || !unit.HasMonster || unit.Monster != targetMonster)
        {
            onFail?.Invoke();
            return;
        }

        if (!employee.TryResearch(targetMonster, researchId))
        {
            onFail?.Invoke();
            return;
        }

        onComplete?.Invoke();
    }

    public void Cancel()
    {
        // Task ini synchronous -- selesai/gagal langsung di Start(), tidak ada
        // state atau subscription event yang perlu dibersihkan saat di-cancel.
    }
}