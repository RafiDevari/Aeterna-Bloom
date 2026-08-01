using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Collider")]
    [Tooltip("Otomatis sesuaikan ukuran BoxCollider2D dengan sprite saat Awake / ganti sprite.")]
    [SerializeField] private bool autoFitCollider = true;

    private BoxCollider2D boxCollider;

    [Header("Visual")]
    [Tooltip("Sprite default untuk containment unit ini. Akan otomatis di-apply ke SpriteRenderer.")]
    [SerializeField] private Sprite unitSprite;
    [SerializeField] private SpriteRenderer unitRenderer; // opsional, auto-cari kalau kosong

    [Header("Monster — assign prefab monster di sini")]
    [Tooltip("Drag prefab monster ke sini. Kosongkan jika unit ini tidak punya monster.")]
    [SerializeField] private GameObject monsterPrefab;

    // Instance monster yang sedang aktif (di-spawn dari prefab)
    private MonsterBase monster;
    private Room parentRoom;

    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action<ContainmentUnit> OnUnitClicked;
    public System.Action<MonsterBase> OnMonsterAssigned;
    public System.Action OnMonsterRemoved;

    public static System.Action<ContainmentUnit> OnAnyUnitClicked;

    // ── Properties ────────────────────────────────────────────────────────────
    public string     UnitName   { get => unitName;  set => unitName = value; }
    public MonsterBase Monster   => monster;
    public bool        HasMonster => monster != null;
    public Room        ParentRoom => parentRoom;
    public MonsterContext Context { get; private set; }
    public SpriteRenderer UnitRenderer => unitRenderer;
    

    public Sprite UnitSprite
    {
        get => unitSprite;
        set
        {
            unitSprite = value;
            ApplySprite();
        }
    }

    // ── Init ──────────────────────────────────────────────────────────────────
    public void SetParentRoom(Room room)
    {
        parentRoom = room;

        Context = new MonsterContext(this);

        Debug.Log($"SetParentRoom dipanggil -> {room}");
    }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        ApplySprite();

        // Auto-find parent room if null (for pre-placed units)
        if (parentRoom == null)
        {
            parentRoom = GetComponentInParent<Room>();
            if (parentRoom != null)
            {
                Context = new MonsterContext(this);
            }
        }

        // Auto-add ContainmentUnitOverlay script to handle growth and mood display
        if (GetComponent<ContainmentUnitOverlay>() == null)
        {
            gameObject.AddComponent<ContainmentUnitOverlay>();
        }
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
    private void ApplySprite()
    {
        if (unitRenderer == null)
            unitRenderer = GetComponent<SpriteRenderer>();

        if (unitRenderer != null && unitSprite != null)
            unitRenderer.sprite = unitSprite;

        FitColliderToSprite();
    }
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


    private void FitColliderToSprite()
    {
        if (!autoFitCollider) return;

        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        if (unitRenderer == null || unitRenderer.sprite == null || boxCollider == null)
            return;

        Bounds spriteBounds = unitRenderer.sprite.bounds;

        boxCollider.size = spriteBounds.size;
        boxCollider.offset = spriteBounds.center;
    }

    /// <summary>
    /// Assign instance MonsterBase yang sudah ada (tidak spawn baru).
    /// </summary>
    public virtual void AssignMonster(MonsterBase newMonster)
    {
        if (newMonster == null) return;

        monster = newMonster;
        monster.InitUnit(this);

        if (monster.MonsterRenderer != null && unitRenderer != null)
        {
            monster.MonsterRenderer.sortingLayerID = unitRenderer.sortingLayerID;
            monster.MonsterRenderer.sortingOrder   = unitRenderer.sortingOrder + 1;
        }
        
        OnMonsterAssigned?.Invoke(monster);
        Debug.Log($"[ContainmentUnit:{unitName}] Monster aktif: {monster.MonsterName}");
    }

    public virtual void RemoveMonster()
    {
        if (monster != null)
        {
            Destroy(monster.gameObject);
            monster = null;
            OnMonsterRemoved?.Invoke();
        }
    }

    // ── Click Handling ────────────────────────────────────────────────────────
    private void OnMouseUp()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

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
        OnAnyUnitClicked?.Invoke(this);   // <-- baris baru
    }

    /// <summary>
    /// Triggers a temporary visual effect (e.g. Heat, Electric Shock) on this ContainmentUnit.
    /// Supports loading either an animated Prefab or a static Sprite/PNG.
    /// </summary>
    public void TriggerEffect(string effectPath)
    {
        if (string.IsNullOrEmpty(effectPath))
            return;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector3 spawnPosition = col != null ? (Vector3)col.offset : Vector3.zero;

        // 1. Try loading as a Prefab first (for animated/complex effects)
        GameObject effectPrefab = Resources.Load<GameObject>(effectPath);
        if (effectPrefab != null)
        {
            GameObject effectGo = Instantiate(effectPrefab, this.transform);
            effectGo.transform.localPosition = spawnPosition;

            SpriteRenderer prefabSr = effectGo.GetComponentInChildren<SpriteRenderer>();
            if (prefabSr != null && prefabSr.sprite != null)
            {
                ScaleEffectToUnit(effectGo, prefabSr.sprite, col);
            }
            return;
        }

        // 2. Fallback: Load as a Sprite (for simple/un-animated PNG effects)
        Sprite effectSprite = Resources.Load<Sprite>(effectPath);
        if (effectSprite == null)
        {
            Debug.LogWarning($"[ContainmentUnit:{unitName}] Could not load effect prefab or sprite at path: {effectPath}");
            return;
        }

        GameObject genericEffectGo = new GameObject("ContainmentEffect_" + effectSprite.name);
        genericEffectGo.transform.SetParent(this.transform);
        genericEffectGo.transform.localPosition = spawnPosition;

        ScaleEffectToUnit(genericEffectGo, effectSprite, col);

        int sortingOrder = 10;
        if (unitRenderer != null)
        {
            sortingOrder = unitRenderer.sortingOrder + 10;
        }

        // Attach specialized effect script based on path or fallback to ContainmentUnitEffect
        string lowerPath = effectPath.ToLower();
        if (lowerPath.Contains("heat"))
        {
            var heat = genericEffectGo.AddComponent<AeternaBloom.Effects.Common.HeatEffect>();
            heat.Init(effectSprite, sortingOrder);
        }
        else if (lowerPath.Contains("electric") || lowerPath.Contains("shock") || lowerPath.Contains("lightning"))
        {
            var shock = genericEffectGo.AddComponent<AeternaBloom.Effects.Common.LightningEffect>();
            shock.Init(effectSprite, sortingOrder);
        }
        else
        {
            var fallbackEffect = genericEffectGo.AddComponent<AeternaBloom.Effects.Containment.ContainmentUnitEffect>();
            fallbackEffect.Init(effectSprite, sortingOrder);
        }
    }

    private void ScaleEffectToUnit(GameObject effectGo, Sprite effectSprite, BoxCollider2D col)
    {
        Vector2 targetSize = Vector2.one;
        if (unitRenderer != null && unitRenderer.sprite != null)
        {
            targetSize = unitRenderer.sprite.bounds.size;
        }
        else if (col != null)
        {
            targetSize = col.size;
        }

        Vector2 spriteSize = effectSprite.bounds.size;
        float scaleX = spriteSize.x > 0 ? (targetSize.x / spriteSize.x) : 1f;
        float scaleY = spriteSize.y > 0 ? (targetSize.y / spriteSize.y) : 1f;
        effectGo.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}

