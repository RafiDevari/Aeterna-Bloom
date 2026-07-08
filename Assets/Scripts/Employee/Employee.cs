using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Kontrak untuk satu unit pekerjaan yang bisa dijalankan Employee secara berurutan.
/// Implementasi HARUS memanggil tepat salah satu dari onComplete / onFail (sekali saja),
/// baik langsung (synchronous, misal ambil stok) maupun setelah proses async
/// (misal lewat callback MoveTo).
/// </summary>

//==============================================================
// Task: berjalan ke sebuah posisi.
// Posisi & validitas dievaluasi LAZY (saat task benar-benar mulai),
// supaya kalau target sudah tidak valid (misal room dihancurkan),
// task gagal dengan bersih alih-alih exception / posisi salah.
//==============================================================
public enum EmployeeState
{
    Idle,
    Moving,
    Feeding
}
public class MoveToTask : EmployeeTask
{
    private readonly System.Func<Vector3> getDestination;
    private readonly System.Func<bool> isValid;

    public MoveToTask(System.Func<Vector3> getDestination, System.Func<bool> isValid = null)
    {
        this.getDestination = getDestination;
        this.isValid = isValid;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (isValid != null && !isValid())
        {
            onFail?.Invoke();
            return;
        }

        employee.MoveTo(getDestination(), onComplete);
    }

    public void Cancel()
    {
        // Tidak ada resource untuk dibersihkan; employee yang berhenti
        // ditangani lewat Employee.ClearTasksAndInterrupt().
    }
}

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

//==============================================================
// Task: beri makan monster target, dengan validasi ulang
// (monster/unit bisa berubah selama employee dalam perjalanan).
//==============================================================
public class FeedMonsterTask : EmployeeTask
{
    private readonly ContainmentUnit unit;
    private readonly MonsterBase targetMonster;

    private System.Action onComplete;
    private bool isWaitingForFeedToFinish;

    public FeedMonsterTask(ContainmentUnit unit, MonsterBase targetMonster)
    {
        this.unit = unit;
        this.targetMonster = targetMonster;
    }

    public void Start(Employee employee, System.Action onComplete, System.Action onFail)
    {
        if (unit == null || !unit.HasMonster || unit.Monster != targetMonster)
        {
            onFail?.Invoke();
            return;
        }

        if (!employee.FeedMonster(targetMonster))
        {
            onFail?.Invoke();
            return;
        }

        this.onComplete = onComplete;
        isWaitingForFeedToFinish = true;

        employee.SetState(EmployeeState.Feeding);
        targetMonster.OnFeedFinished += HandleFeedFinished;
    }

    private void HandleFeedFinished()
    {
        if (!isWaitingForFeedToFinish)
            return;

        isWaitingForFeedToFinish = false;
        targetMonster.OnFeedFinished -= HandleFeedFinished;
        onComplete?.Invoke();
    }

    public void Cancel()
    {
        if (!isWaitingForFeedToFinish)
            return;

        isWaitingForFeedToFinish = false;
        targetMonster.OnFeedFinished -= HandleFeedFinished;
    }
}

/// <summary>
/// Employee dapat dipilih dengan Right Click lalu diperintahkan bergerak.
/// Employee memiliki:
/// - CurrentRoom      -> lokasi fisik saat ini.
/// - AssignedDivision -> divisi tempat ia bekerja.
///
/// Pekerjaan multi-tahap (mis. ambil stok lalu beri makan) dijalankan lewat
/// task queue (EmployeeTask), bukan lewat nested callback, supaya:
/// - job tidak diam-diam ketimpa kalau ada perintah lain masuk,
/// - job bisa di-cancel secara eksplisit,
/// - mudah menambah jenis pekerjaan baru tanpa mengubah Employee.
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
    // Task Queue
    //==============================

    private readonly Queue<EmployeeTask> taskQueue = new();
    private EmployeeTask currentTask;

    /// <summary>True kalau employee sedang menjalankan atau masih punya task tertunda.</summary>
    public bool IsBusy => currentTask != null || taskQueue.Count > 0;
    private EmployeeState currentState = EmployeeState.Idle;
    public EmployeeState CurrentState => currentState;
    public System.Action<EmployeeState> OnStateChanged;

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
    public void SetState(EmployeeState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        OnStateChanged?.Invoke(currentState);
    }

    private void Update()
    {
        HandleMovement();
        HandleGlobalInput();
        ProcessTaskQueue();
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

        if (!target.Feed(carriedFood))
            return false;

        Debug.Log($"[Employee] {employeeName} memberi makan {target.MonsterName} dengan {carriedFood}.");

        hasFood = false;

        return true;
    }

    //==============================
    // High-level Commands
    //==============================

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

        // Job baru menggantikan job lama (kalau ada) secara eksplisit.
        ClearTasksAndInterrupt();

        EnqueueTask(new MoveToTask(
            () => stockRoom.transform.position,
            () => stockRoom != null));

        EnqueueTask(new TakeStockAndPickupTask(stockRoom, food, amount));

        EnqueueTask(new MoveToTask(
            () => capturedMonster.transform.position,
            () => unit != null && unit.HasMonster && unit.Monster == capturedMonster));

        EnqueueTask(new FeedMonsterTask(unit, capturedMonster));

        EnqueueTask(new MoveToTask(
            () => stockRoom.transform.position,
            () => stockRoom != null));

        Debug.Log($"[Employee] {employeeName} menerima job: ambil stok lalu beri makan {capturedMonster?.MonsterName}.");
    }

    //==============================
    // Task Queue Handling
    //==============================

    public void EnqueueTask(EmployeeTask task)
    {
        if (task == null)
            return;

        taskQueue.Enqueue(task);
    }

    /// <summary>
    /// Membatalkan task yang sedang berjalan beserta semua task yang masih mengantre.
    /// Panggil ini secara eksplisit sebelum memberi perintah baru yang menggantikan
    /// job lama (misal klik manual, atau assign job baru).
    /// </summary>
    public void ClearTasksAndInterrupt()
    {
        if (currentTask != null)
        {
            currentTask.Cancel();
            currentTask = null;
        }

        taskQueue.Clear();
    }

    private void ProcessTaskQueue()
    {
        if (currentTask != null)
            return;

        if (taskQueue.Count == 0)
            return;

        currentTask = taskQueue.Dequeue();
        currentTask.Start(this, OnTaskComplete, OnTaskFail);
    }

    private void OnTaskComplete()
    {
        currentTask = null;
    }

    private void OnTaskFail()
    {
        Debug.Log($"[Employee] {employeeName} job dibatalkan: salah satu task gagal, sisa antrean dibersihkan.");
        currentTask = null;
        taskQueue.Clear();
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

            // Klik manual = perintah baru yang membatalkan job otomatis yang sedang berjalan.
            ClearTasksAndInterrupt();

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