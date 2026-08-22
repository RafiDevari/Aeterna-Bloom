// Lift.cs
using UnityEngine;

public class Lift : Room
{
    private static readonly Color BlackoutColor = new Color(0.35f, 0.35f, 0.4f, 1f);

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

    private void OnEnable()
    {
        Facility.OnBlackoutStateChanged += HandleBlackoutChanged;
    }

    private void OnDisable()
    {
        Facility.OnBlackoutStateChanged -= HandleBlackoutChanged;
    }

    protected override void Start()
    {
        base.Start();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (Facility.Instance != null && Facility.Instance.IsBlackout)
            {
                spriteRenderer.color = BlackoutColor;
            }
        }
    }

    private void HandleBlackoutChanged(bool isBlackout)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isBlackout ? BlackoutColor : originalColor;
        }
    }

    public override string GetHUDInfo()
    {
        if (Facility.Instance != null && Facility.Instance.IsBlackout)
        {
            return "⚠️ LIFT OFF (Mati Lampu)";
        }
        return base.GetHUDInfo();
    }
}