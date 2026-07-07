using UnityEngine;

/// <summary>
/// Employee dapat dipilih dengan Right Click lalu diperintahkan bergerak.
/// Employee memiliki:
/// - CurrentRoom      -> lokasi fisik saat ini.
/// - AssignedDivision -> divisi tempat ia bekerja.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Employee : MonoBehaviour
{
    [Header("Employee Info")]
    [SerializeField] private string employeeName = "Employee";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Feeding")]
    [SerializeField] private FoodType carriedFood;
    [SerializeField] private bool hasFood = false;

    //==============================
    // State
    //==============================

    private bool isSelected;
    private bool isMoving;

    private Vector3 targetPosition;

    // Lokasi saat ini
    private Room currentRoom;

    // Divisi tempat bekerja
    private DivisionRoom assignedDivision;

    // Hanya satu employee boleh dipilih
    private static Employee currentlySelected;

    //==============================
    // Events
    //==============================

    public System.Action<Employee, bool> OnSelectionChanged;
    public System.Action<Vector3> OnMoveCommandReceived;

    //==============================
    // Properties
    //==============================

    public string EmployeeName
    {
        get => employeeName;
        set => employeeName = value;
    }

    public bool IsSelected => isSelected;

    public Room CurrentRoom => currentRoom;

    public DivisionRoom AssignedDivision => assignedDivision;

    public FoodType CarriedFood => carriedFood;

    public bool HasFood => hasFood;

    //==============================
    // Unity
    //==============================

    private void Start()
    {
        targetPosition = transform.position;

        Facility.Instance?.RegisterEmployee(this);
    }

    private void OnDestroy()
    {
        Facility.Instance?.UnregisterEmployee(this);

        assignedDivision?.UnassignEmployee(this);
    }

    private void Update()
    {
        HandleMovement();
        HandleGlobalInput();
    }

    //==============================
    // Division
    //==============================

    public void AssignDivision(DivisionRoom division)
    {
        if (assignedDivision == division)
            return;

        assignedDivision?.UnassignEmployee(this);

        assignedDivision = division;

        assignedDivision?.AssignEmployee(this);

        Debug.Log($"[Employee] {employeeName} ditugaskan ke division : {assignedDivision?.RoomName}");
    }

    //==============================
    // Feeding
    //==============================

    /// <summary>
    /// Employee mengambil satu jenis makanan untuk dibawa.
    /// Menimpa makanan sebelumnya kalau belum sempat dipakai.
    /// </summary>
    public void PickUpFood(FoodType food)
    {
        carriedFood = food;
        hasFood = true;

        Debug.Log($"[Employee] {employeeName} mengambil makanan : {food}");
    }

    /// <summary>
    /// Beri makan monster target dengan makanan yang sedang dibawa.
    /// Return false kalau employee tidak sedang membawa makanan.
    /// </summary>
    public virtual bool FeedMonster(MonsterBase target)
    {
        if (target == null)
            return false;

        if (!hasFood)
        {
            Debug.Log($"[Employee] {employeeName} tidak membawa makanan untuk diberikan.");
            return false;
        }

        target.Feed(carriedFood);

        Debug.Log($"[Employee] {employeeName} memberi makan {target.MonsterName} dengan {carriedFood}.");

        hasFood = false;

        return true;
    }

    //==============================
    // Selection
    //==============================

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SelectThisEmployee();
        }
    }

    private void SelectThisEmployee()
    {
        if (currentlySelected != null &&
            currentlySelected != this)
        {
            currentlySelected.Deselect();
        }

        isSelected = true;
        currentlySelected = this;

        OnSelectionChanged?.Invoke(this, true);

        Debug.Log($"[Employee] {employeeName} dipilih.");
    }

    public void Deselect()
    {
        isSelected = false;

        if (currentlySelected == this)
            currentlySelected = null;

        OnSelectionChanged?.Invoke(this, false);
    }

    //==============================
    // Input
    //==============================

    private void HandleGlobalInput()
    {
        if (!isSelected)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            worldPos.z = 0f;

            MoveTo(worldPos);
        }
    }

    //==============================
    // Movement
    //==============================

    public virtual void MoveTo(Vector3 destination)
    {
        destination.z = 0f;

        targetPosition = destination;
        isMoving = true;

        OnMoveCommandReceived?.Invoke(destination);

        Debug.Log($"[Employee] {employeeName} bergerak ke {destination}");
        isSelected = false;
    }

    protected virtual void HandleMovement()
    {
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;

            isMoving = false;

            OnArrived();
        }
    }

    protected virtual void OnArrived()
    {
        Debug.Log($"[Employee] {employeeName} tiba di tujuan.");
    }

    //==============================
    // Room Tracking
    //==============================

    private void OnTriggerEnter2D(Collider2D other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        if (room == currentRoom)
            return;

        currentRoom = room;

        Debug.Log($"[Employee] {employeeName} masuk ke {room.RoomName}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Room room = other.GetComponent<Room>();

        if (room == null)
            return;

        if (room != currentRoom)
            return;

        currentRoom = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isSelected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }

        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }
#endif
}