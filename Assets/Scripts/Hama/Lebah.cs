using UnityEngine;

/// <summary>
/// Hama jenis Lebah.
/// - Berada di ContainmentUnit / ruangan tanaman monster.
/// - Tiap 5 detik mengisap Growth tanaman sebesar 3% (-0.03f).
/// - Jika Growth tanaman sudah menyentuh batas bawah state (growThreshold jika pernah tumbuh, atau 0f),
///   maka pengurangan berikutnya akan mengurangi Mood tanaman sebesar 1 (-1 Mood).
/// - Jika Mood tanaman mencapai 0, Lebah akan mati sendiri.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Lebah : Pest
{
    [Header("Lebah Mechanics")]
    [SerializeField] private float drainInterval = 5f;
    [SerializeField] private float growthDrainAmount = 0.03f; // 3%
    private float drainTimer = 0f;

    [SerializeField] private ContainmentUnit targetUnit;
    [SerializeField] private MonsterBase targetMonster;

    public ContainmentUnit TargetUnit => targetUnit;
    public MonsterBase TargetMonster => targetMonster;

    private void Start()
    {
        FindTargetMonster();
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        if (targetMonster == null || targetUnit == null)
        {
            FindTargetMonster();
            return;
        }

        HandleGrowthDrain();
    }

    public void SetTarget(ContainmentUnit unit, MonsterBase monster)
    {
        targetUnit = unit;
        targetMonster = monster;
    }

    private void FindTargetMonster()
    {
        if (targetMonster != null && targetUnit != null) return;

        Room currentRoom = RoomPathfinder.FindRoomAt(transform.position);
        if (currentRoom is ContainmentRoom containmentRoom)
        {
            foreach (var unit in containmentRoom.ContainmentUnits)
            {
                if (unit != null && unit.HasMonster)
                {
                    targetUnit = unit;
                    targetMonster = unit.Monster;
                    break;
                }
            }
        }

        if (targetMonster == null && Facility.Instance != null)
        {
            foreach (var room in Facility.Instance.Rooms)
            {
                if (room is ContainmentRoom cr)
                {
                    foreach (var unit in cr.ContainmentUnits)
                    {
                        if (unit != null && unit.HasMonster)
                        {
                            targetUnit = unit;
                            targetMonster = unit.Monster;
                            break;
                        }
                    }
                }
                if (targetMonster != null) break;
            }
        }
    }

    private void HandleGrowthDrain()
    {
        if (targetMonster == null) return;

        // Kematian otomatis jika mood tanaman 0
        if (targetMonster.Mood <= 0)
        {
            Debug.LogWarning($"[Lebah] Mood tanaman {targetMonster.MonsterName} sudah 0! Lebah mati secara otomatis.");
            Die();
            return;
        }

        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer = 0f;

            // Batas minimal growth berdasarkan state
            float floor = targetMonster.HasGrown ? targetMonster.GrowThreshold : 0f;

            if (targetMonster.Growth <= floor + 0.0001f)
            {
                // Growth sudah di batas bawah -> Mood berkurang 1
                targetMonster.ModifyMood(-1);
                Debug.LogWarning($"[Lebah] Growth {targetMonster.MonsterName} berada di batas minimal ({floor * 100:F0}%). Mood berkurang 1! Mood tersisa: {targetMonster.Mood}");

                if (targetMonster.Mood <= 0)
                {
                    Debug.LogWarning($"[Lebah] Mood tanaman {targetMonster.MonsterName} mencapai 0! Lebah mati sendiri.");
                    Die();
                }
            }
            else
            {
                // Kurangi growth 3%
                targetMonster.ModifyGrowth(-growthDrainAmount);
                Debug.Log($"[Lebah] Menghisap growth {targetMonster.MonsterName}! Growth berkurang 3% (Sisa Growth: {targetMonster.GrowthPercent:F1}%).");
            }
        }
    }

    public static new void Spawn()
    {
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
            foreach (var room in Facility.Instance.Rooms)
            {
                if (room is ContainmentRoom containmentRoom)
                {
                    foreach (var unit in containmentRoom.ContainmentUnits)
                    {
                        if (unit != null && unit.HasMonster)
                        {
                            Vector3 spawnPos = unit.transform.position + new Vector3(0f, 0.5f, 0f);
                            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

                            Lebah lebah = go.GetComponent<Lebah>();
                            if (lebah == null)
                            {
                                Jamur jamur = go.GetComponent<Jamur>();
                                if (jamur != null) Destroy(jamur);
                                lebah = go.AddComponent<Lebah>();
                            }
                            lebah.SetTarget(unit, unit.Monster);
                            Debug.Log($"[Lebah] Lebah berhasil di-spawn pada ContainmentUnit {unit.UnitName}!");
                            return;
                        }
                    }
                }
            }
        }
    }
}
