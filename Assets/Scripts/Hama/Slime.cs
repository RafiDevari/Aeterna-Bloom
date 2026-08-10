using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hama jenis Slime.
/// MEKANIK:
/// 1. Slime Kecil:
///    - Mencari Employee yang sudah mati (EmployeeState.Dead).
///    - Berjalan menuju mayat tersebut dan memakannya (menghapus objek mayat & unregister dari facility).
///    - Setelah memakan mayat, berubah menjadi Slime Besar (isBigSlime = true, ukuran membesar, HP bertambah).
///    - FALLBACK: Jika tidak ada mayat di fasilitas, Slime Kecil akan mengejar Employee hidup terdekat dan menyerang.
///    - Jika Slime Kecil mati: Memunculkan (Spawn) Virus di tempat dia mati.
///
/// 2. Slime Besar:
///    - Mengejar Employee hidup terdekat dan menyerang mereka.
///    - Damage: 40 HP, Attack Cooldown: 5 detik.
///    - Jika Slime Besar mati: Membelah (split) menjadi 2 Slime Kecil, dan 2 Slime Kecil tersebut akan mulai mencari mayat.
///
/// 3. Interaction:
///    - Klik Kanan pada Slime membuka EmployeeSelectPopup untuk menugaskan Security (KillPestTask).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Slime : Pest
{
    [Header("Slime State")]
    [SerializeField] private bool isBigSlime = false;
    [SerializeField] private Vector3 smallScale = new Vector3(0.6f, 0.6f, 1f);
    [SerializeField] private Vector3 bigScale = new Vector3(1.2f, 1.2f, 1f);

    [Header("Movement & Target")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float eatDistance = 0.8f;
    [SerializeField] private float attackDistance = 0.8f;

    [Header("Attack Stats (Small Slime)")]
    [SerializeField] private int smallAttackDamage = 5;
    [SerializeField] private float smallAttackInterval = 3f;

    [Header("Attack Stats (Big Slime)")]
    [SerializeField] private int bigAttackDamage = 40;
    [SerializeField] private float bigAttackInterval = 5f;

    private float attackTimer = 0f;
    private Employee targetCorpse;
    private Employee targetAliveEmployee;

    private List<Vector3> chasePath = null;
    private float repathTimer = 0f;
    private const float repathInterval = 0.5f;

    public bool IsBigSlime => isBigSlime;

    protected override void Start()
    {
        base.Start();
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (isBigSlime)
        {
            transform.localScale = bigScale;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && !isDead)
            {
                sr.color = new Color(0.2f, 0.9f, 0.2f, 1f); // Lime green for big slime
            }
        }
        else
        {
            transform.localScale = smallScale;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && !isDead)
            {
                sr.color = new Color(0.4f, 0.8f, 0.4f, 1f); // Soft green for small slime
            }
        }
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        if (isBigSlime)
        {
            HandleBigSlimeBehavior();
        }
        else
        {
            HandleSmallSlimeBehavior();
        }
    }

    private void HandleSmallSlimeBehavior()
    {
        // Cari target mayat (Employee yang sudah mati)
        FindTargetCorpse();

        if (targetCorpse != null)
        {
            // Menuju ke mayat
            MoveTowardsTarget(targetCorpse.transform.position);

            float distance = Vector3.Distance(transform.position, targetCorpse.transform.position);
            if (distance <= eatDistance)
            {
                EatCorpse(targetCorpse);
            }
        }
        else
        {
            // FALLBACK: Jika tidak ada mayat, cari & menyerang employee terdekat yang hidup
            FindTargetAliveEmployee();
            if (targetAliveEmployee != null)
            {
                MoveTowardsTarget(targetAliveEmployee.transform.position);

                float distance = Vector3.Distance(transform.position, targetAliveEmployee.transform.position);
                if (distance <= attackDistance)
                {
                    attackTimer += Time.deltaTime;
                    if (attackTimer >= smallAttackInterval)
                    {
                        attackTimer = 0f;
                        targetAliveEmployee.ModifyHp(-smallAttackDamage);
                        Debug.Log($"[Slime Kecil] Menyerang {targetAliveEmployee.EmployeeName}! -{smallAttackDamage} HP.");
                    }
                }
                else
                {
                    attackTimer = 0f;
                }
            }
        }
    }

    private void HandleBigSlimeBehavior()
    {
        // Slime besar mengejar employee hidup terdekat
        FindTargetAliveEmployee();

        if (targetAliveEmployee != null)
        {
            MoveTowardsTarget(targetAliveEmployee.transform.position);

            float distance = Vector3.Distance(transform.position, targetAliveEmployee.transform.position);
            if (distance <= attackDistance)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= bigAttackInterval) // 5 detik
                {
                    attackTimer = 0f;
                    targetAliveEmployee.ModifyHp(-bigAttackDamage); // 40 damage
                    Debug.Log($"[Slime Besar] Menyerang {targetAliveEmployee.EmployeeName}! -{bigAttackDamage} HP. HP tersisa: {targetAliveEmployee.Hp}");
                }
            }
            else
            {
                attackTimer = 0f;
            }
        }
    }

    private void FindTargetCorpse()
    {
        if (Facility.Instance == null)
        {
            targetCorpse = null;
            return;
        }

        Employee closest = null;
        float minDistance = float.MaxValue;

        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp.CurrentState == EmployeeState.Dead)
            {
                float dist = Vector3.Distance(transform.position, emp.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = emp;
                }
            }
        }

        targetCorpse = closest;
    }

    private void FindTargetAliveEmployee()
    {
        if (Facility.Instance == null)
        {
            targetAliveEmployee = null;
            return;
        }

        Employee closest = null;
        float minDistance = float.MaxValue;

        foreach (var emp in Facility.Instance.Employees)
        {
            if (emp != null && emp.CurrentState != EmployeeState.Dead)
            {
                float dist = Vector3.Distance(transform.position, emp.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = emp;
                }
            }
        }

        targetAliveEmployee = closest;
    }

    private void MoveTowardsTarget(Vector3 targetPosition)
    {
        repathTimer += Time.deltaTime;
        if (chasePath == null || repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            chasePath = RoomPathfinder.FindWaypointPath(transform.position, targetPosition, false);
        }

        Vector3 nextTargetPos = targetPosition;
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

        transform.position = Vector3.MoveTowards(
            transform.position,
            nextTargetPos,
            moveSpeed * Time.deltaTime);
    }

    private void EatCorpse(Employee deadEmp)
    {
        if (deadEmp == null) return;

        Debug.Log($"[Slime] Memakan mayat {deadEmp.EmployeeName}! Hapus objek mayat & berubah menjadi Slime Besar.");

        if (Facility.Instance != null)
        {
            Facility.Instance.UnregisterEmployee(deadEmp);
        }

        Destroy(deadEmp.gameObject);
        targetCorpse = null;

        TransformToBigSlime();
    }

    public void TransformToBigSlime()
    {
        isBigSlime = true;
        attackTimer = 0f;
        UpdateAppearance();
        Debug.Log("[Slime] Berhasil bertransformasi menjadi Slime Besar!");
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.gray;

        if (isBigSlime)
        {
            Debug.Log("[Slime] Slime Besar mati! Membelah menjadi 2 Slime Kecil.");
            SpawnAt(transform.position + new Vector3(-0.4f, 0f, 0f), false);
            SpawnAt(transform.position + new Vector3(0.4f, 0f, 0f), false);
            Destroy(gameObject, 0.1f);
        }
        else
        {
            Debug.Log("[Slime] Slime Kecil mati! Spawning Virus di lokasi kematian.");
            Virus.SpawnAt(transform.position);
            Destroy(gameObject, 0.1f);
        }
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

        Debug.Log($"[Slime] Di-klik kanan! Membuka EmployeeSelectPopup untuk menugaskan Security.");

        Slime capturedPest = this;

        EmployeeSelectPopup.Instance.Open(selectedEmp =>
        {
            if (selectedEmp != null && capturedPest != null && !capturedPest.IsDead)
            {
                selectedEmp.EnqueueTask(new KillPestTask(capturedPest));
                Debug.Log($"[Slime] {selectedEmp.EmployeeName} ditugaskan untuk membunuh Slime.");
            }
        }, typeof(DivisionSecurity));
    }

    public static Slime SpawnAt(Vector3 spawnPos, bool isBig = false)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Slime.prefab");
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
        }
#else
        GameObject prefab = null;
#endif
        GameObject go = null;
        if (prefab != null)
        {
            go = Instantiate(prefab, spawnPos, Quaternion.identity);
            if (go.GetComponent<Slime>() == null)
            {
                Jamur jamur = go.GetComponent<Jamur>();
                if (jamur != null) Destroy(jamur);
                go.AddComponent<Slime>();
            }
        }
        else
        {
            go = new GameObject("Slime");
            go.transform.position = spawnPos;
            go.AddComponent<Slime>();
            var sr = go.AddComponent<SpriteRenderer>();
            var circle = Resources.GetBuiltinResource<Sprite>("Sprites-Default.png");
            if (circle != null) sr.sprite = circle;
        }

        Slime slime = go.GetComponent<Slime>();
        if (slime != null && isBig)
        {
            slime.TransformToBigSlime();
        }

        Debug.Log($"[Slime] Slime ({ (isBig ? "Besar" : "Kecil") }) di-spawn pada posisi {spawnPos}");
        return slime;
    }

    public static new void Spawn()
    {
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

                SpawnAt(spawnPos, false);
            }
        }
    }
}
