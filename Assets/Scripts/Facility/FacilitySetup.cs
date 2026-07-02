// using UnityEngine;

// /// <summary>
// /// FacilitySetup: Hanya bertanggung jawab spawn Room dan ContainmentUnit.
// /// Monster di-assign langsung di Inspector tiap prefab ContainmentUnit
// /// via field "Monster Prefab" — bukan urusan FacilitySetup.
// /// </summary>
// public class FacilitySetup : MonoBehaviour
// {
//     [Header("Prefabs")]
//     [SerializeField] private GameObject roomPrefab;
//     [SerializeField] private GameObject containmentUnitPrefab;
//     [SerializeField] private GameObject employeePrefab;

//     [Header("Layout")]
//     [SerializeField] private int   roomCount    = 2;
//     [SerializeField] private int   unitsPerRoom = 2;
//     [SerializeField] private float roomSpacing  = 6f;
//     [SerializeField] private float unitSpacing  = 2f;
//     [SerializeField] private int   employeeCount = 3;

//     private void Start()
//     {
//         if (Facility.Instance == null)
//         {
//             Debug.LogError("[FacilitySetup] Facility.Instance null!");
//             return;
//         }
//     }

//     private void SetupRooms()
//     {
//         for (int r = 0; r < roomCount; r++)
//         {
//             float  xPos = (r - (roomCount - 1) / 2f) * roomSpacing;
//             string name = $"Room {(char)('A' + r)}";
//             var    roomGo = SpawnRoom(name, new Vector3(xPos, 0, 0));
//             var    room   = roomGo.GetComponent<Room>();

//             for (int u = 0; u < unitsPerRoom; u++)
//             {
//                 float  ux    = (u - (unitsPerRoom - 1) / 2f) * unitSpacing;
//                 string uName = $"Unit {(char)('A' + r)}-{u + 1}";
//                 var    unitGo = SpawnContainmentUnit(roomGo, uName, new Vector3(ux, 0, 0));
//                 room.AddContainmentUnit(unitGo.GetComponent<ContainmentUnit>());
//                 // Monster di-handle oleh ContainmentUnit itu sendiri via Inspector prefab-nya
//             }

//             Facility.Instance.AddRoom(room);
//         }

//         Debug.Log($"[FacilitySetup] {roomCount} Room siap. " +
//                   "Monster di-assign lewat Inspector tiap ContainmentUnit prefab.");
//     }

//     private void SpawnEmployees()
//     {
//         if (employeePrefab == null) return;
//         for (int i = 0; i < employeeCount; i++)
//         {
//             var pos = new Vector3(Random.Range(-5f, 5f), Random.Range(-2f, 2f), 0);
//             var go  = Instantiate(employeePrefab, pos, Quaternion.identity);
//             var emp = go.GetComponent<Employee>();
//             if (emp != null) emp.EmployeeName = $"Employee_{i + 1}";
//         }
//     }

//     // ── Helpers ───────────────────────────────────────────────────────────────
//     private GameObject SpawnRoom(string roomName, Vector3 worldPos)
//     {
//         GameObject go;
//         if (roomPrefab != null)
//         {
//             go = Instantiate(roomPrefab, worldPos, Quaternion.identity);
//         }
//         else
//         {
//             go = new GameObject(roomName);
//             go.transform.position = worldPos;
//             go.AddComponent<Room>();
//             var col = go.AddComponent<BoxCollider2D>();
//             col.size = new Vector2(5f, 3f);
//             col.isTrigger = true;
//         }
//         go.name = roomName;
//         var room = go.GetComponent<Room>();
//         if (room != null) room.RoomName = roomName;
//         return go;
//     }

//     private GameObject SpawnContainmentUnit(GameObject parent, string unitName, Vector3 localPos)
//     {
//         GameObject go;
//         if (containmentUnitPrefab != null)
//         {
//             go = Instantiate(containmentUnitPrefab, parent.transform);
//         }
//         else
//         {
//             go = new GameObject(unitName);
//             go.transform.SetParent(parent.transform);
//             go.AddComponent<ContainmentUnit>();
//             var col = go.AddComponent<BoxCollider2D>();
//             col.size = new Vector2(1.4f, 1.4f);
//         }
//         go.transform.localPosition = localPos;
//         go.name = unitName;
//         var unit = go.GetComponent<ContainmentUnit>();
//         if (unit != null) unit.UnitName = unitName;
//         return go;
//     }

//     public Room AddNewRoom(string newRoomName, Vector3 position)
//     {
//         var go   = SpawnRoom(newRoomName, position);
//         var room = go.GetComponent<Room>();
//         Facility.Instance.AddRoom(room);
//         return room;
//     }
// }
