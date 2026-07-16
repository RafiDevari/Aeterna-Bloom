using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kontrak untuk satu unit pekerjaan yang bisa dijalankan Employee secara berurutan.
/// Implementasi HARUS memanggil tepat salah satu dari onComplete / onFail (sekali saja),
/// baik langsung (synchronous, misal ambil stok) maupun setelah proses async
/// (misal lewat callback MoveTo).
///
/// Task konkret (MoveToTask, TakeStockAndPickupTask, FeedMonsterTask, ResearchMonsterTask,
/// HarvestMonsterTask) dan enum EmployeeState sekarang ada di file masing-masing.
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
///
/// Employee di-split jadi beberapa file partial class biar tidak jadi 1 file raksasa
/// (pola yang sama seperti MonsterBase) :
///   - Employee.cs           : identity, movement, task queue, selection, input, lifecycle Unity (file ini)
///   - Employee.Division.cs  : assign ke DivisionRoom & balik ke divisi (BackToDivision)
///   - Employee.Feeding.cs   : sistem makan (PickUpFood, FeedMonster, GoFeed)
///   - Employee.Research.cs  : sistem research (TryResearch, GoResearch)
///   - Employee.Harvest.cs   : sistem harvest (TryHarvest, GoHarvest)
/// Semua file di atas adalah SATU class yang sama (partial) -- API publiknya sama
/// persis seperti sebelum di-pecah, subclass (mis. Researcher, Botanist) tidak perlu
/// tahu ini di-split.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public partial class Employee : MonoBehaviour
{
    [Header("Employee Info")]
    [SerializeField] private string employeeName = "Employee";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    //==============================
    // State
    //==============================

    private bool isSelected;
    private bool isMoving;

    private Vector3 targetPosition;

    /// <summary>
    /// Sisa waypoint yang masih harus dilewati SEBELUM titik akhir (destination asli MoveTo).
    /// Diisi dari RoomPathfinder di MoveTo() -- tiap waypoint = titik tengah 1 room transit
    /// yang dilewati, supaya employee jalan "room-per-room" mengikuti room yang benar-benar
    /// bersebelahan, bukan garis lurus. onArriveCallback baru dipanggil begitu SEMUA waypoint
    /// (termasuk titik akhir) sudah dilewati.
    /// </summary>
    private readonly Queue<Vector3> movementWaypoints = new();

    // Lokasi saat ini
    private Room currentRoom;

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

        assignedDivision?.UnassignEmployee(this); // Employee.Division.cs
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

    //==============================
    // Selection
    //==============================

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

    /// <summary>
    /// Jalan ke destination, TAPI lewat urutan room yang benar-benar bersebelahan
    /// (RoomPathfinder), bukan garis lurus. Room lockdown otomatis dihindari sepenuhnya
    /// (lihat RoomPathfinder.FindRoomPath).
    ///
    /// Kalau RoomPathfinder TIDAK menemukan jalur valid (mis. terhalang lockdown, atau posisi
    /// sekarang/destination tidak ada di room manapun yang terdaftar), employee TIDAK BERGERAK
    /// SAMA SEKALI -- sengaja tidak ada fallback garis lurus, supaya lockdown beneran memblokir,
    /// bukan cuma dihindari kalau kebetulan jalurnya ketemu. onArrive TIDAK dipanggil dalam
    /// kasus ini (belum sampai, memang tidak bisa jalan).
    ///
    /// CATATAN: karena onArrive tidak pernah terpanggil di kasus ini, task pemanggil (mis.
    /// MoveToTask) perlu punya cara sendiri buat mendeteksi "employee tidak akan pernah sampai"
    /// (mis. re-check validity secara berkala) supaya job tidak nyangkut selamanya -- saya belum
    /// pernah lihat isi MoveToTask.cs, jadi belum bisa pastikan itu sudah ditangani di sana.
    /// </summary>
    public virtual void MoveTo(Vector3 destination, System.Action onArrive = null)
    {
        destination.z = 0f;

        List<Vector3> path = RoomPathfinder.FindWaypointPath(transform.position, destination);

        if (path == null)
        {
            Debug.LogWarning($"[Employee] {employeeName} BATAL bergerak ke {destination} : " +
                            "tidak ada jalur yang valid (lockdown, atau posisi tidak ada di room manapun).");
            return;
        }

        onArriveCallback = onArrive;
        movementWaypoints.Clear();

        foreach (Vector3 point in path)
        {
            movementWaypoints.Enqueue(point);
        }

        StartNextWaypoint();

        OnMoveCommandReceived?.Invoke(destination);

        Debug.Log($"[Employee] {employeeName} bergerak ke {destination} lewat {movementWaypoints.Count} titik.");
        isSelected = false;
    }

    private void StartNextWaypoint()
    {
        if (movementWaypoints.Count == 0)
        {
            isMoving = false;
            return;
        }

        targetPosition = movementWaypoints.Dequeue();
        isMoving = true;
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

            if (movementWaypoints.Count > 0)
            {
                // Baru sampai di 1 waypoint transit, masih ada sisa jalur -- lanjut,
                // JANGAN panggil OnArrived() dulu.
                StartNextWaypoint();
            }
            else
            {
                // Ini titik akhir (destination asli MoveTo) -- beneran sampai.
                isMoving = false;
                OnArrived();
            }
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