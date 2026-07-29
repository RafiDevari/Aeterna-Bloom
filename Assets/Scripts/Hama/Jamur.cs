using UnityEngine;

/// <summary>
/// Hama jenis Jamur. Jika berada di dalam ruangan selama 10 detik tanpa disterilisasi,
/// ia akan menyebarkan racun ke ruangan (IsPoisoned = true) dan mengurangi Mood monster di dalamnya sebesar 1.
/// </summary>
public class Jamur : Pest
{
    [Header("Jamur Mechanics")]
    [SerializeField] private float poisonSpreadInterval = 10f;
    private float poisonTimer = 0f;

    [Header("Spreading Mechanics")]
    [SerializeField] private float spreadInterval = 20f;
    private float spreadTimer = 0f;

    private void Awake()
    {
        immuneToPoison = true;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;

        HandlePoisonSpread();
        HandleSelfSpreading();
    }

    private float debugTimer = 0f;

    private void HandlePoisonSpread()
    {
        poisonTimer += Time.deltaTime;
        debugTimer += Time.deltaTime;

        Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);

        if (debugTimer >= 2f)
        {
            debugTimer = 0f;
            if (currentRoom == null)
            {
                Debug.LogWarning($"[Jamur Debug] Jamur di posisi {transform.position} TIDAK mendeteksi ruangan apapun! Pastikan posisi Jamur (terutama sumbu Y dan Z) menyentuh dasar ruangan.");
            }
            else
            {
                Debug.Log($"[Jamur Debug] Jamur mendeteksi berada di dalam ruangan: {currentRoom.RoomName}. Timer racun: {poisonTimer:F1} / {poisonSpreadInterval}");
            }
        }
        
        if (poisonTimer >= poisonSpreadInterval)
        {
            poisonTimer = 0f;
            
            if (currentRoom != null)
            {
                bool newlyPoisoned = false;

                // Jika belum beracun, buat jadi beracun
                if (!currentRoom.IsPoisoned)
                {
                    currentRoom.IsPoisoned = true;
                    newlyPoisoned = true;
                    Debug.Log($"[Jamur] Meracuni ruangan {currentRoom.RoomName}!");
                }

                // Jika ruangan adalah ContainmentRoom, kurangi mood semua monster di dalamnya
                if (currentRoom is ContainmentRoom containmentRoom)
                {
                    foreach (var unit in containmentRoom.ContainmentUnits)
                    {
                        if (unit != null && unit.HasMonster)
                        {
                            unit.Monster.ModifyMood(-1);
                            Debug.Log($"[Jamur] Mood monster {unit.Monster.MonsterName} berkurang 1 akibat wabah jamur.");
                        }
                    }
                }
                else
                {
                    if (!newlyPoisoned)
                    {
                        Debug.Log($"[Jamur] Ruangan {currentRoom.RoomName} masih beracun, jamur terus berkembang biak.");
                    }
                }
            }
        }
    }

    private void HandleSelfSpreading()
    {
        spreadTimer += Time.deltaTime;
        if (spreadTimer >= spreadInterval)
        {
            spreadTimer = 0f;

            Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);
            if (currentRoom != null)
            {
                // Kondisi: Jamur masih hidup (isDead == false), ruangan tidak disterilisasi, dan ruangan tidak dilockdown
                if (!currentRoom.IsSterilizing && !currentRoom.IsLocked)
                {
                    Debug.Log($"[Jamur Spreading] Jamur di {currentRoom.RoomName} tidak di-sterilize dan tidak di-lockdown selama {spreadInterval} detik. Spawning jamur baru...");
                    SpawnNew();
                }
                else
                {
                    Debug.Log($"[Jamur Spreading] Jamur di {currentRoom.RoomName} batal menyebar karena ruangan dalam status Locked ({currentRoom.IsLocked}) atau Sterilizing ({currentRoom.IsSterilizing}).");
                }
            }
            else
            {
                Debug.LogWarning($"[Jamur Spreading] Jamur di {transform.position} tidak berada dalam ruangan. Batal menyebar.");
            }
        }
    }

    public static void SpawnNew()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PestPrefabs/Jamur.prefab");
#else
        GameObject prefab = null;
#endif
        if (prefab == null)
        {
            Debug.LogError("[Jamur] Prefab Jamur tidak ditemukan di Assets/Prefabs/PestPrefabs/Jamur.prefab!");
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
                Debug.Log($"[Jamur] Jamur baru berhasil di-spawn secara random di ruangan {targetRoom.RoomName} pada posisi {spawnPos}");
            }
        }
    }
}
