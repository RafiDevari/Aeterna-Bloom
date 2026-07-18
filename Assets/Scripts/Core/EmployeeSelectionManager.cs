using UnityEngine;

/// <summary>
/// EmployeeSelectionManager: MonoBehaviour singleton yang menangani
/// input global untuk sistem pemilihan employee.
///
/// Flow:
/// 1. Right Click pada Employee → Employee tersebut menjadi "selected"
/// 2. Left Click di mana saja → Employee yang selected bergerak ke sana
///
/// Pasang script ini ke sebuah GameObject di scene (misal: "GameManager").
/// </summary>
public class EmployeeSelectionManager : MonoBehaviour
{
    public static EmployeeSelectionManager Instance { get; private set; }

    private Employee selectedEmployee = null;

    [Header("Visual Feedback (opsional)")]
    [SerializeField] private GameObject selectionIndicatorPrefab;
    private GameObject selectionIndicatorInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // Manual movement is disabled for now.
    }

    public void SelectEmployee(Employee emp)
    {
        if (selectedEmployee != null && selectedEmployee != emp)
            selectedEmployee.Deselect();

        selectedEmployee = emp;
        emp.OnSelectionChanged += OnEmployeeSelectionChanged;
        Debug.Log($"[SelectionManager] Employee dipilih: {emp.EmployeeName}");
    }

    private void OnEmployeeSelectionChanged(Employee emp, bool selected)
    {
        if (!selected && emp == selectedEmployee)
        {
            selectedEmployee = null;
            emp.OnSelectionChanged -= OnEmployeeSelectionChanged;
        }
    }

    private bool IsPointerOverEmployee()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(mousePos);
        return hit != null && hit.GetComponent<Employee>() != null;
    }

    private void ShowMoveTarget(Vector3 pos)
    {
        if (selectionIndicatorPrefab == null) return;

        if (selectionIndicatorInstance == null)
            selectionIndicatorInstance = Instantiate(selectionIndicatorPrefab);

        selectionIndicatorInstance.transform.position = pos;
        // Fade/destroy setelah beberapa detik (opsional)
        Destroy(selectionIndicatorInstance, 1f);
        selectionIndicatorInstance = null;
    }

// #if UNITY_EDITOR
//     private void OnGUI()
//     {
//         if (selectedEmployee != null)
//         {
//             GUI.Label(new Rect(10, 80, 300, 20),
//                 $"Selected: {selectedEmployee.EmployeeName} (Left Click to move)");
//         }
//         else
//         {
//             GUI.Label(new Rect(10, 80, 300, 20), "Right Click an Employee to select");
//         }
//     }
// #endif
}
