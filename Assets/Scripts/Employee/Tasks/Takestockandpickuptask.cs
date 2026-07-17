using UnityEngine;

//==============================================================
// Task: ambil stok dari StockRoom (dengan durasi) lalu simpan
// sebagai makanan yang dibawa.
//==============================================================
public class TakeStockAndPickupTask : EmployeeTask
{
    private readonly StockRoom stockRoom;
    private readonly FoodType food;
    private readonly int amount;

    private Employee employee;
    private Coroutine runningRoutine;
    private bool isWaitingForStockToFinish;

    public TakeStockAndPickupTask(StockRoom stockRoom, FoodType food, int amount)
    {
        this.stockRoom = stockRoom;
        this.food = food;
        this.amount = amount;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (stockRoom == null)
        {
            Debug.Log($"[Employee] {employee.EmployeeName} gagal ambil stok: stock room sudah tidak ada.");
            onFail?.Invoke();
            return;
        }

        if (!stockRoom.HasStock(amount))
        {
            Debug.Log($"[Employee] {employee.EmployeeName} gagal ambil stok, stok habis di {stockRoom.RoomName}.");
            onFail?.Invoke();
            return;
        }

        float duration = employee.CalculateTakeStockDuration(stockRoom, food, amount);

        this.employee = employee;

        // Simpan durasi final ke progress bar (Employee.ProgressBar.cs) -- start time-nya
        // baru benar-benar dipatok saat state berubah ke TakingStock (baris di bawah ini).
        employee.SetActionDuration(duration);

        employee.SetState(EmployeeState.TakingStock);
        isWaitingForStockToFinish = true;

        runningRoutine = stockRoom.BeginTakeStock(amount, duration, () =>
        {
            if (!isWaitingForStockToFinish)
                return;

            isWaitingForStockToFinish = false;

            employee.PickUpFood(food);

            // PENTING: balikin state ke Idle di sini. Tanpa ini, currentState employee
            // nyangkut selamanya di TakingStock (progress bar & sistem lain yang gantung
            // pada CurrentState jadi ikut nyangkut) -- sama bug yang sebelumnya ada di
            // FeedMonsterTask/ResearchMonsterTask/HarvestMonsterTask.
            employee.SetState(EmployeeState.Idle);

            Debug.Log($"[Employee] {employee.EmployeeName} selesai ambil stok {food} x{amount} dari {stockRoom.RoomName} (durasi : {duration}s).");

            onComplete?.Invoke();
        });

        if (runningRoutine == null)
        {
            // Race jarang: stok habis persis sebelum BeginTakeStock sempat reserve.
            Debug.Log($"[Employee] {employee.EmployeeName} gagal ambil stok, stok habis di {stockRoom.RoomName}.");
            isWaitingForStockToFinish = false;
            employee.SetState(EmployeeState.Idle);
            onFail?.Invoke();
        }
    }

    public void Cancel()
    {
        if (!isWaitingForStockToFinish)
            return;

        isWaitingForStockToFinish = false;

        if (runningRoutine != null)
            stockRoom.StopCoroutine(runningRoutine);

        // Job diinterupsi di tengah jalan (mis. player klik pindah manual) --
        // tetap balikin state, jangan biarkan nyangkut di TakingStock.
        employee?.SetState(EmployeeState.Idle);

        // Catatan: stok sudah ter-reserve (dikurangi) saat BeginTakeStock dipanggil.
        // Kalau task ini di-cancel di tengah jalan, stok itu tidak dikembalikan.
        // Tambahkan StockRoom.RefundStock(amount) di sini kalau itu perlu.
    }
}