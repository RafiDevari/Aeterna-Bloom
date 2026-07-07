using UnityEngine;

public class MonsterContext
{
    private readonly ContainmentUnit unit;

    public MonsterContext(ContainmentUnit unit)
    {
        this.unit = unit;
    }
    
    // ======================================================
    // Room
    // ======================================================

    public Room CurrentRoom => unit.ParentRoom;

    public void ChangeRoomTemperature(float delta)
    {
        if (CurrentRoom == null)
            return;

        CurrentRoom.Temperature += delta;
    }

    public void ChangeFacilityTemperature(float delta)
    {
        if (Facility == null)
            return;

        Facility.DefaultRoomTemperature += delta;
    }

    // ======================================================
    // Facility
    // ======================================================

    public Facility Facility => Facility.Instance;

    public void ChangeEnergy(float delta)
    {
        if (Facility == null)
            return;

        Facility.Energy += delta;
    }

    public void ChangeElectricity(float delta)
    {
        if (Facility == null)
            return;

        Facility.Electricity += delta;
    }

    // ======================================================
    // Employee
    // ======================================================

    public Employee GetRandomEmployee()
    {
        return Facility?.GetRandomEmployee();
    }

    public void MoveEmployee(Employee employee, Vector3 position)
    {
        if (employee == null)
            return;

        employee.MoveTo(position);
    }

    public void SummonRandomEmployee()
    {
        Employee emp = GetRandomEmployee();

        if (emp == null)
            return;

        MoveEmployee(emp, unit.transform.position);
    }

    

    // ======================================================

    public Vector3 UnitPosition => unit.transform.position;

    public ContainmentUnit Unit => unit;
}