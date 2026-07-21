using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sunflower (Tanaman Mekanik): Turunan dari MonsterBase.
/// - Menyedot energy dari facility tiap kali mood turun.
/// - Energy disedot dan disimpan ke variabel storedEnergy.
/// - Jika storedEnergy > 20, memanggil fungsi meledak Explode().
/// - Explode() meledakkan room tempat dia ada dan memberikan damage 30
///   kepada siapapun (Employee) yang berada di area ledakan/ruangan tersebut.
/// </summary>
public class SunflowerMonster : MonsterBase
{
    [Header("Sunflower Settings")]
    [Tooltip("Jumlah energy yang disedot dari Facility setiap 1 poin mood turun.")]
    [SerializeField] private float energyPerMoodDrop = 5f;

    [Tooltip("Batas akumulasi energy sebelum Sunflower meledak.")]
    [SerializeField] private float explosionThreshold = 20f;

    [Tooltip("Damage yang dihasilkan ledakan kepada Employee di dalam area/ruangan.")]
    [SerializeField] private int explosionDamage = 30;

    [Tooltip("Radius ledakan tambahan jika diukur dari posisi monster.")]
    [SerializeField] private float explosionRadius = 5f;

    [Header("Energy Storage Status")]
    [SerializeField] private float storedEnergy = 0f;

    public float StoredEnergy => storedEnergy;
    public float ExplosionThreshold => explosionThreshold;

    protected override void Awake()
    {
        base.Awake();
        MonsterName = "Sunflower";
    }

    /// <summary>
    /// Dipanggil setiap kali Mood berubah.
    /// Jika Mood turun (newMood < oldMood), menyedot energy dari Facility.
    /// </summary>
    protected override void OnMoodChange(int oldMood, int newMood)
    {
        base.OnMoodChange(oldMood, newMood);

        if (newMood < oldMood)
        {
            int moodDecrease = oldMood - newMood;
            float energyToSiphon = energyPerMoodDrop * moodDecrease;

            SiphonEnergy(energyToSiphon);
        }
    }

    /// <summary>
    /// Menyedot energi dari facility dan menyimpannya ke storedEnergy.
    /// </summary>
    /// <param name="amount">Jumlah energi yang disedot.</param>
    public void SiphonEnergy(float amount)
    {
        if (amount <= 0f) return;

        // Kurangi energy dari facility (jika ada Context & Facility)
        if (Context != null && Context.Facility != null)
        {
            Context.ChangeEnergy(-amount);
        }
        else if (Facility.Instance != null)
        {
            Facility.Instance.Energy -= amount;
        }

        storedEnergy += amount;
        Debug.Log($"[{MonsterName}] Menyedot {amount} energy dari Facility! Stored Energy: {storedEnergy}/{explosionThreshold}");

        // Cek jika storedEnergy > explosionThreshold (20)
        if (storedEnergy > explosionThreshold)
        {
            Explode();
        }
    }

    /// <summary>
    /// Fungsi unik meledakkan room tempat monster ini berada
    /// dan memberikan damage 30 ke siapapun yang berada di area tersebut.
    /// </summary>
    public void Explode()
    {
        Debug.LogWarning($"[{MonsterName}] MELEDAK! (Stored Energy: {storedEnergy} > {explosionThreshold})");

        Room currentRoom = Context != null ? Context.CurrentRoom : null;

        // Cari semua Employee di Facility
        if (Facility.Instance != null && Facility.Instance.Employees != null)
        {
            // Buat salinan daftar employee untuk me-loop secara aman
            var employees = new List<Employee>(Facility.Instance.Employees);

            foreach (var employee in employees)
            {
                if (employee == null || employee.CurrentState == EmployeeState.Dead)
                    continue;

                bool inArea = false;

                // Cek 1: Apakah employee berada di dalam room tempat monster berada?
                if (currentRoom != null && currentRoom.Contains(employee.transform.position))
                {
                    inArea = true;
                }

                // Cek 2: Apakah employee berada dalam radius ledakan fisik?
                if (!inArea)
                {
                    float dist = Vector3.Distance(transform.position, employee.transform.position);
                    if (dist <= explosionRadius)
                    {
                        inArea = true;
                    }
                }

                if (inArea)
                {
                    Debug.Log($"[{MonsterName}] Ledakan mengenai {employee.EmployeeName}! Melukai sebesar {explosionDamage} damage.");
                    employee.ModifyHp(-explosionDamage);
                }
            }
        }

        // Reset stored energy setelah meledak
        storedEnergy = 0f;
    }
}
