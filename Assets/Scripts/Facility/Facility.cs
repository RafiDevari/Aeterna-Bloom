// Facility.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DefaultExecutionOrder(-100)]
public class Facility : MonoBehaviour
{
    public static Facility Instance { get; private set; }

    [Header("Global Resources")]
    [SerializeField] private float energy = 100f;
    [SerializeField] private float maxEnergy = 100f;

    [Tooltip("Suhu awal yang diberikan ke setiap Room saat dibuat.")]
    [SerializeField] private float defaultRoomTemperature = 20f;

    [Header("Rooms")]
    [SerializeField]
    private List<Room> rooms = new();

    [Header("Employees")]
    [SerializeField]
    private List<Employee> employees = new();

    [Header("Death Records")]
    public List<EmployeeDeathRecord> DeadEmployeesReport = new List<EmployeeDeathRecord>();

    public void RecordEmployeeDeath(Employee emp, string cause)
    {
        if (emp == null) return;
        
        var existing = DeadEmployeesReport.FirstOrDefault(r => r.EmployeeName == emp.EmployeeName);
        if (existing != null)
        {
            existing.CauseOfDeath = cause;
        }
        else
        {
            DeadEmployeesReport.Add(new EmployeeDeathRecord 
            { 
                EmployeeName = emp.EmployeeName, 
                CauseOfDeath = cause 
            });
        }
    }

    [Header("Blackout Settings")]
    [SerializeField] private float maxElectricity = 100f;
    [SerializeField] private float blackoutMoodDecayInterval = 10f;
    [SerializeField] private int blackoutMoodDecayAmount = 1;

    private bool isBlackout = false;
    private float blackoutTimer = 0f;

    public bool IsBlackout => isBlackout;
    public float MaxElectricity
    {
        get
        {
            RecalculateMaxElectricity();
            return maxElectricity;
        }
    }
    public float MaxEnergy
    {
        get
        {
            RecalculateMaxEnergy();
            return maxEnergy;
        }
    }

    public void RecalculateMaxElectricity()
    {
        int level = (GameSaveSystem.Instance != null) ? GameSaveSystem.Instance.ElectricityLevel : 1;
        maxElectricity = 100f + ((level - 1) * 50f);
    }

    public void RecalculateMaxEnergy()
    {
        int day = (GameSaveSystem.Instance != null) ? GameSaveSystem.Instance.Day : 1;
        maxEnergy = 100f * Mathf.Pow(1.1f, Mathf.Max(0, day - 1));
    }
    [Header("Overload Settings")]
    [SerializeField] private float overloadToleranceDuration = 10f;
    private float overloadTimer = 0f;
    private float debugOverloadTimer = 0f;
    private bool hasBroadcastedCountdown = false;

    public float OverloadTimer => overloadTimer;
    public float OverloadToleranceDuration => overloadToleranceDuration;

    //────────────────────────────────────────────────────────

    public float Energy
    {
        get => energy;
        set
        {
            energy = Mathf.Clamp(value, 0, MaxEnergy);
            OnEnergyChanged?.Invoke(energy);
        }
    }

    public float DefaultRoomTemperature
    {
        get => defaultRoomTemperature;
        set
        {
            defaultRoomTemperature = value;
            OnDefaultRoomTemperatureChanged?.Invoke(defaultRoomTemperature);
        }
    }

    /// <summary>
    /// Total pemakaian listrik saat ini : jumlah ElectricityCost dari semua Room.
    /// Murni hasil hitungan, bukan nilai yang di-set manual - selalu mencerminkan
    /// kondisi room-room saat ini (base cost + monster + selisih suhu).
    /// </summary>
    public float Electricity => isBlackout ? 0f : CalculateTotalElectricityUsage();

    public float CalculateTotalElectricityUsage()
    {
        float total = 0f;
        foreach (var r in rooms)
        {
            if (r != null) total += r.ElectricityCost;
        }
        return total;
    }

    public IReadOnlyList<Room> Rooms => rooms;
    public IReadOnlyList<Employee> Employees => employees;

    //────────────────────────────────────────────────────────

    public System.Action<float> OnEnergyChanged;
    public System.Action<float> OnDefaultRoomTemperatureChanged;
    public System.Action<float> OnElectricityChanged;
    public System.Action<bool> OnBlackoutChanged;
    public static event System.Action<bool> OnBlackoutStateChanged;

    public System.Action<Room> OnRoomAdded;
    public System.Action<Employee> OnEmployeeRegistered;
    public System.Action<Employee> OnEmployeeUnregistered;

