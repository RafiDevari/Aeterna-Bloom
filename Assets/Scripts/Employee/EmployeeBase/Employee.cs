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
[RequireComponent(typeof(BoxCollider2D))]
public partial class Employee : MonoBehaviour
{
    [Header("Employee Info")]
    [SerializeField] private string employeeName = "Employee";

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] private float hypnotizedMoveSpeed = 1f;

    private System.Action onTimedActionComplete;
    private System.Action onTimedActionFail;
    private bool hasTimedAction;

    [Header("Employee Stats")]
    [SerializeField] protected int hp = 100;
    [SerializeField] protected int maxHp = 100;
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    public System.Action<int> OnHpChanged;
    public System.Action<int> OnMoodChanged;

    [Header("Poison Mechanics")]
    [SerializeField] private float poisonInterval = 1.0f;
    [SerializeField] private int poisonDamageAmount = 5;
    private float poisonTimer = 0f;

    [Header("Virus & Sickness Mechanics")]
    [SerializeField] private bool isSick = false;
    [SerializeField] private float sickHpInterval = 20.0f;
    [SerializeField] private int sickHpDamage = 3;
    [SerializeField] private float coughInterval = 30.0f;
    [SerializeField] private float coughSpreadRadius = 2.5f;

    private float sickHpTimer = 0f;
    private float coughTimer = 0f;
    private float virusImmunityTimer = 0f;
    private float postCureSleepTimer = 0f;

    public bool IsSick => isSick;
    public bool IsImmuneToVirus => division == EmployeeDivision.Medic || this is EmployeeMedic || virusImmunityTimer > 0f;

    public System.Action<bool> OnSickStatusChanged;

    public int Hp
    {
        get => hp;
        protected set
        {
            if (currentState == EmployeeState.Dead)
                return;

            int previous = hp;
            hp = Mathf.Clamp(value, 0, maxHp);
            if (previous != hp)
            {
                OnHpChanged?.Invoke(hp);
                Debug.Log($"[{EmployeeName}] HP : {previous} -> {hp}");
                if (hp == 0)
                {
                    Die();
                }
            }
        }
    }

    public int MaxHp
    {
        get => maxHp;
        protected set => maxHp = value;
    }

    public int Mood
    {
        get => mood;
        protected set
        {
            int previous = mood;
            mood = Mathf.Clamp(value, minMood, maxMood);
            if (previous != mood)
            {
                OnMoodChanged?.Invoke(mood);
                OnMoodChange(previous, mood);
                Debug.Log($"[{EmployeeName}] Mood : {previous} -> {mood}");
            }
        }
    }

    public int MaxMood => maxMood;
    public int MinMood => minMood;

    public string MoodName
    {
        get
        {
            switch (mood)
            {
                case 5: return "Joy";
                case 4: return "Happy";
                case 3: return "Normal";
                case 2: return "Fear";
                case 1: return "Depressed";
                case 0: return "Depressed";
                default: return "Normal";
            }
        }
    }

    protected virtual void OnMoodChange(int oldMood, int newMood) { }

    public void ModifyHp(int delta)
    {
        Hp += delta;
    }

    public void SetHp(int value)
    {
        Hp = value;
    }

    public void ModifyMood(int delta)
    {
        Mood += delta;
    }

    public void SetMood(int value)
    {
        Mood = value;
    }

    [Header("Collider Settings")]
    [Tooltip("Otomatis sesuaikan ukuran & offset BoxCollider2D mengikuti bounds sprite/visuals employee.")]
    [SerializeField] private bool autoFitCollider = true;

    [Tooltip("BoxCollider2D milik employee ini. Auto-cari kalau kosong.")]
    [SerializeField] private BoxCollider2D employeeCollider;

    [Tooltip("Margin/padding tambahan untuk collider (x = lebar extra, y = tinggi extra).")]
    [SerializeField] private Vector2 colliderPadding = Vector2.zero;

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

    private EmployeeAppearance appearance;

    /// <summary>Access the EmployeeAppearance component attached to this Employee.</summary>
    public EmployeeAppearance Appearance
    {
        get
        {
            if (appearance == null) appearance = GetComponent<EmployeeAppearance>();
            return appearance;
        }
    }

    public string EmployeeName
    {
        get => employeeName;
        set => employeeName = value;
    }

    public bool IsSelected => isSelected;

    public Room CurrentRoom => GetCurrentRoom();

    public Room GetCurrentRoom()
    {
        if (currentRoom != null && currentRoom.Contains(transform.position))
            return currentRoom;

        if (Facility.Instance != null)
        {
            foreach (var room in Facility.Instance.Rooms)
            {
                if (room != null && room.Contains(transform.position))
                {
                    currentRoom = room;
                    return room;
                }
            }
        }

        return currentRoom;
    }

    //==============================
    // Unity & Collider Auto-Fit
    //==============================

    private void OnValidate()
    {
        if (autoFitCollider)
        {
            AutoFitCollider();
        }
    }

    private void Start()
    {
        targetPosition = transform.position;

        if (autoFitCollider)
        {
            AutoFitCollider();
        }

        Facility.Instance?.RegisterEmployee(this);
    }

    /// <summary>
    /// Otomatis menyesuaikan ukuran dan offset BoxCollider2D mengikuti gabungan bounds
    /// dari semua SpriteRenderer pada Employee ini (body, head, hair, dll).
    /// </summary>
    [ContextMenu("Auto Fit Collider")]
    public void AutoFitCollider()
    {
        if (!autoFitCollider) return;

        if (employeeCollider == null)
            employeeCollider = GetComponent<BoxCollider2D>();

        if (employeeCollider == null)
            employeeCollider = GetComponentInChildren<BoxCollider2D>();

        if (employeeCollider == null) return;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Transform root = transform.Find("Visuals") ?? transform.Find("Visual") ?? transform;

        bool foundAny = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var r in renderers)
        {
            if (r == null || !r.enabled || r.sprite == null) continue;

            Bounds b = r.sprite.bounds;

            Vector3[] localCorners = new Vector3[4]
            {
                new Vector3(b.min.x, b.min.y, 0f),
                new Vector3(b.min.x, b.max.y, 0f),
                new Vector3(b.max.x, b.min.y, 0f),
                new Vector3(b.max.x, b.max.y, 0f)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 worldPos = r.transform.TransformPoint(localCorners[i]);
                Vector3 empLocal = transform.InverseTransformPoint(worldPos);

                minX = Mathf.Min(minX, empLocal.x);
                maxX = Mathf.Max(maxX, empLocal.x);
                minY = Mathf.Min(minY, empLocal.y);
                maxY = Mathf.Max(maxY, empLocal.y);
                foundAny = true;
            }
        }

        if (!foundAny) return;

        float width = (maxX - minX) + colliderPadding.x;
        float height = (maxY - minY) + colliderPadding.y;
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        if (width > 0f && height > 0f)
        {
            employeeCollider.size = new Vector2(width, height);
            employeeCollider.offset = center;
        }
    }

    /// <summary>
    /// Dipanggil saat karakter membalik arah hadap (facing left/right)
    /// agar BoxCollider2D otomatis menyesuaikan posisinya dengan visual.
    /// </summary>
    public void OnFacingDirectionChanged(bool isDefaultFacing)
    {
        if (autoFitCollider)
        {
            AutoFitCollider();
        }
        else if (employeeCollider != null)
        {
            Vector2 offset = employeeCollider.offset;
            offset.x = isDefaultFacing ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
            employeeCollider.offset = offset;
        }
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

        if (currentState == EmployeeState.Conversing && newState != EmployeeState.Conversing)
        {
            EndConversation();
        }

        currentState = newState;
        OnStateChanged?.Invoke(currentState);
    }

    private void Update()
    {
        if (currentState == EmployeeState.Dead)
            return;

        HandleMovement();
        HandleGlobalInput();
        ProcessTaskQueue();
        UpdateTimedAction();
        UpdateMoodRegen();
        UpdateSocializing();
        HandleRoomHazards();
        HandleSickMechanics();
        UpdateVirusTimers();
    }

    private void UpdateVirusTimers()
    {
        if (virusImmunityTimer > 0f)
        {
            virusImmunityTimer -= Time.deltaTime;
        }

        if (currentState == EmployeeState.Sleeping && postCureSleepTimer > 0f)
        {
            postCureSleepTimer -= Time.deltaTime;
            if (postCureSleepTimer <= 0f)
            {
                SetState(EmployeeState.Idle);
                BackToDivision();
                Debug.Log($"[{EmployeeName}] Selesai tidur pemulihan pasca sembuh dari virus, kembali bekerja.");
            }
        }
    }

    private void HandleRoomHazards()
    {
        poisonTimer += Time.deltaTime;
        if (poisonTimer >= poisonInterval)
        {
            poisonTimer = 0f;
            Room room = RoomPathfinder.FindRoomAt(transform.position);
            if (room != null)
            {
                // Jika sedang melakukan sterilisasi, employee imun terhadap racun maupun sterilisasi
                bool takesPoisonDamage = room.IsPoisoned && currentState != EmployeeState.Sterilizing;
                bool takesSterilizeDamage = room.IsSterilizing && currentState != EmployeeState.Sterilizing;

                if (takesPoisonDamage || takesSterilizeDamage)
                {
                    int damage = poisonDamageAmount;
                    if (takesSterilizeDamage)
                    {
                        // Employee biasa yang berada di ruangan yang sedang disterilisasi hanya terkena 50% damage
                        damage = Mathf.RoundToInt(poisonDamageAmount * 0.5f);
                    }

                    ModifyHp(-damage);
                    string hazardType = takesSterilizeDamage ? "Sterilisasi" : "Racun";
                    // Menampilkan debug opsional agar developer tahu
                    Debug.Log($"[{EmployeeName}] Terkena damage {hazardType} -{damage} HP di ruangan {room.RoomName}. HP tersisa: {hp}");
                }
            }
        }
    }

    public void InfectVirus()
    {
        if (isSick || IsImmuneToVirus || currentState == EmployeeState.Dead)
            return;

        isSick = true;
        sickHpTimer = 0f;
        coughTimer = 0f;
        OnSickStatusChanged?.Invoke(true);
        Debug.LogWarning($"[{EmployeeName}] TERINFEKSI VIRUS! Memasuki status SICK (-3 HP / 20s, -30% Speed, batuk / 30s).");
    }

    public void CureVirus()
    {
        if (!isSick) return;

        isSick = false;
        sickHpTimer = 0f;
        coughTimer = 0f;
        OnSickStatusChanged?.Invoke(false);
        Debug.Log($"[{EmployeeName}] Sembuh dari Virus!");

        // Efek tambahan pasca disembuhkan:
        virusImmunityTimer = 180f; // Imun terhadap virus selama 180 detik
        postCureSleepTimer = 60f;  // Tidur selama 60 detik
        SetState(EmployeeState.Sleeping);
        ClearTasksAndInterrupt();
        Debug.Log($"[{EmployeeName}] Masuk status Sleeping selama 60 detik pasca sembuh dari virus.");
    }

    private void HandleSickMechanics()
    {
        if (!isSick || currentState == EmployeeState.Dead) return;

        // Damage HP per 20 detik
        sickHpTimer += Time.deltaTime;
        if (sickHpTimer >= sickHpInterval)
        {
            sickHpTimer = 0f;
            ModifyHp(-sickHpDamage);
            Debug.Log($"[{EmployeeName}] Kehilangan {sickHpDamage} HP akibat virus! Sisa HP: {hp}");
        }

        // Batuk per 30 detik
        coughTimer += Time.deltaTime;
        if (coughTimer >= coughInterval)
        {
            coughTimer = 0f;
            Cough();
        }
    }

    private void Cough()
    {
        Debug.LogWarning($"[{EmployeeName}] *UHUK UHUK* (Batuk akibat virus!)");

        if (Facility.Instance == null) return;

        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp != this && emp.CurrentState != EmployeeState.Dead && !emp.IsSick && !emp.IsImmuneToVirus)
            {
                float distance = Vector3.Distance(transform.position, emp.transform.position);
                if (distance <= coughSpreadRadius)
                {
                    emp.InfectVirus();
                    Debug.LogWarning($"[{emp.EmployeeName}] Tertular virus dari batuk {EmployeeName} (Jarak: {distance:F2} unit)!");
                }
            }
        }
    }

    //==============================
    // Task Queue Handling
    //==============================

    public void EnqueueTask(EmployeeTask task)
    {
        if (task == null || currentState == EmployeeState.Hypnotized || currentState == EmployeeState.Dead || currentState == EmployeeState.Sleeping)
            return;

        EndConversation();
        taskQueue.Enqueue(task);
    }

    /// <summary>
    /// Membatalkan task yang sedang berjalan beserta semua task yang masih mengantre.
    /// Panggil ini secara eksplisit sebelum memberi perintah baru yang menggantikan
    /// job lama (misal klik manual, atau assign job baru).
    /// </summary>
    public void ClearTasksAndInterrupt()
    {
        EndConversation();

        if (currentTask != null)
        {
            currentTask.Cancel();
            currentTask = null;
        }

        taskQueue.Clear();
        movementWaypoints.Clear();
        isMoving = false;

        if (hasTimedAction)
        {
            hasTimedAction = false;
            var fail = onTimedActionFail;
            onTimedActionComplete = null;
            onTimedActionFail = null;
            fail?.Invoke();
        }
    }

    private void ProcessTaskQueue()
    {
        if (currentTask != null || currentState == EmployeeState.Hypnotized || currentState == EmployeeState.Dead || currentState == EmployeeState.Sleeping)
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
        BackToDivision();
    }

    //==============================
    // Selection
    //==============================

    private void SelectThisEmployee()
    {
        if (currentState == EmployeeState.Dead || currentState == EmployeeState.Hypnotized)
            return;

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
        // Manual movement is disabled for now.
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
        EndConversation();

        destination.z = 0f;

        bool canEnterLockedRooms = (this is EmployeeSecurity);
        List<Vector3> path = RoomPathfinder.FindWaypointPath(transform.position, destination, canEnterLockedRooms);

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

        if (isMoving && currentState != EmployeeState.Hypnotized)
        {
            SetState(EmployeeState.Moving);
        }

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

        float currentSpeed = (currentState == EmployeeState.Hypnotized) ? hypnotizedMoveSpeed : moveSpeed;
        if (isSick)
        {
            currentSpeed *= 0.7f;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentSpeed * Time.deltaTime);

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

        if (currentState == EmployeeState.Moving)
        {
            SetState(EmployeeState.Idle);
        }

        SetSocialCooldown(postMoveSocialCooldown);

        // Fire once, then clear, so it doesn't leak into the next MoveTo call
        var callback = onArriveCallback;
        onArriveCallback = null;
        callback?.Invoke();
    }

    //==============================
    // Hypnotized & Death Implementation
    //==============================

    public enum HypnotizedInput
    {
        AttackFriend,
        EnterPlantContainment
    }

    public void Hypnotize(HypnotizedInput input, object target = null, System.Action<Employee> onArriveAction = null)
    {
        if (currentState == EmployeeState.Dead)
            return;

        ClearTasksAndInterrupt();
        SetState(EmployeeState.Hypnotized);

        Debug.Log($"[Employee] {EmployeeName} has been HYPNOTIZED! Input action: {input}");

        if (input == HypnotizedInput.AttackFriend)
        {
            Employee friend = FindRandomFriend();
            if (friend != null)
            {
                Debug.Log($"[Employee] {EmployeeName} is attacking friend {friend.EmployeeName}!");
                MoveTo(friend.transform.position, () => {
                    if (friend != null && friend.CurrentState != EmployeeState.Dead)
                    {
                        friend.ModifyHp(-50);
                        Debug.Log($"[Employee] {EmployeeName} attacked {friend.EmployeeName}! HP is now {friend.Hp}.");
                    }
                    if (onArriveAction != null)
                    {
                        onArriveAction.Invoke(this);
                    }
                    else
                    {
                        SetState(EmployeeState.Idle);
                    }
                });
            }
            else
            {
                Debug.Log($"[Employee] No friends found to attack.");
                SetState(EmployeeState.Idle);
            }
        }
        else if (input == HypnotizedInput.EnterPlantContainment)
        {
            ContainmentUnit containmentUnit = target as ContainmentUnit;
            if (containmentUnit == null)
            {
                containmentUnit = FindRandomContainmentUnit();
            }

            if (containmentUnit != null)
            {
                Debug.Log($"[Employee] {EmployeeName} is walking into plant containment at {containmentUnit.gameObject.name}!");
                MoveTo(containmentUnit.transform.position, () => {
                    Debug.Log($"[Employee] {EmployeeName} arrived at plant containment.");
                    if (onArriveAction != null)
                    {
                        onArriveAction.Invoke(this);
                    }
                    else if (containmentUnit.Monster != null)
                    {
                        containmentUnit.Monster.OnHypnotizedEmployeeArrived(this);
                    }
                    else
                    {
                        SetState(EmployeeState.Idle);
                    }
                });
            }
            else
            {
                Debug.Log($"[Employee] No containment unit found.");
                SetState(EmployeeState.Idle);
            }
        }
    }

    public void Die()
    {
        if (currentState == EmployeeState.Dead)
            return;

        hp = 0; // Bypass property setter directly to avoid recursion
        OnHpChanged?.Invoke(hp);

        SetState(EmployeeState.Dead);
        ClearTasksAndInterrupt();
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        Debug.LogWarning($"[Employee] {EmployeeName} HAS DIED.");

        // Notifikasi ke semua rekan satu divisi
        if (Facility.Instance != null && assignedDivision != null)
        {
            foreach (var emp in Facility.Instance.Employees)
            {
                if (emp != null && emp != this)
                {
                    emp.NotifyColleagueDeath(this);
                }
            }
        }
    }

    private Employee FindRandomFriend()
    {
        if (Facility.Instance == null) return null;
        var list = new List<Employee>();
        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp != this && emp.CurrentState != EmployeeState.Dead)
            {
                list.Add(emp);
            }
        }
        if (list.Count > 0)
        {
            return list[Random.Range(0, list.Count)];
        }
        return null;
    }

    private ContainmentUnit FindRandomContainmentUnit()
    {
        if (Facility.Instance == null) return null;
        foreach (var room in Facility.Instance.Rooms)
        {
            if (room is ContainmentRoom containmentRoom)
            {
                foreach (var unit in containmentRoom.ContainmentUnits)
                {
                    if (unit != null) return unit;
                }
            }
        }
        return null;
    }

    public void StartTimedAction(float duration, System.Action onComplete, System.Action onFail)
    {
        SetActionDuration(duration);
        onTimedActionComplete = onComplete;
        onTimedActionFail = onFail;
        hasTimedAction = true;
        actionStartTime = Time.time;
    }

    private void UpdateTimedAction()
    {
        if (!hasTimedAction) return;

        if (Time.time >= actionStartTime + actionDuration)
        {
            hasTimedAction = false;
            var complete = onTimedActionComplete;
            onTimedActionComplete = null;
            onTimedActionFail = null;
            complete?.Invoke();
        }
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