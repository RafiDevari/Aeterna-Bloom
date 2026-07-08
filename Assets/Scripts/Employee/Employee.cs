using UnityEngine;
using System.Linq;

/// <summary>
/// Employee dapat dipilih dengan Right Click lalu diperintahkan bergerak.
/// Employee memiliki:
/// - CurrentRoom      -> lokasi fisik saat ini.
/// - AssignedDivision -> divisi tempat ia bekerja.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Employee : MonoBehaviour
{
    [Header("Employee Info")]
    [SerializeField] private string employeeName = "Employee";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Feeding")]
    [SerializeField] private FoodType carriedFood;
    [SerializeField] private bool hasFood = false;

    //==============================
    // State
    //==============================

    private bool isSelected;
    private bool isMoving;

    private Vector3 targetPosition;

    // Lokasi saat ini
    private Room currentRoom;

    // Divisi tempat bekerja
    private DivisionRoom assignedDivision;

    // Hanya satu employee boleh dipilih
    private static Employee currentlySelected;

    //==============================
    // Events
    //==============================

    public System.Action<Employee, bool> OnSelectionChanged;
    public System.Action<Vector3> OnMoveCommandReceived;
    private System.Action onArriveCallback;

    //==============================
    // Properties
    //==============================

    public string EmployeeName
    {
        get => employeeName;
        set => employeeName = value;
    }

    public bool IsSelected => isSelected;

    public Room CurrentRoom => currentRoom;

    public DivisionRoom AssignedDivision => assignedDivision;

    public FoodType CarriedFood => carriedFood;

    public bool HasFood => hasFood;

    //==============================
    // Unity
    //==============================

    private void Start()
    {
        targetPosition = transform.position;

        Facility.Instance?.RegisterEmployee(this);
    }

    private void OnDestroy()
    {
        Facility.Instance?.UnregisterEmployee(this);

        assignedDivision?.UnassignEmployee(this);
    }

    private void Update()
    {
        HandleMovement();
        HandleGlobalInput();
    }

    //==============================
    // Division
    //==============================

    public void AssignDivision(DivisionRoom division)
    {
        if (assignedDivision == division)
            return;

        assignedDivision?.UnassignEmployee(this);

        assignedDivision = division;

        assignedDivision?.AssignEmployee(this);

        Debug.Log($"[Employee] {employeeName} ditugaskan ke division : {assignedDivision?.RoomName}");
    }

    //==============================
    // Feeding
    //==============================

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

        target.Feed(carriedFood);

        Debug.Log($"[Employee] {employeeName} memberi makan {target.MonsterName} dengan {carriedFood}.");

        hasFood = false;

        return true;
    }

    //==============================
// High-level Commands
//==============================

/// <summary>
/// Perintah lengkap: jalan ke monster, lalu beri makan begitu sampai.
/// TODO: saat ini employee harus sudah punya hasFood=true sebelumnya
/// (belum ada mekanisme pickup food otomatis di sini).
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

        // TAHAP 1: jalan ke stock room
        MoveTo(stockRoom.transform.position, () =>
        {
            if (stockRoom == null)
            {
                Debug.Log($"[Employee] {employeeName} tiba tapi stock room sudah tidak ada.");
                return;
            }

            if (!stockRoom.TakeStock(amount))
            {
                Debug.Log($"[Employee] {employeeName} gagal ambil stok, stok habis di {stockRoom.RoomName}.");
                return;
            }

            PickUpFood(food);

            if (unit == null || !unit.HasMonster || unit.Monster != capturedMonster)
            {
                Debug.Log($"[Employee] {employeeName} sudah ambil stok tapi target monster sudah tidak valid.");
                return;
            }

            // TAHAP 2: jalan ke monster
            MoveTo(capturedMonster.transform.position, () =>
            {
                if (unit != null && unit.HasMonster && unit.Monster == capturedMonster)
                {
                    FeedMonster(capturedMonster);
                }
                else
                {
                    Debug.Log($"[Employee] {employeeName} tiba di monster tapi target sudah tidak valid.");
                }
            });

            Debug.Log($"[Employee] {employeeName} ambil stok, lanjut jalan ke {capturedMonster.MonsterName}.");
        });

        Debug.Log($"[Employee] {employeeName} berjalan menuju stock room {stockRoom.RoomName}.");
    }

    //==============================
    // Selection
    //==============================

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SelectThisEmployee();
        }
    }

    private void SelectThisEmployee()
    {
        if (currentlySelected != null &&
            currentlySelected != this)
        {
            currentlySelected.Deselect();
        }

        isSelected = true;
        currentlySelected = this;

        OnSelectionChanged?.Invoke(this, true);

        Debug.Log($"[Employee] {employeeName} dipilih.");
    }

    public void Deselect()
    {
        isSelected = false;

        if (currentlySelected == this)
            currentlySelected = null;

        OnSelectionChanged?.Invoke(this, false);
    }

    //==============================
    // Input
    //==============================

    private void HandleGlobalInput()
    {
        if (!isSelected)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            worldPos.z = 0f;

            MoveTo(worldPos);
        }
    }

    //==============================
    // Movement
    //==============================

    public virtual void MoveTo(Vector3 destination, System.Action onArrive = null)
    {
        destination.z = 0f;

        targetPosition = destination;
        isMoving = true;
        onArriveCallback = onArrive; // <-- store what to do when we arrive

        OnMoveCommandReceived?.Invoke(destination);

        Debug.Log($"[Employee] {employeeName} bergerak ke {destination}");
        isSelected = false;
    }

    protected virtual void HandleMovement()
    {
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;

            isMoving = false;

            OnArrived();
        }
    }

    protected virtual void OnArrived()
    {
        Debug.Log($"[Employee] {employeeName} tiba di tujuan.");

        // Fire once, then clear, so it doesn't leak into the next MoveTo call
        var callback = onArriveCallback;
        onArriveCallback = null;
        callback?.Invoke();
    }

    //==============================
    // Room Tracking
    //==============================

    private void OnTriggerEnter2D(Collider2D other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        if (room == currentRoom)
            return;

        currentRoom = room;

        Debug.Log($"[Employee] {employeeName} masuk ke {room.RoomName}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        if (room != currentRoom)
            return;

        currentRoom = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isSelected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }

        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }
#endif
}