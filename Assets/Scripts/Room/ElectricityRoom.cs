using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class ElectricityRoom : Room
{
    [Header("Electricity Room Settings")]
    [SerializeField] private float fixDuration = 5f;

    private bool isFixing = false;

    public float FixDuration => fixDuration;
    public bool IsFixing
    {
        get => isFixing;
        set => isFixing = value;
    }

    protected override void Awake()
    {
        base.Awake();
        
        // Auto-fit BoxCollider2D size to SpriteRenderer bounds if present
        var col = GetComponent<BoxCollider2D>();
        if (col != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            col.size = spriteRenderer.sprite.bounds.size;
            col.offset = spriteRenderer.sprite.bounds.center;
        }
    }



    public override string GetHUDInfo()
    {
        if (Facility.Instance != null && Facility.Instance.IsBlackout)
        {
            return isFixing 
                ? "<color=yellow>Fixing Power...</color>" 
                : "<color=red>⚠️ POWER OFF - Click to Fix</color>";
        }
        return "Power Status: OK";
    }
}
