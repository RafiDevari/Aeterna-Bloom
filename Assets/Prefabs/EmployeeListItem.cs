using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmployeeListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private Employee employee;
    private System.Action<Employee> onClicked;

    public void Setup(Employee employee, System.Action<Employee> onClicked)
    {
        this.employee = employee;
        this.onClicked = onClicked;

        nameText.text = employee.EmployeeName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClicked?.Invoke(this.employee));
    }
}