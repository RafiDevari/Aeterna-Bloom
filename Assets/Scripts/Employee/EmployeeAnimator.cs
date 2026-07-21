using UnityEngine;

/// <summary>
/// Synchronizes the Employee's state changes with a Unity Animator component
/// and handles visual flipping (left/right facing) based on actual movement direction.
/// </summary>
[RequireComponent(typeof(Employee))]
public class EmployeeAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualsRoot;

    [Header("Facing Settings")]
    [Tooltip("Set to true if the sprite artwork faces left by default in its untransformed state.")]
    [SerializeField] private bool defaultFacingLeft = true;

    private Employee employee;
    private Vector3 lastPosition;
    private float originalScaleX = 1f;

    // Cache Animator parameter hashes for optimization
    private static readonly int StateHash = Animator.StringToHash("State");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsFeedingHash = Animator.StringToHash("IsFeeding");
    private static readonly int IsResearchingHash = Animator.StringToHash("IsResearching");
    private static readonly int IsHarvestingHash = Animator.StringToHash("IsHarvesting");
    private static readonly int IsTakingStockHash = Animator.StringToHash("IsTakingStock");
    private static readonly int IsFixingElectricityHash = Animator.StringToHash("IsFixingElectricity");
    private static readonly int IsConversingHash = Animator.StringToHash("IsConversing");
    private static readonly int IsConversationHash = Animator.StringToHash("IsConversation");
    private static readonly int IsHypnotizedHash = Animator.StringToHash("IsHypnotized");

    private void Awake()
    {
        employee = GetComponent<Employee>();

        // Fallback for animator
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Fallback for visuals root (checks for child named Visuals or Visual, otherwise uses this object)
        if (visualsRoot == null)
        {
            visualsRoot = transform.Find("Visuals") ?? transform.Find("Visual") ?? transform;
        }

        if (visualsRoot != null)
        {
            originalScaleX = Mathf.Abs(visualsRoot.localScale.x);
        }

        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        if (employee != null)
        {
            employee.OnStateChanged += HandleStateChanged;
            // Set initial state
            HandleStateChanged(employee.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (employee != null)
        {
            employee.OnStateChanged -= HandleStateChanged;
        }
    }

    private void Start()
    {
        // Set initial state animation
        if (employee != null)
        {
            HandleStateChanged(employee.CurrentState);
        }
    }

    private void LateUpdate()
    {
        HandleFlippingAndElevatorMovement();
    }

    /// <summary>
    /// Tracks position delta to automatically flip left/right,
    /// and pauses walking animation (switches to Idle) when riding elevators vertically.
    /// </summary>
    private void HandleFlippingAndElevatorMovement()
    {
        Vector3 currentPosition = transform.position;
        float deltaX = currentPosition.x - lastPosition.x;
        float deltaY = currentPosition.y - lastPosition.y;

        // Flip character based on horizontal movement
        if (Mathf.Abs(deltaX) > 0.005f)
        {
            bool faceLeft = deltaX < 0f;
            SetFacingLeft(faceLeft);
        }

        // If employee is in Moving state, check if movement is purely vertical (elevator/lift)
        if (employee != null && employee.CurrentState == EmployeeState.Moving)
        {
            bool isPurelyVertical = Mathf.Abs(deltaX) < 0.01f && Mathf.Abs(deltaY) > 0.01f;

            if (isPurelyVertical)
            {
                // Temporarily pause walking animation while riding elevator up/down
                if (animator != null && animator.enabled)
                {
                    animator.SetInteger(StateHash, (int)EmployeeState.Idle);
                    SetBoolIfExists(IsMovingHash, false);
                }
            }
            else if (Mathf.Abs(deltaX) >= 0.01f)
            {
                // Resume horizontal walking animation
                if (animator != null && animator.enabled)
                {
                    animator.SetInteger(StateHash, (int)EmployeeState.Moving);
                    SetBoolIfExists(IsMovingHash, true);
                }
            }
        }

        lastPosition = currentPosition;
    }

    /// <summary>
    /// Updates the localScale.x of the visuals root to flip the sprite and all child parts.
    /// </summary>
    public void SetFacingLeft(bool faceLeft)
    {
        if (visualsRoot == null) return;

        float targetScaleX = (faceLeft == defaultFacingLeft) ? originalScaleX : -originalScaleX;
        if (Mathf.Approximately(visualsRoot.localScale.x, targetScaleX))
            return;

        Vector3 scale = visualsRoot.localScale;
        scale.x = targetScaleX;
        visualsRoot.localScale = scale;

        if (employee != null)
        {
            employee.OnFacingDirectionChanged(faceLeft == defaultFacingLeft);
        }
    }

    /// <summary>
    /// Maps EmployeeState to Animator parameters.
    /// Supports both a single "State" (int) parameter and individual boolean flags.
    /// </summary>
    private void HandleStateChanged(EmployeeState state)
    {
        if (animator == null || !animator.enabled) return;

        // Set State (int)
        animator.SetInteger(StateHash, (int)state);

        // Set Boolean Flags (if they exist in the Animator Controller)
        SetBoolIfExists(IsMovingHash, state == EmployeeState.Moving);
        SetBoolIfExists(IsFeedingHash, state == EmployeeState.Feeding);
        SetBoolIfExists(IsResearchingHash, state == EmployeeState.Researching);
        SetBoolIfExists(IsHarvestingHash, state == EmployeeState.Harvesting);
        SetBoolIfExists(IsTakingStockHash, state == EmployeeState.TakingStock);
        SetBoolIfExists(IsFixingElectricityHash, state == EmployeeState.FixingElectricity);
        SetBoolIfExists(IsConversingHash, state == EmployeeState.Conversing);
        SetBoolIfExists(IsConversationHash, state == EmployeeState.Conversing);
        SetBoolIfExists(IsHypnotizedHash, state == EmployeeState.Hypnotized);
    }

    private void SetBoolIfExists(int paramHash, bool value)
    {
        if (HasParameter(paramHash))
        {
            animator.SetBool(paramHash, value);
        }
    }

    private bool HasParameter(int paramHash)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == paramHash)
            {
                return true;
            }
        }
        return false;
    }
}
