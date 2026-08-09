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

    [Header("Blackout Settings")]
    [SerializeField] private float maxElectricity = 100f;
    [SerializeField] private float blackoutMoodDecayInterval = 10f;
    [SerializeField] private int blackoutMoodDecayAmount = 1;

    private bool isBlackout = false;
    private float blackoutTimer = 0f;

    public bool IsBlackout => isBlackout;
    public float MaxElectricity => maxElectricity;
    public float MaxEnergy => (maxEnergy == 1000f || maxEnergy <= 0f) ? 100f : maxEnergy;
    [Header("Overload Settings")]
    [SerializeField] private float overloadToleranceDuration = 10f;
    private float overloadTimer = 0f;
    private float debugOverloadTimer = 0f;

    public float OverloadTimer => overloadTimer;
    public float OverloadToleranceDuration => overloadToleranceDuration;

    //────────────────────────────────────────────────────────

    public float Energy
    {
        get => energy;
        set
        {
            energy = Mathf.Clamp(value, 0, maxEnergy);
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
    public float Electricity => isBlackout ? 0f : rooms.Sum(room => room.ElectricityCost);

    public IReadOnlyList<Room> Rooms => rooms;
    public IReadOnlyList<Employee> Employees => employees;

    //────────────────────────────────────────────────────────

    public System.Action<float> OnEnergyChanged;
    public System.Action<float> OnDefaultRoomTemperatureChanged;
    public System.Action<float> OnElectricityChanged;

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
    }

    private void Start()
    {
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
        if (energy > 75f)
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
            }
        }
    }

    private void CheckBlackoutTrigger()
    {
        if (isBlackout) return;

        // Reset overload timers if current usage drops below capacity
        float actualUsage = rooms.Sum(room => room.ElectricityCost);
        if (actualUsage <= maxElectricity)
        {
            overloadTimer = 0f;
            debugOverloadTimer = 0f;
        }
    }

    private void TriggerBlackout()
    {
        isBlackout = true;
        blackoutTimer = 0f;
        OnElectricityChanged?.Invoke(Electricity); // Will trigger with 0f
        Debug.LogWarning("[Facility] MATI LAMPU! Penggunaan listrik melebihi 100%.");
    }

    public void ResolveBlackout()
    {
        isBlackout = false;
        blackoutTimer = 0f;
        OnElectricityChanged?.Invoke(Electricity); // Will trigger with actual usage
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