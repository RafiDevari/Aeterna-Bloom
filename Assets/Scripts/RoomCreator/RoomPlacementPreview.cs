using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component sementara untuk menangani visual preview (hijau/merah opacity) 
/// dan validasi posisi (overlap detection) serta magnet snapping MURNI RATA BAWAH (Bottom-Aligned).
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

    [Header("Preview Colors")]
    [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.6f);   // Hijau Opacity
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 0.6f); // Kemerahan Opacity

    public bool IsValid => currentValidity;

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
    /// Mengupdate posisi room preview dengan magnet snapping MURNI RATA UJUNG BAWAH SAAT DI SAMPING.
    /// Memastikan garis lantai/bawah (min.y) room baru SELALU 100% sejajar dengan room diletakkan.
    /// </summary>
    public void UpdatePositionWithSnapping(Vector3 mouseWorldPos, List<GameObject> placedRooms, bool enableRoomSnapping = true, float snapThreshold = 2.0f, bool snapGrid = false, float gridStep = 0.5f)
    {
        Vector3 targetPos = mouseWorldPos;
        targetPos.z = 0f;

        transform.position = targetPos;
        Physics2D.SyncTransforms();

        // Grid snapping opsional jika diaktifkan
        if (snapGrid && gridStep > 0.001f)
        {
            targetPos.x = Mathf.Round(targetPos.x / gridStep) * gridStep;
            targetPos.y = Mathf.Round(targetPos.y / gridStep) * gridStep;
            transform.position = targetPos;
            Physics2D.SyncTransforms();
        }

        // Magnet Snapping: HANYA RATA BAWAH SAAT MELETAKKAN DI SAMPING
        if (enableRoomSnapping && placedRooms != null && placedRooms.Count > 0)
        {
            Bounds previewBounds = GetAccurateBounds(gameObject);

            Vector3 bestSnapPos = targetPos;
            float closestDist = snapThreshold;
            bool foundSnap = false;

            foreach (GameObject placedRoom in placedRooms)
            {
                if (placedRoom == null || placedRoom == gameObject) continue;

                Bounds placedBounds = GetAccurateBounds(placedRoom);

                // =========================================================================
                // 1. SNAPPING SAMPING (KANAN & KIRI) -> MURNI RATA UJUNG BAWAH (min.y == min.y)
                // =========================================================================
                float yOffset_Bottom = placedBounds.min.y - previewBounds.min.y;

                // Samping Kanan (Ujung Kiri newRoom = Ujung Kanan placedRoom, GARIS BAWAH SEJAJAR)
                float xOffset_Right = placedBounds.max.x - previewBounds.min.x;
                Vector3 candRight = targetPos + new Vector3(xOffset_Right, yOffset_Bottom, 0f);
                CheckSnapCandidate(targetPos, candRight, ref closestDist, ref bestSnapPos, ref foundSnap);

                // Samping Kiri (Ujung Kanan newRoom = Ujung Kiri placedRoom, GARIS BAWAH SEJAJAR)
                float xOffset_Left = placedBounds.min.x - previewBounds.max.x;
                Vector3 candLeft = targetPos + new Vector3(xOffset_Left, yOffset_Bottom, 0f);
                CheckSnapCandidate(targetPos, candLeft, ref closestDist, ref bestSnapPos, ref foundSnap);

                // =========================================================================
                // 2. SNAPPING VERTIKAL (MENUMPUK DI BAWAH & DI ATAS)
                // =========================================================================
                // Menumpuk di BAWAH (Ujung Atas newRoom = Ujung Bawah placedRoom)
                float yOffset_Below = placedBounds.min.y - previewBounds.max.y;
                float xOffset_AlignLeft = placedBounds.min.x - previewBounds.min.x;
                float xOffset_AlignRight = placedBounds.max.x - previewBounds.max.x;

                Vector3 candBelowLeft = targetPos + new Vector3(xOffset_AlignLeft, yOffset_Below, 0f);
                Vector3 candBelowRight = targetPos + new Vector3(xOffset_AlignRight, yOffset_Below, 0f);
                CheckSnapCandidate(targetPos, candBelowLeft, ref closestDist, ref bestSnapPos, ref foundSnap);
                CheckSnapCandidate(targetPos, candBelowRight, ref closestDist, ref bestSnapPos, ref foundSnap);

                // Menumpuk di ATAS (Ujung Bawah newRoom = Ujung Atas placedRoom)
                float yOffset_Above = placedBounds.max.y - previewBounds.min.y;
                Vector3 candAboveLeft = targetPos + new Vector3(xOffset_AlignLeft, yOffset_Above, 0f);
                Vector3 candAboveRight = targetPos + new Vector3(xOffset_AlignRight, yOffset_Above, 0f);
                CheckSnapCandidate(targetPos, candAboveLeft, ref closestDist, ref bestSnapPos, ref foundSnap);
                CheckSnapCandidate(targetPos, candAboveRight, ref closestDist, ref bestSnapPos, ref foundSnap);
            }

            if (foundSnap)
            {
                targetPos = bestSnapPos;
            }
        }

        transform.position = targetPos;
        Physics2D.SyncTransforms();
    }

    private void CheckSnapCandidate(Vector3 targetPos, Vector3 candPos, ref float minDistance, ref Vector3 bestSnapPos, ref bool foundSnap)
    {
        float dist = Vector3.Distance(targetPos, candPos);
        if (dist < minDistance)
        {
            minDistance = dist;
            bestSnapPos = candPos;
            foundSnap = true;
        }
    }

    /// <summary>
    /// Memeriksa apakah room preview bertumpukan (overlap) dengan room lain yang sudah diletakkan.
    /// </summary>
    public bool CheckValidity(List<GameObject> placedRooms)
    {
        if (previewColliders == null || previewColliders.Length == 0)
        {
            previewColliders = GetComponentsInChildren<Collider2D>(true);
        }

        if (previewColliders == null || previewColliders.Length == 0)
        {
            currentValidity = true;
            return true;
        }

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

        currentValidity = true;
        return true;
    }

    /// <summary>
    /// Mengubah warna opacity (Hijau jika valid, Merah jika invalid/overlap).
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
