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

    private void OnMouseUp()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleClick();
    }

    private void HandleClick()
    {
        // Hanya bisa diklik saat mati lampu
        if (!Facility.Instance.IsBlackout)
        {
            Debug.Log($"[{RoomName}] Listrik stabil. Tidak perlu perbaikan.");
            return;
        }

        if (isFixing)
        {
            Debug.Log($"[{RoomName}] Perbaikan sedang dilakukan oleh employee lain.");
            return;
        }

        Debug.Log($"[{RoomName}] Membuka EmployeeSelectPopup untuk menugaskan perbaikan listrik.");

        // Membuka popup pemilihan employee
        EmployeeSelectPopup.Instance.Open(
            employee => {
                employee.GoFixElectricity(this);
            },
            typeof(DivisionEngineer) // Prioritaskan divisi Engineer jika ada
        );
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
