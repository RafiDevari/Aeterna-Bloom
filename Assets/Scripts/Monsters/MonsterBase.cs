using UnityEngine;

/// <summary>
/// Base class semua monster.
/// MonsterBase hanya menyediakan data dasar dan API.
/// Mekanik mood/growth sepenuhnya ditentukan oleh subclass.
/// </summary>
public class MonsterBase : MonoBehaviour
{
    //────────────────────────────────────────────────────────
    // Identity
    //────────────────────────────────────────────────────────

    [Header("Identity")]
    [SerializeField] private string monsterName = "Unknown Monster";

    //────────────────────────────────────────────────────────
    // Base Stats
    //────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [SerializeField] protected float growth = 0f;
    [SerializeField] protected int mood = 3;
    [SerializeField] protected int maxMood = 5;
    [SerializeField] protected int minMood = 0;

    //────────────────────────────────────────────────────────
    // Environment
    //────────────────────────────────────────────────────────

    [Header("Environment")]
    [SerializeField]
    protected float suitableTemperature = 20f;

    //────────────────────────────────────────────────────────
    // References
    //────────────────────────────────────────────────────────

    protected ContainmentUnit myUnit;
    protected MonsterContext context;

    //────────────────────────────────────────────────────────
    // Events
    //────────────────────────────────────────────────────────

    public System.Action<int> OnMoodChanged;
    public System.Action<float> OnGrowthChanged;

    //────────────────────────────────────────────────────────
    // Properties
    //────────────────────────────────────────────────────────

    public string MonsterName
    {
        get => monsterName;
        protected set => monsterName = value;
    }

    public float Growth
    {
        get => growth;
        protected set
        {
            growth = Mathf.Clamp01(value);
            OnGrowthChanged?.Invoke(growth);
        }
    }

    public int Mood
    {
        get => mood;
        protected set
        {
            int previous = mood;

            mood = Mathf.Clamp(value, minMood, maxMood);

            if (previous == mood)
                return;

            OnMoodChanged?.Invoke(mood);
            OnMoodChange(previous, mood);

            Debug.Log($"[{MonsterName}] Mood : {previous} -> {mood}");
        }
    }

    public float SuitableTemperature
    {
        get => suitableTemperature;
        protected set => suitableTemperature = value;
    }

    public MonsterContext Context => context;

    public ContainmentUnit Unit => myUnit;

    //────────────────────────────────────────────────────────
    // Init
    //────────────────────────────────────────────────────────

    public virtual void InitUnit(ContainmentUnit unit)
    {
        myUnit = unit;
        context = new MonsterContext(unit);
    }

    //────────────────────────────────────────────────────────
    // Unity
    //────────────────────────────────────────────────────────

    protected virtual void Update()
    {
        OnMonsterUpdate();
    }

    //────────────────────────────────────────────────────────
    // Helper
    //────────────────────────────────────────────────────────

    /// <summary>
    /// Helper timer.
    /// Return true setiap interval detik.
    /// </summary>
    protected bool Every(ref float timer, float interval)
    {
        timer += Time.deltaTime;

        if (timer < interval)
            return false;

        timer = 0f; 
        return true;
    }

    //────────────────────────────────────────────────────────
    // Virtual Hooks
    //────────────────────────────────────────────────────────

    protected virtual void OnMonsterUpdate() { }

    protected virtual void OnMoodChange(int oldMood, int newMood) { }

    //────────────────────────────────────────────────────────
    // Public API
    //────────────────────────────────────────────────────────

    public void ModifyMood(int delta)
    {
        Mood += delta;
    }

    public void SetMood(int value)
    {
        Mood = value;
    }

    public void ModifyGrowth(float delta)
    {
        Growth += delta;
    }
}