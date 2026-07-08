using UnityEngine;

public class StockRoom : Room
{
    [Header("Stock Settings")]
    [SerializeField] private int stock = 15;
    [SerializeField] private int maxStock = 15;
    [SerializeField] private float restockDuration = 20f;

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
    public bool TakeStock(int amount)
    {
        if (stock < amount)
            return false;

        stock -= amount;

        Debug.Log($"[{RoomName}] Stock: {stock}/{maxStock}");

        return true;
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