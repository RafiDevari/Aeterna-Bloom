// Room.cs
using UnityEngine;
using System.Linq;

/// <summary>
/// Base class semua jenis Room.
/// Berisi informasi umum yang dimiliki semua Room.
/// </summary>
public abstract class Room : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private string roomName = "Room";

    [SerializeField]
    private float temperature;

    [Header("Bounds")]
    [Tooltip("Ukuran area room ini (world unit), dipusatkan di transform.position. " +
             "Dipakai RoomPathfinder untuk mendeteksi room mana yang bersebelahan " +
             "(bounds saling bersentuhan/overlap) dan untuk mencari room dari sebuah titik (misal klik mouse).")]
    [SerializeField] private Vector2 roomSize = new Vector2(4f, 2f);

    [Header("Electricity")]
    [Tooltip("Biaya listrik dasar room ini. Total ElectricityCost = base + biaya monster + biaya selisih suhu.")]
    [SerializeField] private float baseElectricityCost = 2f;

    private float monsterElectricityCost = 0f;

    [Header("Lockdown")]
    [Tooltip("Kalau true, room ini tidak bisa dilewati sama sekali oleh pathfinding : " +
             "bukan cuma tidak bisa jadi tujuan, tapi juga tidak bisa jadi titik transit. " +
             "Employee yang sedang berada di dalam room ini otomatis tidak akan menemukan jalan keluar.")]
    [SerializeField] private bool isLocked = false;

    public System.Action<float> OnTemperatureChanged;
    public System.Action<bool> OnLockChanged;
    public System.Action<float> OnElectricityCostChanged;

    public string RoomName
    {
        get => roomName;
        set => roomName = value;
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

    public Vector2 RoomSize
    {
        get => roomSize;
        set => roomSize = value;
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
        temperature = defaultTemperature;
    }

    protected virtual void Start()
    {
        if (Facility.Instance != null)
        {
            if (!Facility.Instance.Rooms.Contains(this))
            {
                Facility.Instance.AddRoom(this);
            }

            Facility.Instance.OnDefaultRoomTemperatureChanged += HandleGlobalTemperatureChanged;
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

    private void OnDrawGizmos()
    {
        Bounds bounds = RoomBounds;

        Gizmos.color = isLocked
            ? new Color(1f, 0f, 0f, 0.15f)
            : new Color(0f, 1f, 1f, 0.12f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = isLocked ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    public Bounds RoomBounds => new Bounds(transform.position, new Vector3(roomSize.x, roomSize.y, 1f));

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Bounds bounds = RoomBounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (bounds.extents.y + 0.3f),
            $"{roomName}\n{roomSize.x:F2} x {roomSize.y:F2}\n{temperature:F1}°C" + (isLocked ? "\n[LOCKDOWN]" : ""));
    }
#endif
}