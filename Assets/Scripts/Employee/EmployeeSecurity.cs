using UnityEngine;

/// <summary>
/// Employee divisi Security. Punya privilege khusus untuk bisa masuk ke ruangan yang sedang Lockdown
/// (misal untuk melakukan sterilisasi hama atau membersihkan racun).
/// </summary>
public class EmployeeSecurity : Employee
{
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
