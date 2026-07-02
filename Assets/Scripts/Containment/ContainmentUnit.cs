using UnityEngine;

/// <summary>
/// ContainmentUnit menampung 1 Monster.
/// Assign monster prefab langsung di Inspector field "Monster Prefab".
/// ContainmentUnit akan spawn dan inisialisasi monster-nya sendiri saat Start().
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ContainmentUnit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] private string unitName = "Containment Unit";

    [Header("Monster — assign prefab monster di sini")]
    [Tooltip("Drag prefab monster ke sini. Kosongkan jika unit ini tidak punya monster.")]
    [SerializeField] private GameObject monsterPrefab;

    // Instance monster yang sedang aktif (di-spawn dari prefab)
    private MonsterBase monster;
    private Room parentRoom;

    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action<ContainmentUnit> OnUnitClicked;
    public System.Action<MonsterBase>     OnMonsterAssigned;

    // ── Properties ────────────────────────────────────────────────────────────
    public string     UnitName   { get => unitName;  set => unitName = value; }
    public MonsterBase Monster   => monster;
    public bool        HasMonster => monster != null;
    public Room        ParentRoom => parentRoom;
    public MonsterContext Context { get; private set; }

    // ── Init ──────────────────────────────────────────────────────────────────
    public void SetParentRoom(Room room)
    {
        parentRoom = room;

        Context = new MonsterContext(this);

        Debug.Log($"SetParentRoom dipanggil -> {room}");
    }

    private void Start()
    {
        // Spawn monster dari prefab yang di-assign di Inspector
        if (monsterPrefab != null)
            SpawnMonsterFromPrefab(monsterPrefab);
    }

    // ── Monster Spawning ──────────────────────────────────────────────────────

    /// <summary>
    /// Spawn monster dari prefab, lalu inisialisasi ke unit ini.
    /// Dipanggil otomatis dari Start() jika monsterPrefab di-assign.
    /// Bisa juga dipanggil runtime untuk ganti monster.
    /// </summary>
    public void SpawnMonsterFromPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        // Hapus monster lama kalau ada
        if (monster != null) RemoveMonster();

        var go = Instantiate(prefab, transform);
        go.transform.localPosition = Vector3.zero;

        var m = go.GetComponentInChildren<MonsterBase>(includeInactive: true);
        if (m == null)
        {
            Debug.LogError($"[ContainmentUnit:{unitName}] Prefab '{prefab.name}' tidak punya " +
                           "komponen MonsterBase! Pastikan script monster sudah di-attach.");
            Destroy(go);
            return;
        }

        AssignMonster(m);
    }

    /// <summary>
    /// Assign instance MonsterBase yang sudah ada (tidak spawn baru).
    /// </summary>
    public virtual void AssignMonster(MonsterBase newMonster)
    {
        if (newMonster == null) return;

        monster = newMonster;
        monster.InitUnit(this);
        OnMonsterAssigned?.Invoke(monster);
        Debug.Log($"[ContainmentUnit:{unitName}] Monster aktif: {monster.MonsterName}");
    }

    public virtual void RemoveMonster()
    {
        if (monster != null)
        {
            Destroy(monster.gameObject);
            monster = null;
        }
    }

    // ── Click Handling ────────────────────────────────────────────────────────
    private void OnMouseDown()
    {
        HandleClick();
    }

    protected virtual void HandleClick()
    {
        if (!HasMonster)
        {
            Debug.Log($"[ContainmentUnit:{unitName}] Kosong — tidak ada monster.");
            return;
        }

        Debug.Log($"[ContainmentUnit:{unitName}] " +
                  $"Monster: {monster.MonsterName} | Mood: {monster.Mood} | Growth: {monster.Growth:P0}");
        OnUnitClicked?.Invoke(this);
    }

}
