// Room.cs
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// Base class semua jenis Room.
/// Berisi informasi umum yang dimiliki semua Room.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public abstract class Room : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private string roomName = "Room";

    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    private float temperature = 20f;

    [Header("Electricity")]
    [Tooltip("Biaya listrik dasar room ini. Total ElectricityCost = base + biaya monster + biaya selisih suhu.")]
    [SerializeField] private float baseElectricityCost = 2f;

    private float monsterElectricityCost = 0f;

    [Header("Lockdown")]
    [Tooltip("Kalau true, room ini tidak bisa dilewati sama sekali oleh pathfinding : " +
             "bukan cuma tidak bisa jadi tujuan, tapi juga tidak bisa jadi titik transit. " +
             "Employee yang sedang berada di dalam room ini otomatis tidak akan menemukan jalan keluar.")]
    [SerializeField] private bool isLocked = false;

    [Header("Poison & Hazard")]
    [SerializeField] private bool isPoisoned = false;
    [SerializeField] private bool isSterilizing = false;

    public System.Action<float> OnTemperatureChanged;
    public System.Action<bool> OnLockChanged;
    public System.Action<float> OnElectricityCostChanged;
    public System.Action<bool> OnPoisonChanged;
    public System.Action<bool> OnSterilizeChanged;

    private Color originalColor = Color.white;

    public string RoomName
    {
        get => roomName;
        set => roomName = value;
    }

    /// <summary>
    /// Kumpulan Bounds yang merepresentasikan bentuk NYATA room ini, dipakai untuk
    /// pathfinding & overlap detection (RoomPathfinder). Default cuma 1 elemen (RoomBounds).
    ///
    /// Room berbentuk gabungan (mis. MainRoom yang berbentuk T/⊥) HARUS override ini
    /// dengan bagian-bagiannya secara terpisah -- BUKAN 1 Bounds hasil Encapsulate/union,
    /// karena union dari bentuk non-persegi bisa mencakup area kosong yang secara visual
    /// bukan bagian room (mis. pojok kosong pada bentuk T). RoomPathfinder mengecek
    /// SETIAP elemen array ini satu-satu, bukan 1 kotak pembungkus.
    /// </summary>
    public virtual Bounds[] CollisionBounds
    {
        get
        {
            return new Bounds[] { RoomBounds };
        }
    }

    /// <summary>
    /// True jika titik berada di salah satu bagian room.
    /// Pathfinding sebaiknya memakai fungsi ini daripada RoomBounds.Contains().
    /// </summary>
    public bool Contains(Vector3 point)
    {
        foreach (Bounds bounds in CollisionBounds)
        {
            if (bounds.Contains(point))
                return true;
        }

        return false;
    }

    public float Temperature
    {
        get => temperature;
        set
        {
            temperature = value;
            OnTemperatureChanged?.Invoke(temperature);
            RaiseElectricityCostChanged();

            Debug.Log($"[{roomName}] Temperature = {temperature:F1}");
        }
    }

    /// <summary>
    /// Biaya listrik dasar room ini (diatur lewat inspector, default 2).
    /// </summary>
    public float BaseElectricityCost
    {
        get => baseElectricityCost;
        set
        {
            baseElectricityCost = value;
            RaiseElectricityCostChanged();
        }
    }

    /// <summary>
    /// Biaya listrik tambahan akibat monster yang ada di room ini.
    /// Di-set dari luar (mis. oleh ContainmentUnit lewat MonsterContext), bukan dari inspector.
    /// </summary>
    public float MonsterElectricityCost
    {
        get => monsterElectricityCost;
        set
        {
            if (Mathf.Approximately(monsterElectricityCost, value))
                return;

            monsterElectricityCost = value;
            RaiseElectricityCostChanged();
        }
    }

    /// <summary>
    /// Biaya listrik tambahan akibat selisih suhu room ini terhadap suhu global facility.
    /// Semakin jauh suhu room dari suhu default facility (lebih panas ataupun lebih dingin),
    /// semakin besar biayanya.
    /// </summary>
    public float TemperatureElectricityCost =>
        Facility.Instance != null
            ? Mathf.Abs(Facility.Instance.DefaultRoomTemperature - temperature)
            : 0f;

    /// <summary>
    /// Total kebutuhan listrik room ini : base cost + biaya monster + biaya selisih suhu.
    /// Facility menjumlahkan ElectricityCost semua room untuk dapat total pemakaian listrik.
    /// </summary>
    public float ElectricityCost => baseElectricityCost + monsterElectricityCost + TemperatureElectricityCost;

    /// <summary>
    /// True kalau room sedang lockdown. Room lockdown dianggap TIDAK ADA oleh
    /// RoomPathfinder (bukan cuma "mahal untuk dilewati") : tidak bisa jadi
    /// tujuan, tidak bisa jadi transit, dan siapapun yang sedang di dalamnya
    /// tidak akan menemukan jalur keluar sampai lockdown dicabut.
    /// </summary>
    public bool IsLocked
    {
        get => isLocked;
        protected set
        {
            if (isLocked == value)
                return;

            isLocked = value;
            OnLockChanged?.Invoke(isLocked);

            Debug.Log($"[{roomName}] Lockdown : {(isLocked ? "AKTIF" : "nonaktif")}");
        }
    }

    public bool IsPoisoned
    {
        get => isPoisoned;
        set
        {
            if (isPoisoned == value) return;
            isPoisoned = value;
            OnPoisonChanged?.Invoke(isPoisoned);
            UpdatePoisonVisuals();
        }
    }

    public bool IsSterilizing
    {
        get => isSterilizing;
        set
        {
            if (isSterilizing == value) return;
            isSterilizing = value;

            if (isSterilizing)
            {
                // Sterilisasi menghilangkan state buruk seperti racun
                if (IsPoisoned) IsPoisoned = false;
            }

            OnSterilizeChanged?.Invoke(isSterilizing);
            UpdatePoisonVisuals();
            Debug.Log($"[{roomName}] Sterilization State : {(isSterilizing ? "ACTIVE" : "inactive")}");
        }
    }

    [ContextMenu("Toggle Poison")]
    public void TogglePoison()
    {
        IsPoisoned = !IsPoisoned;
    }

    [ContextMenu("Toggle Sterilize")]
    public void ToggleSterilize()
    {
        IsSterilizing = !IsSterilizing;
    }

    private void UpdatePoisonVisuals()
    {
        if (spriteRenderer != null)
        {
            if (isSterilizing)
            {
                // Warna oranye pudar yang berbeda dari racun
                spriteRenderer.color = new Color(1f, 0.6f, 0.2f, 1f);
            }
            else if (isPoisoned)
            {
                // Warna hijau racun
                spriteRenderer.color = new Color(0.3f, 0.9f, 0.4f, 1f);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Set status lockdown room ini. Sistem lockdown penuh (trigger dari UI, dsb)
    /// belum diimplementasi - ini cuma fondasi supaya RoomPathfinder sudah siap
    /// dipakai begitu fitur lockdown-nya dibuat.
    /// </summary>
    public void SetLocked(bool locked)
    {
        IsLocked = locked;
    }

    /// <summary>
    /// Triggers a temporary visual effect in this room.
    /// Supports loading either an animated Prefab or a static Sprite/PNG.
    /// </summary>
    /// <param name="effectPath">The resource path to the Prefab or JPG/PNG sprite.</param>
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

            // Scale to fill the room size (matching prefab's root size if it has a SpriteRenderer)
            SpriteRenderer prefabSr = effectGo.GetComponentInChildren<SpriteRenderer>();
            if (prefabSr != null && prefabSr.sprite != null)
            {
                ScaleEffectToRoom(effectGo, prefabSr.sprite, col);
            }
            return;
        }

        // 2. Fallback: Load as a Sprite (for simple/un-animated PNG effects)
        Sprite effectSprite = Resources.Load<Sprite>(effectPath);
        if (effectSprite == null)
        {
            Debug.LogWarning($"[{RoomName}] Could not load effect prefab or sprite at path: {effectPath}");
            return;
        }

        GameObject genericEffectGo = new GameObject("RoomEffect_" + effectSprite.name);
        genericEffectGo.transform.SetParent(this.transform);
        genericEffectGo.transform.localPosition = spawnPosition;

        ScaleEffectToRoom(genericEffectGo, effectSprite, col);

        int sortingOrder = 10;
        if (spriteRenderer != null)
        {
            sortingOrder = spriteRenderer.sortingOrder + 10;
        }

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
            var fallbackEffect = genericEffectGo.AddComponent<AeternaBloom.Effects.Room.RoomEffect>();
            fallbackEffect.Init(effectSprite, sortingOrder);
        }
    }

    /// <summary>
    /// Triggers a temporary small visual effect (e.g., broken heart / mood indicator) 
    /// placed in the top-left corner of the room for 3 seconds.
    /// </summary>
    /// <param name="effectPath">The resource path to the Prefab or JPG/PNG sprite.</param>
    public void TriggerSmallEffect(string effectPath)
    {
        if (string.IsNullOrEmpty(effectPath))
            return;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector3 spawnPosition = Vector3.zero;
        if (col != null)
        {
            // Position at top-left with a small offset/margin so it's not cut off by borders.
            // Using a default of 0.6f units margin.
            float marginX = 0.6f;
            float marginY = 0.6f;
            spawnPosition = new Vector3(col.offset.x - col.size.x / 2f + marginX, col.offset.y + col.size.y / 2f - marginY, 0f);
        }

        // 1. Try loading as a Prefab first (for animated/complex small effects)
        GameObject effectPrefab = Resources.Load<GameObject>(effectPath);
        if (effectPrefab != null)
        {
            GameObject effectGo = Instantiate(effectPrefab, this.transform);
            effectGo.transform.localPosition = spawnPosition;
            // Keeps the prefab's default local scale instead of scaling to the room
            return;
        }

        // 2. Fallback: Load as a Sprite (for simple/un-animated small PNG effects)
        Sprite effectSprite = Resources.Load<Sprite>(effectPath);
        if (effectSprite == null)
        {
            Debug.LogWarning($"[{RoomName}] Could not load small effect prefab or sprite at path: {effectPath}");
            return;
        }

        GameObject genericEffectGo = new GameObject("RoomSmallEffect_" + effectSprite.name);
        genericEffectGo.transform.SetParent(this.transform);
        genericEffectGo.transform.localPosition = spawnPosition;
        genericEffectGo.transform.localScale = Vector3.one; // Keep original sprite scale

        int sortingOrder = 15; // Render on top of standard room effects
        if (spriteRenderer != null)
        {
            sortingOrder = spriteRenderer.sortingOrder + 15;
        }

        // Attach generic RoomEffect script to handle the Sprite rendering and 3-second lifetime
        var fallbackEffect = genericEffectGo.AddComponent<AeternaBloom.Effects.Room.RoomEffect>();
        fallbackEffect.Init(effectSprite, sortingOrder);
    }


    private void ScaleEffectToRoom(GameObject effectGo, Sprite effectSprite, BoxCollider2D col)
    {
        Vector2 targetSize = Vector2.one;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            targetSize = spriteRenderer.sprite.bounds.size;
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

    public virtual void InitFromFacility(float defaultTemperature)
    {
        Temperature = defaultTemperature;
    }

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Ensure BoxCollider2D exists on all Room instances
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
    }

    protected virtual void Start()
    {
        UpdatePoisonVisuals();

        // Configure BoxCollider2D
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                col.size = spriteRenderer.sprite.bounds.size;
                col.offset = spriteRenderer.sprite.bounds.center;
            }
        }

        if (Facility.Instance != null)
        {
            if (!Facility.Instance.Rooms.Contains(this))
            {
                Facility.Instance.AddRoom(this);
            }
            else
            {
                InitFromFacility(Facility.Instance.DefaultRoomTemperature);
            }

            Facility.Instance.OnDefaultRoomTemperatureChanged += HandleGlobalTemperatureChanged;
        }
    }

    protected virtual void OnMouseOver()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1)) // Klik Kanan
        {
            if (IsPoisoned)
            {
                HandlePoisonClick();
            }
        }
    }

    protected void HandlePoisonClick()
    {
        Debug.Log($"[{RoomName}] Membuka EmployeeSelectPopup untuk menugaskan sterilisasi.");

        if (EmployeeSelectPopup.Instance != null)
        {
            EmployeeSelectPopup.Instance.Open(
                employee => {
                    employee.GoSterilize(this);
                },
                typeof(DivisionSecurity) // Default ke divisi Security
            );
        }
        else
        {
            Debug.LogError("[Room] EmployeeSelectPopup.Instance belum ada. Pastikan component EmployeeSelectPopup sudah ditambahkan ke scene.");
        }
    }

    protected virtual void OnDestroy()
    {
        if (Facility.Instance != null)
        {
            Facility.Instance.OnDefaultRoomTemperatureChanged -= HandleGlobalTemperatureChanged;
        }
    }

    private void HandleGlobalTemperatureChanged(float _)
    {
        RaiseElectricityCostChanged();
    }

    private void RaiseElectricityCostChanged()
    {
        OnElectricityCostChanged?.Invoke(ElectricityCost);
    }

    protected virtual void Update()
    {
        OnRoomUpdate();
    }

    protected virtual void OnRoomUpdate()
    {

    }

    public virtual string GetHUDInfo()
    {
        return "";
    }

    //==============================
    // Gizmos - visualisasi RoomBounds
    //==============================

    /// <summary>
    /// Daftar kotak yang digambar sebagai gizmo. Default = CollisionBounds,
    /// supaya visual gizmo SELALU sinkron dengan bounds yang benar-benar
    /// dipakai pathfinding (tidak ada 2 sumber kebenaran yang bisa beda sendiri).
    /// </summary>
    protected virtual Bounds[] GetGizmoBounds()
    {
        return CollisionBounds;
    }

    private void OnDrawGizmos()
    {
        foreach (Bounds bounds in GetGizmoBounds())
        {
            Gizmos.color = isLocked
                ? new Color(1f, 0f, 0f, 0.15f)
                : new Color(0f, 1f, 1f, 0.12f);
            Gizmos.DrawCube(bounds.center, bounds.size);

            Gizmos.color = isLocked ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    /// <summary>
    /// Representasi kasar 1-kotak dari room ini. TIDAK dipakai lagi oleh
    /// RoomPathfinder (yang sekarang pakai CollisionBounds/Contains) -- masih
    /// dipertahankan untuk keperluan lain (mis. kamera, HUD, label posisi)
    /// yang cukup butuh 1 Bounds kasar, bukan bentuk detail per-bagian.
    /// </summary>
    public virtual Bounds RoomBounds
    {
        get
        {
            if (spriteRenderer != null)
            {
                Bounds spriteBounds = spriteRenderer.bounds;

                float floorY = spriteBounds.min.y;
                float height = 0.5f;

                Vector3 center = new Vector3(
                    spriteBounds.center.x,
                    floorY + height * 0.5f,
                    spriteBounds.center.z
                );

                Vector3 size = new Vector3(spriteBounds.size.x, height, spriteBounds.size.z);

                return new Bounds(center, size);
            }

            return new Bounds(transform.position, new Vector3(1f, 1f, 1f));
        }
    }

    /// <summary>
    /// Menghitung koordinat lantai / walkway terdekat di bagian bawah ruangan ini
    /// berdasarkan titik world yang diklik pemain.
    /// Memastikan posisi Y berada tepat di lantai (CollisionBounds) dan posisi X di-clamp
    /// di dalam batas lebar ruangan dengan margin aman.
    /// </summary>
    public Vector3 GetNearestWalkablePosition(Vector3 worldPoint)
    {
        Bounds[] boundsList = CollisionBounds;
        if (boundsList == null || boundsList.Length == 0)
        {
            return transform.position;
        }

        Vector3 bestPoint = boundsList[0].center;
        float minDistanceSqr = float.MaxValue;

        foreach (var b in boundsList)
        {
            // Margin aman dari dinding kiri dan kanan agar employee tidak mentok di ujung
            float marginX = Mathf.Min(0.35f, b.size.x * 0.2f);
            float minX = b.min.x + marginX;
            float maxX = b.max.x - marginX;

            float x = (minX < maxX) ? Mathf.Clamp(worldPoint.x, minX, maxX) : b.center.x;
            
            // Y ditaruh tepat di tengah area walkway/lantai bounds ini
            float y = b.center.y;
            Vector3 candidate = new Vector3(x, y, 0f);

            float distSqr = (worldPoint - candidate).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                bestPoint = candidate;
            }
        }

        bestPoint.z = 0f;
        return bestPoint;
    }

    /// <summary>
    /// Mencari instance Room yang berada di bawah posisi pointer/mouse di world.
    /// Mengecek BoxCollider2D, Contains(), dan SpriteRenderer bounds dari semua Room yang terdaftar.
    /// </summary>
    public static Room FindRoomUnderPointer(Vector3 worldPoint)
    {
        worldPoint.z = 0f;

        if (Facility.Instance != null && Facility.Instance.Rooms != null)
        {
            // 1. Cek langsung via Room.Contains atau SpriteRenderer bounds
            foreach (Room room in Facility.Instance.Rooms)
            {
                if (room == null) continue;

                if (room.Contains(worldPoint))
                    return room;

                if (room.spriteRenderer != null && room.spriteRenderer.bounds.Contains(worldPoint))
                    return room;

                var col = room.GetComponent<BoxCollider2D>();
                if (col != null && col.OverlapPoint(worldPoint))
                    return room;
            }

            // 2. Fallback: Cari room dengan jarak terdekat jika klik sedikit di luar margin (radius 1.5 unit)
            Room closestRoom = null;
            float closestDist = 1.5f;

            foreach (Room room in Facility.Instance.Rooms)
            {
                if (room == null) continue;
                Bounds b = room.spriteRenderer != null ? room.spriteRenderer.bounds : room.RoomBounds;
                float dist = Mathf.Sqrt(b.SqrDistance(worldPoint));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestRoom = room;
                }
            }

            if (closestRoom != null)
                return closestRoom;
        }

        return RoomPathfinder.FindRoomAt(worldPoint);
    }

#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    foreach (Bounds bounds in GetGizmoBounds())
    {
        Gizmos.color = isLocked
            ? new Color(1f, 0f, 0f, 0.35f)
            : new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = isLocked ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(bounds.center, 0.05f);
    }

    UnityEditor.Handles.color = Color.white;
    UnityEditor.Handles.Label(RoomBounds.center, $"{roomName}");
}
#endif
}