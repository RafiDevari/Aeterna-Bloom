using UnityEngine;

/// <summary>
/// Employee divisi Security. Punya privilege khusus untuk bisa masuk ke ruangan yang sedang Lockdown
/// (misal untuk melakukan sterilisasi hama atau membersihkan racun).
/// </summary>
public class EmployeeSecurity : Employee
{
    /// <summary>
    /// Memerintahkan Security untuk pergi ke ruangan tertentu dan mensterilisasinya.
    /// </summary>
    public void GoSterilize(Room targetRoom)
    {
        if (targetRoom == null) return;

        ClearTasksAndInterrupt();

        Debug.Log($"[Security] {EmployeeName} ditugaskan mensterilisasi ruangan {targetRoom.RoomName}. Berangkat sekarang!");

        // 1. Bergerak ke target ruangan
        EnqueueTask(new MoveToTask(() => targetRoom.transform.position, () => targetRoom != null));
        
        // 2. Mulai proses sterilisasi setelah sampai
        EnqueueTask(new SterilizeTask(targetRoom));
    }

    // ==========================================
    // Context Menu untuk Testing Mudah di Editor
    // ==========================================
    [ContextMenu("Test Auto Sterilize")]
    public void TestAutoSterilize()
    {
        if (Facility.Instance == null)
        {
            Debug.LogWarning("[Security] Facility instance tidak ditemukan!");
            return;
        }

        Room target = null;
        // Cari ruangan pertama yang sedang Lockdown ATAU Poisoned
        foreach (var room in Facility.Instance.Rooms)
        {
            if (room.IsLocked || room.IsPoisoned)
            {
                target = room;
                break;
            }
        }

        if (target != null)
        {
            GoSterilize(target);
        }
        else
        {
            Debug.Log("[Security] Tidak ada ruangan yang berstatus Lockdown atau Poisoned untuk disterilisasi saat ini.");
        }
    }
}
