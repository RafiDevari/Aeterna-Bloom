using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus sistem Feeding : ambil makanan, beri makan monster,
/// hitung durasi final, dan perintah tingkat tinggi GoFeed.
/// </summary>
public partial class Employee
{
    [Header("Feeding")]
    [SerializeField] private FoodType carriedFood;
    [SerializeField] private bool hasFood = false;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    public FoodType CarriedFood => carriedFood;

    public bool HasFood => hasFood;

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Employee mengambil satu jenis makanan untuk dibawa.
    /// Menimpa makanan sebelumnya kalau belum sempat dipakai.
    /// </summary>
    public void PickUpFood(FoodType food)
    {
        carriedFood = food;
        hasFood = true;

        Debug.Log($"[Employee] {employeeName} mengambil makanan : {food}");
    }

    /// <summary>
    /// Beri makan monster target dengan makanan yang sedang dibawa.
    /// Return false kalau employee tidak sedang membawa makanan.
    /// </summary>
    public virtual bool FeedMonster(MonsterBase target)
    {
        if (target == null)
            return false;

        if (!hasFood)
        {
            Debug.Log($"[Employee] {employeeName} tidak membawa makanan untuk diberikan.");
            return false;
        }

        float finalFeedDuration = CalculateFeedDuration(target);

        if (!target.Feed(carriedFood, finalFeedDuration))
            return false;

        Debug.Log($"[Employee] {employeeName} memberi makan {target.MonsterName} dengan {carriedFood} (durasi : {finalFeedDuration}s).");

        hasFood = false;

        return true;
    }

    /// <summary>
    /// Menghitung durasi makan FINAL (detik) untuk target monster, dari sudut
    /// pandang employee ini yang sedang memberi makan.
    ///
    /// Sengaja dipisah dari MonsterBase.FeedDuration supaya monster tidak perlu
    /// tahu siapa yang memberinya makan. Semua faktor yang berhubungan dengan
    /// "siapa yang bekerja" (jenis employee, level, skill, buff, dsb) nantinya
    /// tinggal ditambahkan di sini lewat override, tanpa menyentuh MonsterBase
    /// maupun subclass Employee lain.
    ///
    /// Default: tidak ada modifikasi, sama persis dengan FeedDuration bawaan monster.
    /// </summary>
    protected virtual float CalculateFeedDuration(MonsterBase target)
    {
        float baseDuration = target.FeedDuration;

        // Feed = keahlian Botanist. Researcher yang mengerjakan ini kena penalti.
        return division == EmployeeDivision.Researcher
            ? baseDuration * offDivisionMultiplier
            : baseDuration;
    }

    protected internal virtual float CalculateTakeStockDuration(StockRoom stockRoom, FoodType food, int amount)
    {
        float baseDuration = stockRoom.TakeStockDuration;

        return division == EmployeeDivision.Researcher
            ? baseDuration * offDivisionMultiplier
            : baseDuration;
    }

    //────────────────────────────────────────────────────────
    // High-level Commands
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Perintah lengkap: jalan ke stock room, ambil stok, jalan ke monster, lalu beri makan.
    /// Disusun sebagai rangkaian task di task queue, bukan nested callback,
    /// sehingga job ini tidak akan ketimpa diam-diam oleh perintah lain
    /// (perintah lain akan lewat ClearTasksAndInterrupt terlebih dahulu).
    /// </summary>
    public void GoFeed(ContainmentUnit unit, FoodType food, int amount = 1)
    {
        if (unit == null || !unit.HasMonster)
        {
            Debug.Log($"[Employee] {employeeName} batal: unit tidak valid / tidak ada monster.");
            return;
        }

        if (Facility.Instance == null)
        {
            Debug.Log($"[Employee] {employeeName} batal: Facility tidak ditemukan.");
            return;
        }

        StockRoom stockRoom = Facility.Instance.FindNearestStockRoom(transform.position);

        if (stockRoom == null)
        {
            Debug.Log($"[Employee] {employeeName} batal: tidak ada stock room dengan stok tersedia.");
            return;
        }

        MonsterBase capturedMonster = unit.Monster;

        ClearTasksAndInterrupt();

        EnqueueTask(new MoveToTask(
            () => stockRoom.transform.position,
            () => stockRoom != null));

        EnqueueTask(new TakeStockAndPickupTask(stockRoom, food, amount));

        EnqueueTask(new MoveToTask(
            () => capturedMonster.transform.position,
            () => unit != null && unit.HasMonster && unit.Monster == capturedMonster));

        EnqueueTask(new FeedMonsterTask(unit, capturedMonster));

        BackToDivision();

        Debug.Log($"[Employee] {employeeName} menerima job: ambil stok lalu beri makan {capturedMonster?.MonsterName}.");
    }
}