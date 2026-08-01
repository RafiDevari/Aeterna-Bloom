using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component sementara untuk menangani visual preview (hijau/merah opacity),
/// validasi posisi (overlap detection), aturan penempatan room (Lift & Room non-Main Hall wajib nempel).
/// </summary>
public class RoomPlacementPreview : MonoBehaviour
{
    private struct SpriteColorBackup
    {
        public SpriteRenderer renderer;
        public Color originalColor;
    }

    private List<SpriteColorBackup> spriteBackups = new List<SpriteColorBackup>();
    private Collider2D[] previewColliders;
    private bool currentValidity = true;

    private bool isCurrentlySnapped = false;
    private GameObject snappedTargetRoom = null;

    [Header("Preview Colors")]
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.6f);   // Hijau Opacity
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 0.6f); // Kemerahan Opacity

    public bool IsValid => currentValidity;
    public bool IsCurrentlySnapped => isCurrentlySnapped;
    public GameObject SnappedTargetRoom => snappedTargetRoom;

    public bool IsMainHall
    {
        get
        {
            string name = gameObject.name.ToLower();
            return GetComponent<MainRoom>() != null || name.Contains("mainhall") || name.Contains("mainroom") || name.Contains("main");
        }
    }

    public bool IsLift
    {
        get
        {
            string name = gameObject.name.ToLower();
            return GetComponent<Lift>() != null || name.Contains("lift");
        }
    }

    private void Awake()
    {
        CacheRenderersAndColliders();
    }

    public void CacheRenderersAndColliders()
    {
        spriteBackups.Clear();
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            spriteBackups.Add(new SpriteColorBackup
            {
                renderer = sr,
                originalColor = sr.color
            });
        }

        previewColliders = GetComponentsInChildren<Collider2D>(true);
    }

    /// <summary>
    /// Mengupdate posisi room preview dengan magnet snapping sesuai aturan jenis room.
    /// </summary>
    public void UpdatePositionWithSnapping(Vector3 mouseWorldPos, List<GameObject> placedRooms, bool enableRoomSnapping = true, float snapThreshold = 2.5f, bool snapGrid = false, float gridStep = 0.5f)
    {
        Vector3 targetPos = mouseWorldPos;
        targetPos.z = 0f;

        transform.position = targetPos;
        Physics2D.SyncTransforms();

        isCurrentlySnapped = false;
        snappedTargetRoom = null;

        // Grid snapping opsional jika diaktifkan
        if (snapGrid && gridStep > 0.001f)
        {
            targetPos.x = Mathf.Round(targetPos.x / gridStep) * gridStep;
            targetPos.y = Mathf.Round(targetPos.y / gridStep) * gridStep;
            transform.position = targetPos;
            Physics2D.SyncTransforms();
        }

        // Magnet Snapping
        if (enableRoomSnapping && placedRooms != null && placedRooms.Count > 0)
        {
            Bounds previewBounds = GetAccurateBounds(gameObject);

            Vector3 bestSnapPos = targetPos;
            float closestDist = snapThreshold;
            bool foundSnap = false;
            GameObject bestTarget = null;

            foreach (GameObject placedRoom in placedRooms)
            {
                if (placedRoom == null || placedRoom == gameObject) continue;

                Bounds placedBounds = GetAccurateBounds(placedRoom);

                bool targetIsMainRoom = placedRoom.GetComponent<MainRoom>() != null || placedRoom.name.ToLower().Contains("main");
                bool targetIsLift = placedRoom.GetComponent<Lift>() != null || placedRoom.name.ToLower().Contains("lift");

                // =========================================================================
                // ATURAN KHUSUS LIFT: Hanya dapat diletakkan di ATAS atau BAWAH Main Hall / Lift!
                // (TIDAK BISA di samping kiri/kanan atau di room biasa)
                // =========================================================================
                if (IsLift)
                {
                    if (!targetIsMainRoom && !targetIsLift)
                    {
                        continue; // Lift tidak boleh menempel ke room biasa (misal HallRoom / Botanist)
                    }

                    // Candidate Vertikal (BAWAH & ATAS) untuk Lift
                    float yOffset_BelowLift = placedBounds.min.y - previewBounds.max.y;
                    float yOffset_AboveLift = placedBounds.max.y - previewBounds.min.y;
                    float xOffset_AlignLeftLift = placedBounds.min.x - previewBounds.min.x;
                    float xOffset_AlignRightLift = placedBounds.max.x - previewBounds.max.x;
                    float xOffset_AlignCenterLift = placedBounds.center.x - previewBounds.center.x;

                    Vector3 cBelowLeft = targetPos + new Vector3(xOffset_AlignLeftLift, yOffset_BelowLift, 0f);
                    Vector3 cBelowRight = targetPos + new Vector3(xOffset_AlignRightLift, yOffset_BelowLift, 0f);
                    Vector3 cBelowCenter = targetPos + new Vector3(xOffset_AlignCenterLift, yOffset_BelowLift, 0f);

                    Vector3 cAboveLeft = targetPos + new Vector3(xOffset_AlignLeftLift, yOffset_AboveLift, 0f);
                    Vector3 cAboveRight = targetPos + new Vector3(xOffset_AlignRightLift, yOffset_AboveLift, 0f);
                    Vector3 cAboveCenter = targetPos + new Vector3(xOffset_AlignCenterLift, yOffset_AboveLift, 0f);

                    CheckSnapCandidateWithTarget(targetPos, cBelowLeft, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);
                    CheckSnapCandidateWithTarget(targetPos, cBelowRight, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);
                    CheckSnapCandidateWithTarget(targetPos, cBelowCenter, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);

                    CheckSnapCandidateWithTarget(targetPos, cAboveLeft, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);
                    CheckSnapCandidateWithTarget(targetPos, cAboveRight, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);
                    CheckSnapCandidateWithTarget(targetPos, cAboveCenter, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);

                    continue;
                }

                // =========================================================================
                // ATURAN ROOM BIASA (NON-LIFT): HANYA BISA DI SAMPING (Samping Kanan & Kiri, Rata Bawah)
                // (Hanya Lift yang bisa diletakkan secara vertikal)
                // =========================================================================
                float yOffset_Bottom = placedBounds.min.y - previewBounds.min.y;
                float xOffset_Right = placedBounds.max.x - previewBounds.min.x;
                Vector3 candRight = targetPos + new Vector3(xOffset_Right, yOffset_Bottom, 0f);
                CheckSnapCandidateWithTarget(targetPos, candRight, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);

                float xOffset_Left = placedBounds.min.x - previewBounds.max.x;
                Vector3 candLeft = targetPos + new Vector3(xOffset_Left, yOffset_Bottom, 0f);
                CheckSnapCandidateWithTarget(targetPos, candLeft, placedRoom, ref closestDist, ref bestSnapPos, ref bestTarget, ref foundSnap);
            }

            if (foundSnap)
            {
                targetPos = bestSnapPos;
                isCurrentlySnapped = true;
                snappedTargetRoom = bestTarget;
            }
        }

        transform.position = targetPos;
        Physics2D.SyncTransforms();
    }

    private void CheckSnapCandidateWithTarget(Vector3 targetPos, Vector3 candPos, GameObject targetRoom, ref float minDistance, ref Vector3 bestSnapPos, ref GameObject bestTargetRoom, ref bool foundSnap)
    {
        float dist = Vector3.Distance(targetPos, candPos);
        if (dist < minDistance)
        {
            minDistance = dist;
            bestSnapPos = candPos;
            bestTargetRoom = targetRoom;
            foundSnap = true;
        }
    }

    /// <summary>
    /// Memeriksa apakah room preview valid untuk diletakkan.
    /// Memeriksa aturan keterikatan room non-MainHall, aturan Lift, dan overlap detection.
    /// </summary>
    public bool CheckValidity(List<GameObject> placedRooms)
    {
        // ATURAN 1: Room selain Main Hall TIDAK BOLEH melayang bebas jika sudah ada room di scene!
        // Wajib tersnapped / nempel ke room lain.
        if (placedRooms != null && placedRooms.Count > 0 && !IsMainHall)
        {
            if (!isCurrentlySnapped || snappedTargetRoom == null)
            {
                currentValidity = false;
                return false;
            }
        }

        // ATURAN 2: Khusus Lift, WAJIB tersnapped vertikal ke Main Hall atau Lift lain!
        if (IsLift)
        {
            if (!isCurrentlySnapped || snappedTargetRoom == null)
            {
                currentValidity = false;
                return false;
            }

            bool targetIsMainRoom = snappedTargetRoom.GetComponent<MainRoom>() != null || snappedTargetRoom.name.ToLower().Contains("main");
            bool targetIsLift = snappedTargetRoom.GetComponent<Lift>() != null || snappedTargetRoom.name.ToLower().Contains("lift");
            if (!targetIsMainRoom && !targetIsLift)
            {
                currentValidity = false;
                return false;
            }
        }

        // Check overlap bertumpukan dengan room lain
        if (previewColliders == null || previewColliders.Length == 0)
        {
            previewColliders = GetComponentsInChildren<Collider2D>(true);
        }

        if (previewColliders != null && previewColliders.Length > 0)
        {
            foreach (GameObject placedRoom in placedRooms)
            {
                if (placedRoom == null || placedRoom == gameObject) continue;

                Collider2D[] placedColliders = placedRoom.GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D pCol in previewColliders)
                {
                    if (pCol == null || !pCol.enabled) continue;

                    Bounds pBounds = pCol.bounds;
                    // Shrink bounds sedikit agar peletakan bersebelahan pas di garis tidak dianggap overlap
                    pBounds.Expand(new Vector3(-0.1f, -0.1f, 0f));

                    foreach (Collider2D oCol in placedColliders)
                    {
                        if (oCol == null || !oCol.enabled) continue;

                        if (pBounds.Intersects(oCol.bounds))
                        {
                            currentValidity = false;
                            return false;
                        }
                    }
                }
            }
        }

        currentValidity = true;
        return true;
    }

    /// <summary>
    /// Mengubah warna opacity (Hijau jika valid, Merah jika invalid/overlap/belum nempel).
    /// </summary>
    public void SetPreviewState(bool isValid)
    {
        currentValidity = isValid;
        Color targetColor = isValid ? validColor : invalidColor;

        foreach (var backup in spriteBackups)
        {
            if (backup.renderer != null)
            {
                backup.renderer.color = targetColor;
            }
        }
    }

    /// <summary>
    /// Mengembalikan warna original sprite dan menyelesaikan penempatan room.
    /// </summary>
    public void ConfirmPlacement()
    {
        foreach (var backup in spriteBackups)
        {
            if (backup.renderer != null)
            {
                backup.renderer.color = backup.originalColor;
            }
        }

        // Hapus script preview agar room menjadi room permanen
        Destroy(this);
    }

    /// <summary>
    /// Membatalkan penempatan room dan menghapus preview object.
    /// </summary>
    public void CancelPlacement()
    {
        Destroy(gameObject);
    }

    public static Bounds GetAccurateBounds(GameObject obj)
    {
        Physics2D.SyncTransforms();

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
            }
            return b;
        }

        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
        if (colliders != null && colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled)
                {
                    b.Encapsulate(colliders[i].bounds);
                }
            }
            return b;
        }

        return new Bounds(obj.transform.position, Vector3.one);
    }
}
