using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class Lebah : Pest
{
    [Header("Lebah Mechanics")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float drainInterval = 5f;
    [SerializeField] private float growthDrainAmount = 0.03f; // 3%
    [SerializeField] private float wanderInterval = 3f;

    private float drainTimer = 0f;
    private float wanderTimer = 0f;
    private Vector3 wanderTarget;
    private bool isWandering = false;

    [SerializeField] private ContainmentUnit currentTargetUnit;
    [SerializeField] private MonsterBase currentTargetMonster;

    public ContainmentUnit TargetUnit => currentTargetUnit;
    public MonsterBase TargetMonster => currentTargetMonster;

    private Queue<Vector3> movementWaypoints = new Queue<Vector3>();
    private Vector3 currentWaypoint;
    private bool isMoving = false;
    private bool hasArrivedAtTarget = false;

    protected virtual void Start()
    {
        base.Start();
        // Pastikan ada Rigidbody2D agar interaksi trigger/klik bekerja
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        wanderTarget = transform.position;
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        FindTargetMonster();

        if (currentTargetMonster != null && currentTargetUnit != null)
        {
            if (!isMoving && !hasArrivedAtTarget)
            {
                StartPathToTarget();
            }

            if (isMoving)
            {
                HandleMovement();
            }
            
            if (hasArrivedAtTarget)
            {
                HandleGrowthDrain();
            }
        }
        else
        {
            HandleWander();
        }
    }

    public void SetTarget(ContainmentUnit unit, MonsterBase monster)
    {
        currentTargetUnit = unit;
        currentTargetMonster = monster;
        hasArrivedAtTarget = false;
        isMoving = false;
        movementWaypoints.Clear();
    }

    private void FindTargetMonster()
    {
        // Jika sudah ada target valid yang memiliki monster aktif, tidak perlu cari lagi
        if (currentTargetMonster != null && currentTargetUnit != null && currentTargetUnit.HasMonster && currentTargetMonster.Mood > 0)
            return;

        if (Facility.Instance == null)
        {
            currentTargetUnit = null;
            currentTargetMonster = null;
            hasArrivedAtTarget = false;
            isMoving = false;
            movementWaypoints.Clear();
            return;
        }

        ContainmentUnit closest = null;
        float minDistance = float.MaxValue; // Selalu cari yang terdekat di seluruh facility

        foreach (var room in Facility.Instance.Rooms)
        {
            if (room is ContainmentRoom cr)
            {
                foreach (var unit in cr.ContainmentUnits)
                {
                    if (unit != null && unit.HasMonster && unit.Monster.Mood > 0)
                    {
                        float dist = Vector3.Distance(transform.position, unit.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closest = unit;
                        }
                    }
                }
            }
        }

        if (closest != null)
        {
            currentTargetUnit = closest;
            currentTargetMonster = closest.Monster;
            hasArrivedAtTarget = false;
            isMoving = false;
            movementWaypoints.Clear();
        }
        else
        {
            currentTargetUnit = null;
            currentTargetMonster = null;
            hasArrivedAtTarget = false;
            isMoving = false;
            movementWaypoints.Clear();
        }
    }

    private void StartPathToTarget()
    {
        if (currentTargetUnit == null) return;

        Room targetRoom = RoomPathfinder.FindRoomAt(currentTargetUnit.transform.position);
        Vector3 dest = currentTargetUnit.transform.position;
        if (targetRoom != null && targetRoom.CollisionBounds.Length > 0)
        {
            dest.y = targetRoom.CollisionBounds[0].center.y;
        }
        dest.z = 0f;

        List<Vector3> path = RoomPathfinder.FindWaypointPath(transform.position, dest, false);
        movementWaypoints.Clear();

        if (path != null && path.Count > 0)
        {
            foreach (var point in path)
            {
                movementWaypoints.Enqueue(point);
            }
            isMoving = true;
            StartNextWaypoint();
        }
        else
        {
            // Fallback garis lurus jika pathfinding gagal, tapi tetap kunci Y
            dest.y = transform.position.y;
            movementWaypoints.Enqueue(dest);
            isMoving = true;
            StartNextWaypoint();
        }
    }

    private void StartNextWaypoint()
    {
        if (movementWaypoints.Count == 0)
        {
            isMoving = false;
            return;
        }
        currentWaypoint = movementWaypoints.Dequeue();
    }

    private void HandleMovement()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentWaypoint,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentWaypoint) < 0.1f)
        {
            transform.position = currentWaypoint;

            if (movementWaypoints.Count > 0)
            {
                StartNextWaypoint();
            }
            else
            {
                isMoving = false;
                hasArrivedAtTarget = true;
                Debug.Log($"[Lebah] Sampai di target ContainmentUnit {currentTargetUnit.UnitName}!");
            }
        }
    }

    private void HandleGrowthDrain()
    {
        if (currentTargetMonster == null) return;

        // Kematian otomatis jika mood tanaman 0
        if (currentTargetMonster.Mood <= 0)
        {
            Debug.LogWarning($"[Lebah] Mood tanaman {currentTargetMonster.MonsterName} sudah 0! Lebah mati secara otomatis.");
            Die();
            return;
        }

        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer = 0f;

            // Batas minimal growth berdasarkan state
            float floor = currentTargetMonster.HasGrown ? currentTargetMonster.GrowThreshold : 0f;

            if (currentTargetMonster.Growth <= floor + 0.0001f)
            {
                // Growth sudah di batas bawah -> Mood berkurang 1
                currentTargetMonster.ModifyMood(-1);
                Debug.LogWarning($"[Lebah] Growth {currentTargetMonster.MonsterName} berada di batas minimal ({floor * 100:F0}%). Mood berkurang 1! Mood tersisa: {currentTargetMonster.Mood}");

                if (currentTargetMonster.Mood <= 0)
                {
                    Debug.LogWarning($"[Lebah] Mood tanaman {currentTargetMonster.MonsterName} mencapai 0! Lebah mati sendiri.");
                    Die();
                }
            }
            else
            {
                // Kurangi growth 3%
                currentTargetMonster.ModifyGrowth(-growthDrainAmount);
                Debug.Log($"[Lebah] Menghisap growth {currentTargetMonster.MonsterName}! Growth berkurang 3% (Sisa Growth: {currentTargetMonster.GrowthPercent:F1}%).");
            }
        }
    }

    private void HandleWander()
    {
        drainTimer = 0f;
        wanderTimer += Time.deltaTime;
        isWandering = true;

        Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);

        if (wanderTimer >= wanderInterval || Vector3.Distance(transform.position, wanderTarget) < 0.1f || currentRoom == null)
        {
            wanderTimer = 0f;

            if (currentRoom != null)
            {
                Bounds[] boundsList = currentRoom.CollisionBounds;
                if (boundsList != null && boundsList.Length > 0)
                {
                    Bounds bounds = boundsList[Random.Range(0, boundsList.Length)];
                    float randomX = Random.Range(bounds.min.x, bounds.max.x);
                    wanderTarget = new Vector3(randomX, bounds.center.y, 0f);
                }
                else
                {
                    wanderTarget = transform.position + new Vector3(Random.Range(-2.5f, 2.5f), 0f, 0f);
                }
            }
            else
            {
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

        // Kunci koordinat Y target wander ke Y lantai
        if (currentRoom != null && currentRoom.CollisionBounds.Length > 0)
        {
            wanderTarget.y = currentRoom.CollisionBounds[0].center.y;
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
            if (room == null) continue;
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

        Debug.Log($"[Lebah] Di-klik kanan! Membuka EmployeeSelectPopup untuk menugaskan pembasmian.");

        Lebah capturedPest = this;

        EmployeeSelectPopup.Instance.Open(selectedEmp =>
        {
            if (selectedEmp != null && capturedPest != null && !capturedPest.IsDead)
            {
                selectedEmp.EnqueueTask(new KillPestTask(capturedPest));
                Debug.Log($"[Lebah] {selectedEmp.EmployeeName} ditugaskan untuk membunuh lebah.");
            }
        }, typeof(DivisionSecurity));
    }

    private static bool hasSpawned = false;

    public static new void Spawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Lebah.prefab");
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
        }
#else
        GameObject prefab = null;
#endif
        if (prefab == null)
        {
            Debug.LogError("[Lebah] Prefab Lebah / Jamur tidak ditemukan!");
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
                Lebah lebah = go.GetComponent<Lebah>();
                if (lebah == null)
                {
                    Jamur jamur = go.GetComponent<Jamur>();
                    if (jamur != null) Destroy(jamur);
                    lebah = go.AddComponent<Lebah>();
                }
                Debug.Log($"[Lebah] Lebah berhasil di-spawn di ruangan {targetRoom.RoomName} pada posisi {spawnPos}");
            }
        }
    }
}
