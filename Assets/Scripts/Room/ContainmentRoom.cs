using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ContainmentRoom : Room
{
    [Header("Containment Units")]
    [SerializeField]
    private List<ContainmentUnit> containmentUnits = new();

    [Header("Monitoring")]
    [SerializeField] private Employee assignedMonitor = null;

    public IReadOnlyList<ContainmentUnit> ContainmentUnits
        => containmentUnits;

    public Employee AssignedMonitor => assignedMonitor;
    public bool HasMonitor => assignedMonitor != null;

    /// <summary>
    /// True if this room has at least one monster in any containment unit.
    /// </summary>
    public bool HasMonsters => containmentUnits.Any(u => u != null && u.HasMonster);

    protected override void Start()
    {
        base.Start();

        foreach (var unit in containmentUnits)
        {
            if (unit == null) continue;

            unit.SetParentRoom(this);
        }
    }

    public void AddContainmentUnit(ContainmentUnit unit)
    {
        if (unit == null || containmentUnits.Contains(unit))
            return;

        containmentUnits.Add(unit);

        unit.SetParentRoom(this);
    }

    public void RemoveContainmentUnit(ContainmentUnit unit)
    {
        containmentUnits.Remove(unit);
    }

    /// <summary>
    /// Assign a researcher to monitor this room.
    /// </summary>
    public void AssignMonitor(Employee researcher)
    {
        if (researcher == null) return;
        assignedMonitor = researcher;
        Debug.Log($"[{RoomName}] Researcher {researcher.EmployeeName} assigned to monitor this room.");
    }

    /// <summary>
    /// Unassign the current monitor.
    /// </summary>
    public void UnassignMonitor()
    {
        if (assignedMonitor != null)
        {
            Debug.Log($"[{RoomName}] Researcher {assignedMonitor.EmployeeName} unassigned from monitoring.");
            assignedMonitor = null;
        }
    }

    /// <summary>
    /// Calculate the average suitable temperature of all monsters in this room.
    /// Returns current room temperature if no monsters present.
    /// </summary>
    public float CalculateAverageSuitableTemperature()
    {
        var monsters = containmentUnits
            .Where(u => u != null && u.HasMonster)
            .Select(u => u.Monster);

        if (!monsters.Any())
            return Temperature;

        return monsters.Average(m => m.SuitableTemperature);
    }


    public override string GetHUDInfo()
    {
        string monitorInfo = HasMonitor ? $" | Monitor: {assignedMonitor.EmployeeName}" : "";
        return $"Containment Units : {ContainmentUnits.Count}{monitorInfo}";
    }

}