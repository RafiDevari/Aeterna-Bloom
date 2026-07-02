using UnityEngine;

/// <summary>
/// MonsterABC
/// Mekanik:
/// - Jika mood di bawah threshold,
///   Energy Facility akan berkurang setiap beberapa detik.
/// </summary>
public class MonsterABC : MonsterBase
{
    [Header("Energy Drain")]

    [Tooltip("Mood harus di bawah nilai ini agar energy mulai berkurang.")]
    [SerializeField] private int moodThresholdForDrain = 2;

    [Tooltip("Jumlah energy yang dikurangi.")]
    [SerializeField] private float energyDrainAmount = 1f;

    [Tooltip("Interval pengurangan energy.")]
    [SerializeField] private float energyDrainInterval = 5f;

    private float energyDrainTimer;

    private void Awake()
    {
        MonsterName = "MonsterABC";
    }

    protected override void OnMonsterUpdate()
    {
        if (Context == null)
            return;

        // Mood masih aman
        if (Mood >= moodThresholdForDrain)
        {
            energyDrainTimer = 0f;
            return;
        }

        // Mood rendah → kurangi energy tiap interval
        if (Every(ref energyDrainTimer, energyDrainInterval))
        {
            Context.ChangeEnergy(-energyDrainAmount);

            Debug.Log(
                $"[{MonsterName}] Energy Facility berkurang {energyDrainAmount}. " +
                $"Mood = {Mood}");
        }
    }

    protected override void OnMoodChange(int oldMood, int newMood)
    {
        if (oldMood >= moodThresholdForDrain &&
            newMood < moodThresholdForDrain)
        {
            Debug.Log($"[{MonsterName}] Energy drain dimulai.");
        }

        if (oldMood < moodThresholdForDrain &&
            newMood >= moodThresholdForDrain)
        {
            Debug.Log($"[{MonsterName}] Energy drain berhenti.");

            energyDrainTimer = 0f;
        }
    }
}