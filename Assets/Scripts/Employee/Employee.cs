using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Kontrak untuk satu unit pekerjaan yang bisa dijalankan Employee secara berurutan.
/// Implementasi HARUS memanggil tepat salah satu dari onComplete / onFail (sekali saja),
/// baik langsung (synchronous, misal ambil stok) maupun setelah proses async
/// (misal lewat callback MoveTo).
///
/// Task konkret (MoveToTask, TakeStockAndPickupTask, FeedMonsterTask) dan
/// enum EmployeeState sekarang ada di file masing-masing.
/// </summary>

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

    /// <summary>Employee yang sedang dipilih player sekarang (null kalau tidak ada). Dipakai popup/UI lain (mis. tombol Research) yang butuh tahu employee mana yang akan diberi perintah.</summary>
    public static Employee CurrentlySelected => currentlySelected;

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
        return target.FeedDuration ;
    }

    //==============================
    // Research
    //==============================

    /// <summary>
    /// Coba jalankan satu aksi research pada monster target.
    /// - researchId null/kosong -> coba research APA SAJA yang available sekarang (TryResearchNext).
    /// - researchId diisi        -> coba entry spesifik itu (TryResearch(id)).
    /// Return false kalau target null, atau tidak ada research yang syaratnya terpenuhi sekarang.
    ///
    /// Dipisah dari MonsterBase (sama seperti CalculateFeedDuration) supaya nanti kalau mau
    /// ada faktor "siapa yang research" (skill employee, kecepatan, dsb), tinggal ditambah di
    /// sini lewat override tanpa menyentuh MonsterBase.
    /// </summary>
    public virtual bool TryResearch(MonsterBase target, string researchId = null)
    {
        if (target == null)
            return false;

        bool success = string.IsNullOrEmpty(researchId)
            ? target.TryResearchNext()
            : target.TryResearch(researchId);

        if (success)
            Debug.Log($"[Employee] {employeeName} berhasil melakukan research pada {target.MonsterName}.");
        else
            Debug.Log($"[Employee] {employeeName} gagal research pada {target.MonsterName} " +
                      $"(syarat belum terpenuhi / sudah selesai / tidak ada yang available sekarang).");

        return success;
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

        Debug.Log($"[Employee] {employeeName} menerima job: research {capturedMonster?.MonsterName}.");
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