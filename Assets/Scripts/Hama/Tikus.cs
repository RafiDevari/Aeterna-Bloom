using UnityEngine;

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

    private void Start()
    {
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

        Employee closest = null;
        float minDistance = detectionRadius;

        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp.CurrentState != EmployeeState.Dead)
            {
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

        // Gerak mengejar target
        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTargetEmployee.transform.position,
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

        if (wanderTimer >= wanderInterval || Vector3.Distance(transform.position, wanderTarget) < 0.1f || !isWandering)
        {
            wanderTimer = 0f;
            isWandering = true;
            // Pilih titik acak di sekitar posisi saat ini
            wanderTarget = transform.position + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-0.3f, 0.3f), 0f);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            wanderTarget,
            moveSpeed * 0.5f * Time.deltaTime);
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
        }, typeof(EmployeeSecurity));
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
