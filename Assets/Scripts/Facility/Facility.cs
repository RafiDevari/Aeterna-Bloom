using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DefaultExecutionOrder(-100)]
public class Facility : MonoBehaviour
{
    public static Facility Instance { get; private set; }

    [Header("Global Resources")]
    [SerializeField] private float energy = 100f;

    [Tooltip("Suhu awal yang diberikan ke setiap Room saat dibuat.")]
    [SerializeField] private float defaultRoomTemperature = 20f;

    [SerializeField] private float electricity = 100f;

    [Header("Rooms")]
    [SerializeField]
    private List<Room> rooms = new();

    [Header("Employees")]
    [SerializeField]
    private List<Employee> employees = new();

    //────────────────────────────────────────────────────────


    public float Energy
    {
        get => energy;
        set
        {
            energy = Mathf.Max(0, value);
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

    public float Electricity
    {
        get => electricity;
        set
        {
            electricity = Mathf.Max(0, value);
            OnElectricityChanged?.Invoke(electricity);
        }
    }

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

    //────────────────────────────────────────────────────────
    // Room
    //────────────────────────────────────────────────────────

    public void AddRoom(Room room)
    {
        if (room == null || rooms.Contains(room))
            return;

        rooms.Add(room);

        room.InitFromFacility(DefaultRoomTemperature);

        OnRoomAdded?.Invoke(room);

        Debug.Log($"[Facility] Room ditambahkan : {room.RoomName}");
    }

    public void RemoveRoom(Room room)
    {
        rooms.Remove(room);
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
}