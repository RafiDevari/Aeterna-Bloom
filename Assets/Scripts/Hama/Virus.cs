using UnityEngine;

/// <summary>
/// Hama jenis Virus. Berukuran sangat kecil / invisible.
/// Jika bersentuhan (trigger collision) dengan Employee yang tidak imun (bukan Medic),
/// virus akan hilang (Destroy) dan Employee tersebut akan terkena status Sick (IsSick = true).
/// Employee Medic bersifat imun terhadap Virus.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Virus : Pest
{
    [Header("Virus Visual & Contact")]
    [SerializeField] private Vector3 visualScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private bool makeInvisible = true;

    protected void Awake()
    {
        immuneToPoison = true;
    }

    private void Start()
    {
        transform.localScale = visualScale;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Memastikan ada Rigidbody2D agar deteksi trigger collision dengan Employee bekerja
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

        if (makeInvisible)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.05f; // Hampir tidak terlihat
                sr.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        Employee emp = other.GetComponent<Employee>();
        if (emp == null)
        {
            emp = other.GetComponentInParent<Employee>();
        }

        if (emp != null && emp.CurrentState != EmployeeState.Dead)
        {
            if (emp.IsImmuneToVirus)
            {
                Debug.Log($"[Virus] Bersentuhan dengan {emp.EmployeeName} (Medic/Imun), virus tidak menginfeksi.");
                return;
            }

            Debug.Log($"[Virus] Bersentuhan dengan {emp.EmployeeName}! Virus lenyap dan employee menjadi SICK.");
            emp.InfectVirus();
            Destroy(gameObject);
        }
    }

    private static bool hasSpawned = false;

    public static new void Spawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Virus.prefab");
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
        }
#else
        GameObject prefab = null;
#endif
        if (prefab == null)
        {
            Debug.LogError("[Virus] Prefab Virus / Jamur tidak ditemukan!");
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
                if (go.GetComponent<Virus>() == null)
                {
                    Jamur jamur = go.GetComponent<Jamur>();
                    if (jamur != null) Destroy(jamur);
                    go.AddComponent<Virus>();
                }
                Debug.Log($"[Virus] Virus berhasil di-spawn di ruangan {targetRoom.RoomName} pada posisi {spawnPos}");
            }
        }
    }
}
