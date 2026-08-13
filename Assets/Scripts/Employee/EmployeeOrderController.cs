using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controller untuk menangani perintah pergerakan interaktif (Order To / Move Command)
/// dari pemain kepada Employee.
///
/// Alur Penggunaan:
/// 1. Player membuka EmployeePopup dan menekan tombol "➤ ORDER TO".
/// 2. EmployeeOrderController mengaktifkan mode order untuk employee tersebut.
/// 3. Player mengklik ruangan tujuan mana saja di game world.
/// 4. Controller otomatis menghitung lantai/walkway terdekat di bagian bawah ruangan
///    (via Room.GetNearestWalkablePosition) lalu menggerakkan employee ke sana.
/// 5. Mode order dapat dibatalkan kapan saja dengan Klik Kanan atau tombol ESC.
/// </summary>
public class EmployeeOrderController : MonoBehaviour
{
    public static EmployeeOrderController Instance { get; private set; }

    private Employee selectedEmployee;
    private bool isOrdering = false;

    public bool IsOrdering => isOrdering;
    public Employee SelectedEmployee => selectedEmployee;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("EmployeeOrderController", typeof(EmployeeOrderController));
            Instance = go.GetComponent<EmployeeOrderController>();
            DontDestroyOnLoad(go);
        }
    }

    /// <summary>
    /// Memulai mode pemilihan tujuan untuk employee yang dipilih.
    /// </summary>
    public void StartOrder(Employee employee)
    {
        if (employee == null || employee.CurrentState == EmployeeState.Dead)
        {
            Debug.LogWarning("[EmployeeOrderController] Tidak bisa memberi order: employee tidak valid atau sudah gugur.");
            return;
        }

        selectedEmployee = employee;
        isOrdering = true;

        Debug.Log($"[EmployeeOrderController] Mode ORDER TO aktif untuk {employee.EmployeeName}. Klik sebuah ruangan untuk memindahkan.");
    }

    /// <summary>
    /// Membatalkan mode order pergerakan.
    /// </summary>
    public void CancelOrder()
    {
        if (isOrdering)
        {
            Debug.Log("[EmployeeOrderController] Mode ORDER TO dibatalkan.");
        }

        isOrdering = false;
        selectedEmployee = null;
    }

    public static event System.Action<Employee, Room> OnOrderExecuted;

    private void Update()
    {
        if (!isOrdering || selectedEmployee == null)
            return;

        // Jika employee tiba-tiba mati / invalid saat mode order aktif
        if (selectedEmployee.CurrentState == EmployeeState.Dead)
        {
            CancelOrder();
            return;
        }

        // 1. Batalkan mode order jika pemain menekan ESC atau Klik Kanan
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelOrder();
            return;
        }

        // 2. Eksekusi perpindahan saat pemain melakukan Klik Kiri pada ruangan tujuan
        if (Input.GetMouseButtonDown(0))
        {
            // Abaikan jika klik berada di atas elemen UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Camera.main == null)
                return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            Room targetRoom = Room.FindRoomUnderPointer(mouseWorldPos);

            if (targetRoom != null)
            {
                // Hitung koordinat lantai/walkway terdekat di bagian bawah ruangan
                Vector3 walkableDestination = targetRoom.GetNearestWalkablePosition(mouseWorldPos);

                // Batalkan task lama dan kirim perintah jalan langsung ke lokasi tujuan
                selectedEmployee.ClearTasksAndInterrupt();
                selectedEmployee.MoveTo(walkableDestination);

                Debug.Log($"[EmployeeOrderController] Menugaskan {selectedEmployee.EmployeeName} pergi ke ruangan '{targetRoom.RoomName}' di posisi lantai {walkableDestination}");

                OnOrderExecuted?.Invoke(selectedEmployee, targetRoom);

                // Selesai memberi order
                isOrdering = false;
                selectedEmployee = null;
            }
            else
            {
                Debug.LogWarning("[EmployeeOrderController] Titik yang diklik berada di luar ruangan fasilitas. Klik pada salah satu ruangan untuk memindahkan employee.");
            }
        }
    }

    private void OnGUI()
    {
        if (!isOrdering || selectedEmployee == null)
            return;

        // Tampilkan banner panduan elegan di bagian atas layar
        float width = 460f;
        float height = 44f;
        float x = (Screen.width - width) * 0.5f;
        float y = 18f;

        GUI.Box(new Rect(x, y, width, height), GUIContent.none);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            richText = true
        };

        string msg = $"<color=#38bdf8>➤ <b>MODE ORDER:</b></color> Klik ruangan tujuan untuk <b>{selectedEmployee.EmployeeName}</b>\n<color=#94a3b8><size=11>[Klik Kanan / ESC untuk Batal]</size></color>";
        GUI.Label(new Rect(x, y, width, height), msg, style);
    }
}
