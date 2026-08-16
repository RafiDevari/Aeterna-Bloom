using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hama jenis Tikus.
/// - Bergerak acak (wander) jika tidak ada Employee di dekatnya.
/// - Mendeteksi Employee dalam radius tertentu dan mengejarnya.
/// - Menyerang Employee setiap 3 detik (-2 HP) saat bersentuhan/jarak dekat.
/// - Dapat di-klik kanan oleh player untuk menugaskan Employee (KillPestTask) membunuhnya.
/// - Saat mati, mayatnya TIDAK dihilangkan/Destroy (tetap berada di scene).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Tikus : Pest
{
    [Header("Tikus Mechanics")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private int attackDamage = 2;
    [SerializeField] private float wanderInterval = 3f;

    private float attackTimer = 0f;
    private float wanderTimer = 0f;
    private Vector3 wanderTarget;
    private bool isWandering = false;

    private Employee currentTargetEmployee;

    private List<Vector3> chasePath = null;
    private float repathTimer = 0f;
    private const float repathInterval = 0.5f;

    protected virtual void Start()
    {
        base.Start();
        wanderTarget = transform.position;
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        FindTargetEmployee();

        if (currentTargetEmployee != null)
        {
            HandleChaseAndAttack();
        }
        else
        {
            HandleWander();
        }
    }

    private void FindTargetEmployee()
    {
        if (Facility.Instance == null)
        {
            currentTargetEmployee = null;
            return;
        }

        // Jika target saat ini sudah terhalang/tidak terjangkau, reset
        if (currentTargetEmployee != null)
        {
            Room curTargetRoom = RoomPathfinder.FindRoomAt(currentTargetEmployee.transform.position);
            if (curTargetRoom != null && curTargetRoom.IsLocked)
            {
                currentTargetEmployee = null;
            }
            else
            {
                var curPath = RoomPathfinder.FindWaypointPath(transform.position, currentTargetEmployee.transform.position, false);
                if (curPath == null || curPath.Count == 0)
                {
                    currentTargetEmployee = null;
                }
            }
        }

        Employee closest = null;
        float minDistance = detectionRadius;

        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp.CurrentState != EmployeeState.Dead)
            {
                Room empRoom = RoomPathfinder.FindRoomAt(emp.transform.position);
                if (empRoom != null && empRoom.IsLocked)
                    continue;

                // Cek apakah ada jalur valid ke employee tersebut
                var path = RoomPathfinder.FindWaypointPath(transform.position, emp.transform.position, false);
                if (path == null || path.Count == 0)
                    continue;

                float dist = Vector3.Distance(transform.position, emp.transform.position);
                if (dist <= minDistance)
                {
                    minDistance = dist;
                    closest = emp;
                }
            }
        }

        currentTargetEmployee = closest;
    }

    private void HandleChaseAndAttack()
    {
        isWandering = false;

        Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);
        if (currentRoom != null && currentRoom.IsLocked)
        {
            // Tikus terkunci di dalam ruangan lockdown, fallback ke wander dalam ruangan saja
            HandleWander();
            return;
        }

        repathTimer += Time.deltaTime;
        if (chasePath == null || repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            chasePath = RoomPathfinder.FindWaypointPath(transform.position, currentTargetEmployee.transform.position, false);
        }

        Vector3 nextTargetPos = currentTargetEmployee.transform.position;
        if (chasePath != null && chasePath.Count > 0)
        {
            nextTargetPos = chasePath[0];
            if (Vector3.Distance(transform.position, nextTargetPos) < 0.1f)
            {
                chasePath.RemoveAt(0);
                if (chasePath.Count > 0)
                {
                    nextTargetPos = chasePath[0];
                }
            }
        }
        else
        {
            // Jika target tidak terjangkau (misal di locked room), diam atau fallback ke wander
            HandleWander();
            return;
        }

        // Gerak mengejar target melewati waypoint agar tidak menembus dinding
        transform.position = Vector3.MoveTowards(
            transform.position,
            nextTargetPos,
            moveSpeed * Time.deltaTime);

        float distance = Vector3.Distance(transform.position, currentTargetEmployee.transform.position);

        // Jika bersentuhan / sangat dekat (jarak <= 0.8 unit)
        if (distance <= 0.8f)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                currentTargetEmployee.ModifyHp(-attackDamage);
                Debug.Log($"[Tikus] Menyerang {currentTargetEmployee.EmployeeName}! -{attackDamage} HP. HP tersisa: {currentTargetEmployee.Hp}");
            }
        }
        else
        {
            attackTimer = 0f;
        }
    }

    private void HandleWander()
    {
        attackTimer = 0f;
        wanderTimer += Time.deltaTime;

        Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);

        if (wanderTimer >= wanderInterval || Vector3.Distance(transform.position, wanderTarget) < 0.1f || !isWandering || currentRoom == null)
        {
            wanderTimer = 0f;
            isWandering = true;

            if (currentRoom != null)
            {
                Bounds[] boundsList = currentRoom.CollisionBounds;
                if (boundsList != null && boundsList.Length > 0)
                {
                    Bounds bounds = boundsList[Random.Range(0, boundsList.Length)];
                    // Pilih titik acak mendatar di dalam bounds ruangan saat ini
                    float randomX = Random.Range(bounds.min.x, bounds.max.x);
                    // Pastikan Y tepat di tengah bounds ruangan agar tidak melayang/keluar batas vertikal
                    wanderTarget = new Vector3(randomX, bounds.center.y, 0f);
                }
                else
                {
                    // Fallback
                    wanderTarget = transform.position + new Vector3(Random.Range(-2.5f, 2.5f), 0f, 0f);
                }
            }
            else
            {
                // Jika berada di luar ruangan, cari ruangan terdekat untuk masuk kembali
                Room nearestRoom = FindNearestRoom();
                if (nearestRoom != null)
                {
                    Bounds[] boundsList = nearestRoom.CollisionBounds;
                    if (boundsList != null && boundsList.Length > 0)
                    {
                        Bounds bounds = boundsList[0];
                        wanderTarget = new Vector3(bounds.center.x, bounds.center.y, 0f);
                    }
                }
                else
                {
                    wanderTarget = transform.position;
                }
            }
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            wanderTarget,
            moveSpeed * 0.5f * Time.deltaTime);
    }

    private Room FindNearestRoom()
    {
        if (Facility.Instance == null || Facility.Instance.Rooms.Count == 0)
            return null;

        Room closest = null;
        float minDist = float.MaxValue;
        foreach (var room in Facility.Instance.Rooms)
        {
            if (room == null || room.IsLocked) continue;
            float dist = Vector3.Distance(transform.position, room.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = room;
            }
        }
        return closest;
    }

    private void OnMouseOver()
    {
        if (isDead) return;

        if (Input.GetMouseButtonDown(1)) // Klik Kanan
        {
            HandleRightClick();
        }
    }

    private void HandleRightClick()
    {
        if (isDead) return;

        Debug.Log($"[Tikus] Di-klik kanan! Membuka EmployeeSelectPopup untuk menugaskan pembasmian.");

        Tikus capturedPest = this;

        EmployeeSelectPopup.Instance.Open(selectedEmp =>
        {
            if (selectedEmp != null && capturedPest != null && !capturedPest.IsDead)
            {
                selectedEmp.EnqueueTask(new KillPestTask(capturedPest));
                Debug.Log($"[Tikus] {selectedEmp.EmployeeName} ditugaskan untuk membunuh tikus.");
            }
        }, typeof(DivisionSecurity));
    }

    protected override void Die()
    {
        isDead = true;
        Debug.Log($"[Tikus] Tikus telah mati! Mayat tetap berada di scene.");

        // Nonaktifkan collider & pergerakan
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Ubah warna sprite menjadi abu-abu
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        // CATATAN: Jangan panggil Destroy(gameObject) agar mayat tidak hilang dari scene!
    }

    private static bool hasSpawned = false;

    public static new void Spawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Tikus.prefab");
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
        }
#else
        GameObject prefab = null;
#endif
        if (prefab == null)
        {
            Debug.LogError("[Tikus] Prefab Tikus / Jamur tidak ditemukan!");
            return;
        }

        if (Facility.Instance != null && Facility.Instance.Rooms.Count > 0)
        {
            var rooms = Facility.Instance.Rooms;
            Room targetRoom = rooms[Random.Range(0, rooms.Count)];

            Bounds[] boundsList = targetRoom.CollisionBounds;
            if (boundsList != null && boundsList.Length > 0)
            {
                Bounds bounds = boundsList[Random.Range(0, boundsList.Length)];
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float spawnY = bounds.center.y;
                Vector3 spawnPos = new Vector3(randomX, spawnY, 0f);

                GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
                if (go.GetComponent<Tikus>() == null)
                {
                    Jamur jamur = go.GetComponent<Jamur>();
                    if (jamur != null) Destroy(jamur);
                    go.AddComponent<Tikus>();
                }
                Debug.Log($"[Tikus] Tikus berhasil di-spawn di ruangan {targetRoom.RoomName} pada posisi {spawnPos}");
            }
        }
    }
}
