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