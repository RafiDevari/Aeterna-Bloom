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

    [Header("Hazard Mechanics")]
    [SerializeField] private float hazardInterval = 1.0f;
    [SerializeField] private int hazardDamageAmount = 5;
    protected float hazardTimer = 0f;

    protected bool isDead = false;

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
            if (room != null && (room.IsPoisoned || room.IsSterilizing))
            {
                Hp -= hazardDamageAmount;
                string hazardType = room.IsSterilizing ? "Sterilisasi" : "Racun";
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
}
