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

    protected override void Update()
    {
        base.Update();

        if (isDead) return;

        HandlePoisonSpread();
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
}
