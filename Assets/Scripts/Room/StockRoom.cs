using UnityEngine;

public class StockRoom : Room
{
    [Header("Stock Settings")]
    [SerializeField] private int stock = 15;
    [SerializeField] private int maxStock = 15;
    [SerializeField] private float restockDuration = 20f;
    [SerializeField] private float takeStockDuration = 3f;  

    public float TakeStockDuration => takeStockDuration;

    public bool HasStock(int amount) => stock >= amount;

    private float restockTimer;

    public int Stock => stock;

    protected override void Start()
    {
        base.Start();

        stock = maxStock;
        restockTimer = restockDuration;
    }

    protected override void OnRoomUpdate()
    {
        if (stock < maxStock)
        {
            restockTimer -= Time.deltaTime;

            if (restockTimer <= 0f)
            {
                Restock();
                restockTimer = restockDuration;
            }
        }
    }

    /// <summary>
    /// Consume stock.
    /// Returns true if successful.
    /// </summary>
    /// 
    /// 
    public bool TakeStock(int amount)
    {
        if (stock < amount)
            return false;

        stock -= amount;

        Debug.Log($"[{RoomName}] Stock: {stock}/{maxStock}");

        return true;
    }

    public Coroutine BeginTakeStock(int amount, float duration, System.Action onFinished)
    {
        if (!TakeStock(amount))
            return null;

        return StartCoroutine(TakeStockRoutine(duration, onFinished));
    }

    private System.Collections.IEnumerator TakeStockRoutine(float duration, System.Action onFinished)
    {
        yield return new WaitForSeconds(duration);
        onFinished?.Invoke();
    }

    /// <summary>
    /// Restocks the room back to maximum stock.
    /// </summary>
    public void Restock()
    {
        stock = maxStock;

        Debug.Log($"[{RoomName}] Restocked to {stock}");
    }
}