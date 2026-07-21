using UnityEngine;

/// <summary>
/// Handles references to individual SpriteRenderers of the employee's modular body parts:
/// - Face 3 Parts: Eye, Nose, Mouth (Expression renderer has been removed)
/// - Body: Main Body, Left Arm, Right Arm
/// - Hands: Left Hand, Right Hand (preserved)
/// - Legs: Left Leg, Right Leg
/// - Head & Hair
/// Customizable appearance inputs in Inspector are configured for Body, Hair, Eyes, and Mouth.
/// </summary>
public class EmployeeAppearance : MonoBehaviour
{
    [Header("Modular Body & Limb Renderers")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer armLRenderer;
    [SerializeField] private SpriteRenderer armRRenderer;
    [SerializeField] private SpriteRenderer handLRenderer;
    [SerializeField] private SpriteRenderer handRRenderer;
    [SerializeField] private SpriteRenderer legLRenderer;
    [SerializeField] private SpriteRenderer upperLegLRenderer;
    [SerializeField] private SpriteRenderer legRRenderer;
    [SerializeField] private SpriteRenderer upperLegRRenderer;

    [Header("Modular Head & Facial Renderers")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer hairRenderer;
    [SerializeField] private SpriteRenderer eyeRenderer;
    [SerializeField] private SpriteRenderer noseRenderer;
    [SerializeField] private SpriteRenderer mouthRenderer;

    [Header("Customizable Appearance Inputs")]
    [Tooltip("Optional sprite override for employee main body texture.")]
    [SerializeField] private Sprite bodySprite;
    [Tooltip("Color tint for employee body (default White = un-tinted original image color).")]
    [SerializeField] private Color bodyColor = Color.white;
    [Tooltip("Optional sprite override for employee hair texture.")]
    [SerializeField] private Sprite hairSprite;
    [Tooltip("Color tint for employee hair (default White = un-tinted original image color).")]
    [SerializeField] private Color hairColor = Color.white;
    [Tooltip("Optional sprite override for employee eye texture.")]
    [SerializeField] private Sprite eyeSprite;
    [Tooltip("Optional sprite override for employee mouth texture.")]
    [SerializeField] private Sprite mouthSprite;

    [Header("Shader Tint Settings")]
    [Tooltip("If enabled, automatically applies the Custom/SpriteWhiteTint shader to body & arms so only white shirt areas change color while keeping skin and neck untouched.")]
    [SerializeField] private bool useWhiteOnlyTint = true;
    [Tooltip("Optional override material with Custom/SpriteWhiteTint shader.")]
    [SerializeField] private Material whiteTintMaterial;

#if UNITY_EDITOR
    [Header("Folder Auto-Fill (Drag & Drop Folder)")]
    [Tooltip("Drag a sprite folder from the Project window here to automatically populate body, left arm, and right arm (and limbs).")]
    [SerializeField] private UnityEngine.Object suitFolder;
#endif
    [Tooltip("Optional relative Resources folder path (e.g. 'Suits/Suit01') to auto-load body and arm sprites at runtime.")]
    [SerializeField] private string resourcesFolderPath;

    // Public getters for individual SpriteRenderers
    public SpriteRenderer BodyRenderer => bodyRenderer;
    public SpriteRenderer ArmLRenderer => armLRenderer;
    public SpriteRenderer ArmRRenderer => armRRenderer;
    public SpriteRenderer HandLRenderer => handLRenderer;
    public SpriteRenderer HandRRenderer => handRRenderer;
    public SpriteRenderer LegLRenderer => legLRenderer;
    public SpriteRenderer UpperLegLRenderer => upperLegLRenderer;
    public SpriteRenderer LegRRenderer => legRRenderer;
    public SpriteRenderer UpperLegRRenderer => upperLegRRenderer;
    public SpriteRenderer HeadRenderer => headRenderer;
    public SpriteRenderer HairRenderer => hairRenderer;
    public SpriteRenderer EyeRenderer => eyeRenderer;
    public SpriteRenderer NoseRenderer => noseRenderer;
    public SpriteRenderer MouthRenderer => mouthRenderer;

    // Public getters and setters for Inspector properties
    public Sprite BodySprite
    {
        get => bodySprite;
        set => SetBody(value);
    }

    public Color BodyColor
    {
        get => bodyColor;
        set => SetBodyColor(value);
    }

    public Sprite HairSprite
    {
        get => hairSprite;
        set => SetHair(value, hairColor);
    }

    public Color HairColor
    {
        get => hairColor;
        set => SetHairColor(value);
    }

    public Sprite EyeSprite
    {
        get => eyeSprite;
        set => SetEye(value);
    }

    public Sprite MouthSprite
    {
        get => mouthSprite;
        set => SetMouth(value);
    }

    private void Awake()
    {
        ResolveRenderers();
        AutoFillFromFolder();
        ApplyCustomInputs();
        NotifyVisualsChanged();
    }

    private void OnValidate()
    {
        ResolveRenderers();
#if UNITY_EDITOR
        AutoFillFromFolder();
#endif
        ApplyCustomInputs();
    }

    /// <summary>
    /// Automatically scans the assigned suitFolder or resourcesFolderPath
    /// and populates body, left arm, and right arm (and optional limbs).
    /// </summary>
    [ContextMenu("Auto-Fill Body & Arms From Folder")]
    public void AutoFillFromFolder()
    {
#if UNITY_EDITOR
        if (suitFolder != null)
        {
            string folderPath = UnityEditor.AssetDatabase.GetAssetPath(suitFolder);
            if (System.IO.Directory.Exists(folderPath))
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;

                    AssignSpriteByName(sprite);
                }
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
        }
#endif

        if (!string.IsNullOrEmpty(resourcesFolderPath))
        {
            LoadBodyAndArmsFromResources(resourcesFolderPath);
        }
    }

    private void AssignSpriteByName(Sprite sprite)
    {
        if (sprite == null) return;
        string name = sprite.name.ToLower();

        if (name.Contains("body"))
        {
            bodySprite = sprite;
            if (bodyRenderer != null) bodyRenderer.sprite = sprite;
        }
        else if (name.Contains("arm_l") || name.Contains("arml") || name.Contains("leftarm") || name.Contains("arm_left") || (name.Contains("arm") && name.Contains("l")))
        {
            if (armLRenderer != null) armLRenderer.sprite = sprite;
        }
        else if (name.Contains("arm_r") || name.Contains("armr") || name.Contains("rightarm") || name.Contains("arm_right") || (name.Contains("arm") && name.Contains("r")))
        {
            if (armRRenderer != null) armRRenderer.sprite = sprite;
        }
        else if (name.Contains("hand_l") || name.Contains("handl") || name.Contains("lefthand"))
        {
            if (handLRenderer != null) handLRenderer.sprite = sprite;
        }
        else if (name.Contains("hand_r") || name.Contains("handr") || name.Contains("righthand"))
        {
            if (handRRenderer != null) handRRenderer.sprite = sprite;
        }
        else if (name.Contains("upperleg_l") || name.Contains("upperlegl") || name.Contains("upper_leg_l"))
        {
            if (upperLegLRenderer != null) upperLegLRenderer.sprite = sprite;
        }
        else if (name.Contains("leg_l") || name.Contains("legl") || name.Contains("leftleg"))
        {
            if (legLRenderer != null) legLRenderer.sprite = sprite;
        }
        else if (name.Contains("upperleg_r") || name.Contains("upperlegr") || name.Contains("upper_leg_r"))
        {
            if (upperLegRRenderer != null) upperLegRRenderer.sprite = sprite;
        }
        else if (name.Contains("leg_r") || name.Contains("legr") || name.Contains("rightleg"))
        {
            if (legRRenderer != null) legRRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// Loads body, left arm, and right arm sprites from a Resources folder path (e.g. "Suits/Suit01").
    /// </summary>
    public void LoadBodyAndArmsFromResources(string folderPath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(folderPath);
        if (sprites == null || sprites.Length == 0) return;

        foreach (var sprite in sprites)
        {
            AssignSpriteByName(sprite);
        }
        NotifyVisualsChanged();
    }

    /// <summary>
    /// Applies customizable inputs (Body, Hair, Eyes, Mouth) and colors if set.
    /// </summary>
    public void ApplyCustomInputs()
    {
        if (bodyRenderer != null)
        {
            EnsureWhiteTintShader(bodyRenderer);
            if (bodySprite != null) bodyRenderer.sprite = bodySprite;
            bodyRenderer.color = bodyColor;
        }

        if (armLRenderer != null)
        {
            EnsureWhiteTintShader(armLRenderer);
        }

        if (armRRenderer != null)
        {
            EnsureWhiteTintShader(armRRenderer);
        }

        if (hairRenderer != null)
        {
            if (hairSprite != null) hairRenderer.sprite = hairSprite;
            hairRenderer.color = hairColor;
        }

        if (eyeRenderer != null && eyeSprite != null) eyeRenderer.sprite = eyeSprite;
        if (mouthRenderer != null && mouthSprite != null) mouthRenderer.sprite = mouthSprite;
    }

    private void EnsureWhiteTintShader(SpriteRenderer renderer)
    {
        if (renderer == null || !useWhiteOnlyTint) return;

        Shader whiteShader = Shader.Find("Custom/SpriteWhiteTint");
        if (whiteShader == null) return;

        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != whiteShader)
        {
            if (whiteTintMaterial != null && whiteTintMaterial.shader == whiteShader)
            {
                renderer.sharedMaterial = whiteTintMaterial;
            }
            else
            {
                Material newMat = new Material(whiteShader);
                newMat.name = "SpriteWhiteTint_Instance";
                renderer.material = newMat;
            }
        }
    }

    /// <summary>
    /// Attempts to automatically find and assign renderers based on common naming patterns
    /// if they have not been set in the inspector.
    /// </summary>
    public void ResolveRenderers()
    {
        if (bodyRenderer == null) bodyRenderer = FindRendererByName("body");
        if (armLRenderer == null) armLRenderer = FindRendererByName("arm_l", "arml", "leftarm");
        if (armRRenderer == null) armRRenderer = FindRendererByName("arm_r", "armr", "rightarm");
        if (handLRenderer == null) handLRenderer = FindRendererByName("hand_l", "handl", "lefthand");
        if (handRRenderer == null) handRRenderer = FindRendererByName("hand_r", "handr", "righthand");

        if (upperLegLRenderer == null) upperLegLRenderer = FindRendererByName("upperleg_l", "upperlegl", "upper_leg_l", "upperleftleg");
        if (legLRenderer == null) legLRenderer = FindRendererByName("leg_l", "legl", "leftleg", "lowerleg_l", "lowerlegl");
        if (upperLegRRenderer == null) upperLegRRenderer = FindRendererByName("upperleg_r", "upperlegr", "upper_leg_r", "upperrightleg");
        if (legRRenderer == null) legRRenderer = FindRendererByName("leg_r", "legr", "rightleg", "lowerleg_r", "lowerlegr");

        if (headRenderer == null) headRenderer = FindRendererByName("head");
        if (hairRenderer == null) hairRenderer = FindRendererByName("hair");
        if (eyeRenderer == null) eyeRenderer = FindRendererByName("eye", "eyes");
        if (noseRenderer == null) noseRenderer = FindRendererByName("nose");
        if (mouthRenderer == null) mouthRenderer = FindRendererByName("mouth");
    }

    private SpriteRenderer FindRendererByName(params string[] nameParts)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var namePart in nameParts)
        {
            foreach (var r in renderers)
            {
                if (r.gameObject == gameObject && r.name.ToLower() == "employee")
                    continue;

                if (r.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return r;
                }
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
    // Dynamic Customization API - Body & Arms
    //=========================================

    /// <summary>Sets the main body sprite.</summary>
    public void SetBody(Sprite newBodySprite)
    {
        bodySprite = newBodySprite;
        if (bodyRenderer != null && bodySprite != null)
        {
            bodyRenderer.sprite = bodySprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the body color tint.</summary>
    public void SetBodyColor(Color color)
    {
        bodyColor = color;
        if (bodyRenderer != null)
        {
            bodyRenderer.color = bodyColor;
        }
    }

    /// <summary>Sets the left arm sprite.</summary>
    public void SetArmL(Sprite newArmLSprite)
    {
        if (armLRenderer != null && newArmLSprite != null)
        {
            armLRenderer.sprite = newArmLSprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the right arm sprite.</summary>
    public void SetArmR(Sprite newArmRSprite)
    {
        if (armRRenderer != null && newArmRSprite != null)
        {
            armRRenderer.sprite = newArmRSprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets sprites for left and right arms.</summary>
    public void SetArms(Sprite newArmLSprite, Sprite newArmRSprite)
    {
        if (armLRenderer != null && newArmLSprite != null) armLRenderer.sprite = newArmLSprite;
        if (armRRenderer != null && newArmRSprite != null) armRRenderer.sprite = newArmRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets main body sprite and arm sprites simultaneously.</summary>
    public void SetBodyAndArms(Sprite newBodySprite, Sprite newArmLSprite, Sprite newArmRSprite)
    {
        SetBody(newBodySprite);
        SetArms(newArmLSprite, newArmRSprite);
    }

    //=========================================
    // Dynamic Customization API - Facial Parts
    //=========================================

    /// <summary>Sets the eye sprite.</summary>
    public void SetEye(Sprite newEyeSprite)
    {
        eyeSprite = newEyeSprite;
        if (eyeRenderer != null && eyeSprite != null)
        {
            eyeRenderer.sprite = eyeSprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the nose sprite.</summary>
    public void SetNose(Sprite newNoseSprite)
    {
        if (noseRenderer != null && newNoseSprite != null)
        {
            noseRenderer.sprite = newNoseSprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the mouth sprite.</summary>
    public void SetMouth(Sprite newMouthSprite)
    {
        mouthSprite = newMouthSprite;
        if (mouthRenderer != null && mouthSprite != null)
        {
            mouthRenderer.sprite = mouthSprite;
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets eye, nose, and mouth sprites simultaneously for facial parts.</summary>
    public void SetFaceParts(Sprite newEyeSprite, Sprite newNoseSprite, Sprite newMouthSprite)
    {
        SetEye(newEyeSprite);
        SetNose(newNoseSprite);
        SetMouth(newMouthSprite);
    }

    //=========================================
    // Dynamic Customization API - Head & Hair
    //=========================================

    /// <summary>Sets the head sprite.</summary>
    public void SetHead(Sprite headSprite)
    {
        if (headRenderer != null) headRenderer.sprite = headSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets the hair sprite and optionally tints the hair color.</summary>
    public void SetHair(Sprite newHairSprite, Color? newHairColor = null)
    {
        hairSprite = newHairSprite;
        if (hairRenderer != null)
        {
            if (hairSprite != null)
            {
                hairRenderer.sprite = hairSprite;
            }
            if (newHairColor.HasValue)
            {
                hairColor = newHairColor.Value;
                hairRenderer.color = hairColor;
            }
        }
        NotifyVisualsChanged();
    }

    /// <summary>Sets the hair color tint.</summary>
    public void SetHairColor(Color color)
    {
        hairColor = color;
        if (hairRenderer != null)
        {
            hairRenderer.color = hairColor;
        }
    }

    //=========================================
    // Dynamic Customization API - Limbs & Suits
    //=========================================

    /// <summary>Sets sprites for the lower left and lower right legs.</summary>
    public void SetLegs(Sprite legLSprite, Sprite legRSprite)
    {
        if (legLRenderer != null && legLSprite != null) legLRenderer.sprite = legLSprite;
        if (legRRenderer != null && legRSprite != null) legRRenderer.sprite = legRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets sprites for all 4 leg parts: leg left, upper leg left, leg right, upper leg right.</summary>
    public void SetLegs(Sprite legLSprite, Sprite upperLegLSprite, Sprite legRSprite, Sprite upperLegRSprite)
    {
        if (legLRenderer != null && legLSprite != null) legLRenderer.sprite = legLSprite;
        if (upperLegLRenderer != null && upperLegLSprite != null) upperLegLRenderer.sprite = upperLegLSprite;
        if (legRRenderer != null && legRSprite != null) legRRenderer.sprite = legRSprite;
        if (upperLegRRenderer != null && upperLegRSprite != null) upperLegRRenderer.sprite = upperLegRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets individual left leg sprites.</summary>
    public void SetLegL(Sprite legLSprite, Sprite upperLegLSprite = null)
    {
        if (legLRenderer != null && legLSprite != null) legLRenderer.sprite = legLSprite;
        if (upperLegLRenderer != null && upperLegLSprite != null) upperLegLRenderer.sprite = upperLegLSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets individual right leg sprites.</summary>
    public void SetLegR(Sprite legRSprite, Sprite upperLegRSprite = null)
    {
        if (legRRenderer != null && legRSprite != null) legRRenderer.sprite = legRSprite;
        if (upperLegRRenderer != null && upperLegRSprite != null) upperLegRRenderer.sprite = upperLegRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>Sets sprites for the left and right hands.</summary>
    public void SetHands(Sprite handLSprite, Sprite handRSprite)
    {
        if (handLRenderer != null && handLSprite != null) handLRenderer.sprite = handLSprite;
        if (handRRenderer != null && handRSprite != null) handRRenderer.sprite = handRSprite;
        NotifyVisualsChanged();
    }

    /// <summary>
    /// Sets a full visual suit set including body, arms, hands, lower legs, and upper legs.
    /// </summary>
    public void SetSuit(Sprite bodySprite, Sprite armLSprite, Sprite armRSprite, Sprite handLSprite, Sprite handRSprite, Sprite legLSprite, Sprite upperLegLSprite, Sprite legRSprite, Sprite upperLegRSprite)
    {
        SetBodyAndArms(bodySprite, armLSprite, armRSprite);
        SetHands(handLSprite, handRSprite);
        SetLegs(legLSprite, upperLegLSprite, legRSprite, upperLegRSprite);
    }

    /// <summary>
    /// Sets a full visual suit set including body, arms, hands, and legs.
    /// </summary>
    public void SetSuit(Sprite bodySprite, Sprite armLSprite, Sprite armRSprite, Sprite handLSprite, Sprite handRSprite, Sprite legLSprite, Sprite legRSprite)
    {
        SetBodyAndArms(bodySprite, armLSprite, armRSprite);
        SetHands(handLSprite, handRSprite);
        SetLegs(legLSprite, legRSprite);
    }

    /// <summary>
    /// SetSuit overload without separate arm parameters.
    /// </summary>
    public void SetSuit(Sprite bodySprite, Sprite handLSprite, Sprite handRSprite, Sprite legLSprite, Sprite legRSprite)
    {
        SetBody(bodySprite);
        SetHands(handLSprite, handRSprite);
        SetLegs(legLSprite, legRSprite);
    }
}
