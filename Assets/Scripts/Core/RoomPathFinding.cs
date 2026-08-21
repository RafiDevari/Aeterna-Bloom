using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility buat cari jalur pergerakan employee.
///
/// PENTING: graph pathfinding di sini beroperasi di level TIAP ELEMEN
/// Room.CollisionBounds ("part"), BUKAN di level Room. Ini wajib untuk room
/// berbentuk gabungan non-convex (mis. MainRoom yang berbentuk T/⊥ dengan
/// HorizontalBar + VerticalBar terpisah) : kalau graph cuma di level Room,
/// titik "pintu" transit bisa dipilih dari bagian (bar) yang berbeda dengan
/// bagian tempat employee sedang berada, sehingga garis lurus di antaranya
/// bisa memotong lewat notch/pojok kosong yang bukan bagian room manapun.
///
/// Dengan graph per-part, SETIAP segmen garis dijamin berada di antara dua
/// titik yang sama-sama ada di 1 Bounds convex (baik itu Bounds yang sama,
/// atau titik pintu hasil irisan dua Bounds bertetangga) -- sehingga seluruh
/// segmen otomatis tidak pernah keluar dari bentuk asli room.
///
/// Room yang IsLocked DIHAPUS TOTAL dari graph (semua part miliknya).
/// </summary>
public static class RoomPathfinder
{
    /// <summary>
    /// Toleransi jarak (world unit) supaya dua bounds yang "nyaris nempel"
    /// (mis. celah kecil akibat placement manual) tetap dianggap bersebelahan.
    /// </summary>
    public const float AdjacencyTolerance = 0.05f;

    /// <summary>Satu bagian/potongan geometris dari sebuah Room (1 elemen CollisionBounds-nya).</summary>
    private readonly struct RoomPart
    {
        public readonly Room room;
        public readonly Bounds bounds;

        public RoomPart(Room room, Bounds bounds)
        {
            this.room = room;
            this.bounds = bounds;
        }
    }

    private static List<RoomPart> CollectParts(bool canEnterLockedRooms = false, bool includeDisabledLifts = false)
    {
        var parts = new List<RoomPart>();

        if (Facility.Instance == null)
            return parts;

        bool isBlackout = Facility.Instance.IsBlackout;

        foreach (Room room in Facility.Instance.Rooms)
        {
            if (room == null || (!canEnterLockedRooms && room.IsLocked))
                continue;

            // Saat mati lampu (Blackout), Lift tidak dialiri listrik sehingga TIDAK DAPAT DILEWATI
            if (!includeDisabledLifts && isBlackout && room is Lift)
                continue;

            foreach (Bounds bounds in room.CollisionBounds)
            {
                parts.Add(new RoomPart(room, bounds));
            }
        }

        return parts;
    }

    private static bool TryFindPartAt(List<RoomPart> parts, Vector3 point, out int index)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].bounds.Contains(point))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static bool PartsAdjacent(RoomPart a, RoomPart b)
    {
        Bounds expanded = a.bounds;
        expanded.Expand(AdjacencyTolerance);
        return expanded.Intersects(b.bounds);
    }

    /// <summary>
    /// Titik tengah irisan dua Bounds. Return false kalau memang tidak overlap.
    /// </summary>
    private static bool TryGetDoorPoint(Bounds a, Bounds b, out Vector3 doorPoint)
    {
        Bounds expandedA = a;
        expandedA.Expand(AdjacencyTolerance);

        Vector3 min = Vector3.Max(expandedA.min, b.min);
        Vector3 max = Vector3.Min(expandedA.max, b.max);

        if (min.x > max.x || min.y > max.y || min.z > max.z)
        {
            doorPoint = Vector3.zero;
            return false;
        }

        doorPoint = (min + max) * 0.5f;
        doorPoint.z = 0f;
        return true;
    }

    /// <summary>
    /// Cari Room yang mengandung titik dunia tertentu. Return null kalau titik itu
    /// tidak ada di bagian manapun dari room manapun yang terdaftar (atau kalau room-nya lockdown).
    /// </summary>
    public static Room FindRoomAt(Vector3 worldPosition)
    {
        var parts = CollectParts(canEnterLockedRooms: true, includeDisabledLifts: true);
        return TryFindPartAt(parts, worldPosition, out int idx) ? parts[idx].room : null;
    }

    /// <summary>
    /// True kalau ADA SETIDAKNYA SATU pasang bagian (CollisionBounds) dari a dan b
    /// yang saling bersentuhan/overlap. Dipakai UI/debug -- pathfinding sesungguhnya
    /// (FindWaypointPath) tidak memakai fungsi ini, dia bekerja di level part sendiri.
    /// </summary>
    public static bool AreAdjacent(Room a, Room b)
    {
        if (a == null || b == null || a == b)
            return false;

        foreach (Bounds boundsA in a.CollisionBounds)
        {
            foreach (Bounds boundsB in b.CollisionBounds)
            {
                if (PartsAdjacent(new RoomPart(a, boundsA), new RoomPart(b, boundsB)))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Cari urutan titik dunia (waypoints) dari from ke to yang SELALU berada di
    /// dalam bagian room yang valid & tidak lockdown -- termasuk transit antar
    /// bagian (bar) dalam 1 room yang sama seperti MainRoom.
    ///
    /// List yang dikembalikan SUDAH termasuk titik akhir (to) sebagai elemen terakhir.
    /// Return null kalau from/to tidak ada di bagian room manapun, atau memang tidak
    /// ada jalur yang menghubungkan keduanya (mis. terhalang lockdown).
    /// </summary>
    public static List<Vector3> FindWaypointPath(Vector3 from, Vector3 to, bool canEnterLockedRooms = false)
    {
        var parts = CollectParts(canEnterLockedRooms);

        if (!TryFindPartAt(parts, from, out int startIdx))
            return null;

        if (!TryFindPartAt(parts, to, out int targetIdx))
            return null;

        if (startIdx == targetIdx)
            return new List<Vector3> { to };

        var visited = new HashSet<int> { startIdx };
        var cameFrom = new Dictionary<int, int>();
        var queue = new Queue<int>();
        queue.Enqueue(startIdx);

        bool pathFound = false;

        while (queue.Count > 0 && !pathFound)
        {
            int current = queue.Dequeue();

            for (int i = 0; i < parts.Count; i++)
            {
                if (visited.Contains(i))
                    continue;

                if (!PartsAdjacent(parts[current], parts[i]))
                    continue;

                visited.Add(i);
                cameFrom[i] = current;

                if (i == targetIdx)
                {
                    pathFound = true;
                    break;
                }

                queue.Enqueue(i);
            }
        }

        if (!pathFound)
            return null;

        // Reconstruct urutan index part.
        var indexPath = new List<int> { targetIdx };
        int cur = targetIdx;
        while (cur != startIdx)
        {
            cur = cameFrom[cur];
            indexPath.Add(cur);
        }
        indexPath.Reverse();

        // Konversi urutan part -> titik pintu antar tiap part berurutan.
        var waypoints = new List<Vector3>();
        for (int i = 0; i < indexPath.Count - 1; i++)
        {
            if (TryGetDoorPoint(parts[indexPath[i]].bounds, parts[indexPath[i + 1]].bounds, out Vector3 door))
            {
                waypoints.Add(door);
            }
            else
            {
                // Seharusnya tidak pernah kejadian (part sudah lolos PartsAdjacent).
                Debug.LogWarning("[RoomPathfinder] Gagal cari door point antar part yang seharusnya adjacent.");
            }
        }

        waypoints.Add(to);
        return waypoints;
    }
}