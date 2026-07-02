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

    public System.Action<float> OnTemperatureChanged;

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

            Debug.Log($"[{roomName}] Temperature = {temperature:F1}");
        }
    }

    public virtual void InitFromFacility(float defaultTemperature)
    {
        temperature = defaultTemperature;
    }

    protected virtual void Start()
    {
        if (Facility.Instance != null &&
            !Facility.Instance.Rooms.Contains(this))
        {
            Facility.Instance.AddRoom(this);
        }
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

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(4,2,0.1f));

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            $"{roomName}\n{temperature:F1}°C");
    }
#endif
}