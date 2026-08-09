using UnityEngine;

/// <summary>
/// Class dasar untuk entitas Hama (Pest). 
/// Hama dapat mati dan akan terkena damage jika berada di ruangan yang sedang disterilisasi atau beracun.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Pest : MonoBehaviour
{
    [Header("Pest Info")]
    [SerializeField] private string pestName = "Hama";
    
    [Header("Stats")]
    [SerializeField] private int hp = 20;
    [SerializeField] private int maxHp = 20;

    [Header("Immunities")]
    [SerializeField] protected bool immuneToPoison = false;

    [Header("Hazard Mechanics")]
    [SerializeField] private float hazardInterval = 1.0f;
    [SerializeField] private int hazardDamageAmount = 5;
    protected float hazardTimer = 0f;

    protected bool isDead = false;

    public bool IsDead => isDead;

    public string PestName
    {
        get
        {
            if (!string.IsNullOrEmpty(pestName) && pestName != "Hama") return pestName;
            return GetType().Name;
        }
    }

    protected virtual void Start()
    {
        FacilityHUD.ShowBroadcast($"{PestName} Terdeteksi difasilitas", "System");
    }

    public int Hp
    {
        get => hp;
        private set
        {
            if (isDead) return;
            hp = Mathf.Clamp(value, 0, maxHp);
            if (hp <= 0)
            {
                Die();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        Hp -= amount;
    }

    public void Kill()
    {
        Die();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        HandleRoomHazards();
    }

    protected virtual void HandleRoomHazards()
    {
        hazardTimer += Time.deltaTime;
        if (hazardTimer >= hazardInterval)
        {
            hazardTimer = 0f;
            Room room = RoomPathfinder.FindRoomAt(transform.position);
            
            // Hama terkena damage dari Racun ATAU Sterilisasi
            bool takesPoisonDamage = room != null && room.IsPoisoned && !immuneToPoison;
            bool takesSterilizeDamage = room != null && room.IsSterilizing;

            if (takesPoisonDamage || takesSterilizeDamage)
            {
                Hp -= hazardDamageAmount;
                string hazardType = takesSterilizeDamage ? "Sterilisasi" : "Racun";
                Debug.Log($"[Hama] {pestName} terkena {hazardType}! HP tersisa: {hp}");
            }
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[Hama] {pestName} telah mati.");

        // Nonaktifkan collider
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Beri warna abu-abu / hancurkan objek
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        // Destroy(gameObject, 2f); // Boleh di-uncomment jika ingin otomatis terhapus
    }

    private static bool hasSpawned = false;

    public static void Spawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
#else
        GameObject prefab = null;
#endif
        if (prefab == null)
        {
            Debug.LogError("[Pest] Prefab Jamur tidak ditemukan di Assets/Prefabs/PestPrefabs/Jamur.prefab!");
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

                Instantiate(prefab, spawnPos, Quaternion.identity);
                Debug.Log($"[Pest] Jamur berhasil di-spawn secara random di ruangan {targetRoom.RoomName} pada posisi {spawnPos}");
            }
        }
    }
}

