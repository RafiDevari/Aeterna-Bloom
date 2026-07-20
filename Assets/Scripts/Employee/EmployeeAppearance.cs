using UnityEngine;

/// <summary>
/// Handles references to individual SpriteRenderers of the employee's modular body parts
/// (body, head, hair, expression, legs, hands). Offers API methods to swap sprites dynamically
/// for customizable apparel, hairstyles, and expressions (similar to Lobotomy Corporation EGO suits).
/// </summary>
public class EmployeeAppearance : MonoBehaviour
{
    [Header("Modular Sprite Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer hairRenderer;
    [SerializeField] private SpriteRenderer expressionRenderer;
    [SerializeField] private SpriteRenderer legLRenderer;
    [SerializeField] private SpriteRenderer legRRenderer;
    [SerializeField] private SpriteRenderer handLRenderer;
    [SerializeField] private SpriteRenderer handRRenderer;

    // Public getters in case other systems need to read or manipulate them
    public SpriteRenderer BodyRenderer => bodyRenderer;
    public SpriteRenderer HeadRenderer => headRenderer;
    public SpriteRenderer HairRenderer => hairRenderer;
    public SpriteRenderer ExpressionRenderer => expressionRenderer;
    public SpriteRenderer LegLRenderer => legLRenderer;
    public SpriteRenderer LegRRenderer => legRRenderer;
    public SpriteRenderer HandLRenderer => handLRenderer;
    public SpriteRenderer HandRRenderer => handRRenderer;

    private void Awake()
    {
        ResolveRenderers();
        NotifyVisualsChanged();
    }

    /// <summary>
    /// Attempts to automatically find and assign renderers based on common naming patterns
    /// if they have not been set in the inspector.
    /// </summary>
    public void ResolveRenderers()
    {
        if (bodyRenderer == null) bodyRenderer = FindRendererByName("body");
        if (headRenderer == null) headRenderer = FindRendererByName("head");
        if (hairRenderer == null) hairRenderer = FindRendererByName("hair");
        if (expressionRenderer == null) expressionRenderer = FindRendererByName("expression") ?? FindRendererByName("face");
        if (legLRenderer == null) legLRenderer = FindRendererByName("leg_l") ?? FindRendererByName("legl") ?? FindRendererByName("leftleg") ?? FindRendererByName("leg");
        if (legRRenderer == null) legRRenderer = FindRendererByName("leg_r") ?? FindRendererByName("legr") ?? FindRendererByName("rightleg");
        if (handLRenderer == null) handLRenderer = FindRendererByName("hand_l") ?? FindRendererByName("handl") ?? FindRendererByName("lefthand") ?? FindRendererByName("hand");
        if (handRRenderer == null) handRRenderer = FindRendererByName("hand_r") ?? FindRendererByName("handr") ?? FindRendererByName("righthand");
    }

    private SpriteRenderer FindRendererByName(string namePart)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            // Avoid finding the main/root component if it's on this exact GameObject
            if (r.gameObject == gameObject && r.name.ToLower() == "employee")
                continue;

            if (r.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return r;
            }
        }
        return null;
    }

    private void NotifyVisualsChanged()
    {
        if (TryGetComponent<Employee>(out var emp))
        {
            emp.AutoFitCollider();
        }
    }

    //=========================================
    // Dynamic Customization API
    //=========================================

    /// <summary>Sets the main body/suit sprite.</summary>
    public void SetBody(Sprite bodySprite)
    {
        if (bodyRenderer != null) bodyRenderer.sprite = bodySprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets the head sprite.</summary>
    public void SetHead(Sprite headSprite)
    {
        if (headRenderer != null) headRenderer.sprite = headSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets the hair sprite and optionally tints the hair color.</summary>
    public void SetHair(Sprite hairSprite, Color? hairColor = null)
    {
        if (hairRenderer != null)
        {
            hairRenderer.sprite = hairSprite;
            if (hairColor.HasValue)
            {
                hairRenderer.color = hairColor.Value;
            }
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the facial expression / face sprite.</summary>
    public void SetExpression(Sprite expressionSprite)
    {
        if (expressionRenderer != null) expressionRenderer.sprite = expressionSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets sprites for the left and right legs.</summary>
    public void SetLegs(Sprite legLSprite, Sprite legRSprite)
    {
        if (legLRenderer != null) legLRenderer.sprite = legLSprite;
        if (legRRenderer != null) legRRenderer.sprite = legRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets sprites for the left and right hands.</summary>
    public void SetHands(Sprite handLSprite, Sprite handRSprite)
    {
        if (handLRenderer != null) handLRenderer.sprite = handLSprite;
        if (handRRenderer != null) handRRenderer.sprite = handRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>
    /// Sets a full visual set, typically representing equipping a new Suit/Armor.
    /// </summary>
    public void SetSuit(Sprite bodySprite, Sprite handLSprite, Sprite handRSprite, Sprite legLSprite, Sprite legRSprite)
    {
        SetBody(bodySprite);
        SetHands(handLSprite, handRSprite);
        SetLegs(legLSprite, legRSprite);
    }
}
