using UnityEngine;
using UnityEngine.EventSystems;

public partial class Employee
{
    public static event System.Action<Employee> OnAnyEmployeeRightClicked;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonUp(1)) // 1 = right mouse button
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            HandleClick();
        }
    }

    protected virtual void HandleClick()
    {
        Debug.Log($"dwedwedwedwd");

        if (currentState != EmployeeState.Dead && currentState != EmployeeState.Hypnotized)
        {
            SelectThisEmployee();
        }

        OnAnyEmployeeRightClicked?.Invoke(this);
    }
}