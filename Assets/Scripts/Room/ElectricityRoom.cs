using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class ElectricityRoom : Room
{
    [Header("Electricity Room Settings")]
    [SerializeField] private float fixDuration = 5f;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

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
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        // Auto-fit BoxCollider2D size to SpriteRenderer bounds if present
        var col = GetComponent<BoxCollider2D>();
        if (col != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            col.size = spriteRenderer.sprite.bounds.size;
            col.offset = spriteRenderer.sprite.bounds.center;
        }
    }

    /// <summary>
    /// Triggers the "Shock" Animator parameter during electricity overload / blackout.
    /// In Animator Controller, Shock automatically transitions to the looping Kebakar state upon finishing.
    /// </summary>
    public void TriggerShock()
    {
        isFixing = false;
        if (animator != null)
        {
            animator.SetTrigger("Shock");
        }
        else
        {
            TriggerEffect(AeternaBloom.Effects.Room.RoomEffectPaths.ElectricShock);
        }
    }

    /// <summary>
    /// Resets the electricity room animation state back to normal when blackout is resolved.
    /// </summary>
    public void ResetPower()
    {
        isFixing = false;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
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
