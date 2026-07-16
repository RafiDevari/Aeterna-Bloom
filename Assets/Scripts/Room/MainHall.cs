// MainRoom.cs
using UnityEngine;

/// <summary>
/// Room utama dalam fasilitas.
/// Bentuknya gabungan dua kotak seperti huruf T terbalik (⊥):
/// - Bar horizontal di lantai (lebar = sprite, tinggi = 1)
/// - Bar vertikal naik ke atas (lebar = 1, tinggi = sprite)
///
/// PENTING: CollisionBounds mengembalikan KEDUA bar secara TERPISAH (bukan 1
/// Bounds hasil Encapsulate/union). Union dari bentuk T akan mencakup 2 pojok
/// kosong di kiri-atas & kanan-atas yang secara visual bukan bagian room --
/// kalau digabung jadi 1 kotak, RoomPathfinder bisa menganggap pojok kosong itu
/// valid (employee bisa "nyasar" ke sana). Dengan CollisionBounds terpisah,
/// setiap bagian dicek independen sehingga pojok kosong itu otomatis invalid.
/// </summary>
public class MainRoom : Room
{
    private Bounds HorizontalBar
    {
        get
        {
            Bounds spriteBounds = spriteRenderer.bounds;
            float floorY = spriteBounds.min.y;
            float height = 1f;

            Vector3 center = new Vector3(
                spriteBounds.center.x,
                floorY + height * 0.5f,
                spriteBounds.center.z
            );
            Vector3 size = new Vector3(spriteBounds.size.x, height, spriteBounds.size.z);

            return new Bounds(center, size);
        }
    }

    private Bounds VerticalBar
    {
        get
        {
            Bounds spriteBounds = spriteRenderer.bounds;
            float floorY = spriteBounds.min.y;
            float topY = spriteBounds.max.y;
            float width = 1f;

            Vector3 center = new Vector3(
                spriteBounds.center.x,
                (floorY + topY) * 0.5f,
                spriteBounds.center.z
            );
            Vector3 size = new Vector3(width, topY - floorY, spriteBounds.size.z);

            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// Bagian-bagian NYATA bentuk T room ini, dipakai RoomPathfinder
    /// (FindRoomAt/AreAdjacent/TryGetDoorPoint lewat Room.Contains()).
    /// Dicek terpisah per-elemen, bukan digabung jadi 1 kotak.
    /// </summary>
    public override Bounds[] CollisionBounds
    {
        get
        {
            if (spriteRenderer == null)
                return base.CollisionBounds;

            return new Bounds[] { HorizontalBar, VerticalBar };
        }
    }
}