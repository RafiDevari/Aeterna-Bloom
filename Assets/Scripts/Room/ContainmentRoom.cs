using System.Collections.Generic;
using UnityEngine;

public class ContainmentRoom : Room
{
    [Header("Containment Units")]
    [SerializeField]
    private List<ContainmentUnit> containmentUnits = new();

    public IReadOnlyList<ContainmentUnit> ContainmentUnits
        => containmentUnits;

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

    public override string GetHUDInfo()
    {
        return $"Containment Units : {ContainmentUnits.Count}";
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        UnityEditor.Handles.Label(
            transform.position + Vector3.down * 1.2f,
            $"Units : {containmentUnits.Count}");
    }
#endif
}