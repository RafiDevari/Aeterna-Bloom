// Lift.cs
using UnityEngine;

public class Lift : Room
{
    public override Bounds RoomBounds
    {
        get
        {
            if (spriteRenderer != null)
            {
                Bounds spriteBounds = spriteRenderer.bounds;

                float width = 0.5f;

                Vector3 center = spriteBounds.center;
                Vector3 size = new Vector3(width, spriteBounds.size.y, spriteBounds.size.z);

                return new Bounds(center, size);
            }

            return base.RoomBounds;
        }
    }
}