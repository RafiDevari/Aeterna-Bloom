using System.Collections.Generic;
using UnityEngine;

public class DivisionRoom : Room
{
    [Header("Assigned Employees")]
    [SerializeField]
    private List<Employee> employees = new();

    public IReadOnlyList<Employee> Employees => employees;

    public void AssignEmployee(Employee employee)
    {
        if (employee == null)
            return;

        if (employees.Contains(employee))
            return;

        employees.Add(employee);

        Debug.Log($"[Division] {employee.EmployeeName} ditugaskan ke {RoomName}");
    }

    public void UnassignEmployee(Employee employee)
    {
        if (employee == null)
            return;

        employees.Remove(employee);

        Debug.Log($"[Division] {employee.EmployeeName} keluar dari {RoomName}");
    }
}