    //────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RecalculateMaxEnergy();
        RecalculateMaxElectricity();
    }

    private void OnEnable()
    {
        GameSaveSystem.OnDayChanged += HandleDayChanged;
        GameSaveSystem.OnElectricityLevelChanged += HandleElectricityLevelChanged;
        RecalculateMaxEnergy();
        RecalculateMaxElectricity();
    }

    private void OnDisable()
    {
        GameSaveSystem.OnDayChanged -= HandleDayChanged;
        GameSaveSystem.OnElectricityLevelChanged -= HandleElectricityLevelChanged;
    }

    private void HandleDayChanged(int newDay)
    {
        RecalculateMaxEnergy();
        energy = Mathf.Clamp(energy, 0, maxEnergy);
        OnEnergyChanged?.Invoke(energy);
    }

    private void HandleElectricityLevelChanged(int newLevel)
    {
        RecalculateMaxElectricity();
        OnElectricityChanged?.Invoke(Electricity);
        CheckBlackoutTrigger();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RecalculateMaxEnergy();
        RecalculateMaxElectricity();
    }
#endif

    private void Start()
    {
        RecalculateMaxEnergy();
        RecalculateMaxElectricity();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                room.InitFromFacility(DefaultRoomTemperature);
                room.OnElectricityCostChanged -= HandleRoomElectricityCostChanged;
                room.OnElectricityCostChanged += HandleRoomElectricityCostChanged;
            }
        }
        OnElectricityChanged?.Invoke(Electricity);
    }

    //────────────────────────────────────────────────────────
    // Room
    //────────────────────────────────────────────────────────

    public void AddRoom(Room room)
    {
        if (room == null || rooms.Contains(room))
            return;

        rooms.Add(room);

        room.InitFromFacility(DefaultRoomTemperature);
        room.OnElectricityCostChanged += HandleRoomElectricityCostChanged;

        OnRoomAdded?.Invoke(room);
        OnElectricityChanged?.Invoke(Electricity);
        CheckBlackoutTrigger();

        Debug.Log($"[Facility] Room ditambahkan : {room.RoomName}");
    }

    public void RemoveRoom(Room room)
    {
        if (room == null)
            return;

        if (rooms.Remove(room))
        {
            room.OnElectricityCostChanged -= HandleRoomElectricityCostChanged;
            OnElectricityChanged?.Invoke(Electricity);
            CheckBlackoutTrigger();
        }
    }

    private void HandleRoomElectricityCostChanged(float _)
    {
        OnElectricityChanged?.Invoke(Electricity);
        CheckBlackoutTrigger();
    }

    //────────────────────────────────────────────────────────
    // Employee
    //────────────────────────────────────────────────────────

    public void RegisterEmployee(Employee employee)
    {
        if (employee == null || employees.Contains(employee))
            return;

        employees.Add(employee);

        OnEmployeeRegistered?.Invoke(employee);
    }

    public void UnregisterEmployee(Employee employee)
    {
        if (employee == null)
            return;

        if (employees.Remove(employee))
        {
            OnEmployeeUnregistered?.Invoke(employee);
        }
    }

    /// <summary>
    /// Menghitung jumlah employee yang masih hidup di facility saat ini.
    /// </summary>
    public int LivingEmployeesCount
    {
        get
        {
            var list = (employees != null && employees.Count > 0)
                ? employees.Where(e => e != null).ToList()
                : FindObjectsByType<Employee>(FindObjectsSortMode.None).ToList();

            return list.Count(e => e.CurrentState != EmployeeState.Dead && e.Hp > 0);
        }
    }

    /// <summary>
    /// Mengecek apakah semua employee telah gugur (Lose Condition).
    /// </summary>
    public bool IsAllEmployeesDead
    {
        get
        {
            var list = (employees != null && employees.Count > 0)
                ? employees.Where(e => e != null).ToList()
                : FindObjectsByType<Employee>(FindObjectsSortMode.None).ToList();

            if (list.Count == 0) return false;
            return list.All(e => e.CurrentState == EmployeeState.Dead || e.Hp <= 0);
        }
    }

    public StockRoom FindNearestStockRoom(Vector3 fromPosition)
    {
        return Rooms
            .OfType<StockRoom>()
            .Where(room => room != null && room.Stock > 0)
            .OrderBy(room => Vector3.Distance(fromPosition, room.transform.position))
            .FirstOrDefault();
    }

    public Employee GetRandomEmployee()
    {
        if (employees.Count == 0)
            return null;

        return employees[Random.Range(0, employees.Count)];
    }

    //────────────────────────────────────────────────────────
    // Blackout Logic & Update
    //────────────────────────────────────────────────────────

    private void Update()
    {
        // SEMENTARA: Spawn tikus jika energy > 75%
        if (energy > MaxEnergy * 0.75f)
        {
            Pest.Spawn();
        }

        if (isBlackout)
        {
            blackoutTimer += Time.deltaTime;
            if (blackoutTimer >= blackoutMoodDecayInterval)
            {
                blackoutTimer = 0f;
                ApplyBlackoutMoodPenalty();
            }
        }
        else
        {
            blackoutTimer = 0f;

            // Overload tolerance check
            float actualUsage = rooms.Sum(room => room.ElectricityCost);
            if (actualUsage > maxElectricity)
            {
                if (!hasBroadcastedCountdown)
                {
                    hasBroadcastedCountdown = true;
                    FacilityHUD.ShowBroadcast($"Penggunaan listrik berlebih! Pemadaman listrik dalam {overloadToleranceDuration:F0} detik.", "System");
                }

                overloadTimer += Time.deltaTime;
                debugOverloadTimer += Time.deltaTime;

                if (debugOverloadTimer >= 1.0f)
                {
                    debugOverloadTimer = 0f;
                    Debug.LogWarning($"[Facility] Kebutuhan listrik berlebih ({actualUsage:F1} / {maxElectricity:F1}). Blackout dalam {Mathf.Max(0f, overloadToleranceDuration - overloadTimer):F1} detik.");
                }

                if (overloadTimer >= overloadToleranceDuration)
                {
                    TriggerBlackout();
                }
            }
            else
            {
                overloadTimer = 0f;
                debugOverloadTimer = 0f;
                hasBroadcastedCountdown = false;
            }
        }
    }

    private void CheckBlackoutTrigger()
    {
        if (isBlackout) return;

        // Reset overload timers if current usage drops below capacity
        float actualUsage = CalculateTotalElectricityUsage();
        if (actualUsage <= maxElectricity)
        {
            overloadTimer = 0f;
            debugOverloadTimer = 0f;
            hasBroadcastedCountdown = false;
        }
    }

    [ContextMenu("Trigger Blackout (Mati Lampu)")]
    public void TriggerBlackout()
    {
        isBlackout = true;
        blackoutTimer = 0f;
        hasBroadcastedCountdown = false;

        foreach (var room in rooms.OfType<ElectricityRoom>())
        {
            if (room != null)
            {
                room.TriggerShock();
            }
        }

        OnElectricityChanged?.Invoke(Electricity); // Will trigger with 0f
        OnBlackoutChanged?.Invoke(true);
        OnBlackoutStateChanged?.Invoke(true);
        Debug.LogWarning("[Facility] MATI LAMPU! Penggunaan listrik melebihi 100%.");
        FacilityHUD.ShowBroadcast("MATI LAMPU! Penggunaan listrik melebihi 100%.", "System");
    }

    [ContextMenu("Resolve Blackout (Perbaiki Listrik)")]
    public void ResolveBlackout()
    {
        isBlackout = false;
        blackoutTimer = 0f;

        foreach (var room in rooms.OfType<ElectricityRoom>())
        {
            if (room != null)
            {
                room.ResetPower();
            }
        }

        OnElectricityChanged?.Invoke(Electricity); // Will trigger with actual usage
        OnBlackoutChanged?.Invoke(false);
        OnBlackoutStateChanged?.Invoke(false);
        Debug.Log("[Facility] Listrik telah diperbaiki.");
    }

    private void ApplyBlackoutMoodPenalty()
    {
        Debug.Log("[Facility] Mati lampu! Mood tanaman/monster/employee berkurang.");
        foreach (var room in rooms)
        {
            if (room is ContainmentRoom containmentRoom)
            {
                foreach (var unit in containmentRoom.ContainmentUnits)
                {
                    if (unit != null && unit.HasMonster)
                    {
                        unit.Monster.ModifyMood(-blackoutMoodDecayAmount);
                    }
                }
            }
        }

        foreach (var employee in employees)
        {
            if (employee != null)
            {
                employee.ModifyMood(-blackoutMoodDecayAmount);
            }
        }
    }
}

[System.Serializable]
public class EmployeeDeathRecord
{
    public string EmployeeName;
    public string CauseOfDeath;
}