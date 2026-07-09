using UnityEngine;

//==============================================================
// Task: ambil stok dari StockRoom lalu simpan sebagai makanan yang dibawa.
//==============================================================
public class TakeStockAndPickupTask : EmployeeTask
{
    private readonly StockRoom stockRoom;
    private readonly FoodType food;
    private readonly int amount;

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

        if (!stockRoom.TakeStock(amount))
        {
            Debug.Log($"[Employee] {employee.EmployeeName} gagal ambil stok, stok habis di {stockRoom.RoomName}.");
            onFail?.Invoke();
            return;
        }

        employee.PickUpFood(food);
        onComplete?.Invoke();
    }

    public void Cancel() { }
}