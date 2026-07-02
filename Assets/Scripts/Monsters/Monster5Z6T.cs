// using UnityEngine;

// /// <summary>
// /// Monster5Z6T: Turunan dari MonsterBase.
// /// Mekanik utama: Jika mood &gt; 4, secara berkala mengubah suhu GLOBAL Facility.
// /// Mekanik mood decay: Override ApplyMoodDecay dengan kondisi khusus sendiri.
// /// </summary>
// public class Monster5Z6T : MonsterBase
// {
//     [Header("Monster5Z6T - Global Temperature")]
//     [Tooltip("Threshold mood untuk mulai mempengaruhi suhu global")]
//     [SerializeField] private int moodThresholdForTemp = 4;
//     [Tooltip("Berapa banyak suhu global berubah per interval")]
//     [SerializeField] private float globalTempChangeAmount = 1.5f;
//     [Tooltip("Interval perubahan suhu global (detik)")]
//     [SerializeField] private float globalTempInterval = 8f;
//     [Tooltip("Apakah suhu naik (true) atau turun (false)?")]
//     [SerializeField] private bool increasesTemp = true;

//     [Header("Monster5Z6T - Custom Mood Decay")]
//     [Tooltip("Decay lebih cepat jika suhu global terlalu tinggi (threshold)")]
//     [SerializeField] private float highTempThreshold = 40f;
//     [Tooltip("Multiplier decay saat kondisi khusus aktif")]
//     [SerializeField] private int fastDecayMultiplier = 2;

//     private float globalTempTimer = 0f;

//     private void Awake()
//     {
//         if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "Unknown Monster")
//             gameObject.name = "Monster5Z6T";

//         // Override mood decay settings dari base
//         moodDecayInterval = 20f;    // lebih lambat dari default
//         moodDecayAmount = 1;
//         moodDecayEnabled = true;
//     }

//     protected override void OnMonsterUpdate()
//     {
//         // Hanya aktif jika mood > threshold
//         if (Mood > moodThresholdForTemp)
//         {
//             globalTempTimer += Time.deltaTime;
//             if (globalTempTimer >= globalTempInterval)
//             {
//                 globalTempTimer = 0f;
//                 AffectGlobalTemperature();
//             }
//         }
//         else
//         {
//             globalTempTimer = 0f;
//         }
//     }

//     private void AffectGlobalTemperature()
//     {
//         var fac = GetFacility();
//         if (fac == null) return;

//         float delta = increasesTemp ? globalTempChangeAmount : -globalTempChangeAmount;
//         fac.Temperature += delta;

//         Debug.Log($"[Monster5Z6T] Mood tinggi ({Mood} > {moodThresholdForTemp})! " +
//                   $"Suhu global Facility {(increasesTemp ? "naik" : "turun")} {Mathf.Abs(delta):F1}°C. " +
//                   $"Suhu global sekarang: {fac.Temperature:F1}°C");
//     }

//     /// <summary>
//     /// Override mood decay: Jika suhu global sangat tinggi,
//     /// mood turun lebih cepat (x fastDecayMultiplier).
//     /// </summary>
//     protected override void ApplyMoodDecay()
//     {
//         var fac = GetFacility();

//         if (fac != null && fac.Temperature >= highTempThreshold)
//         {
//             // Kondisi khusus: suhu global terlalu tinggi → mood turun lebih cepat
//             int decayAmount = moodDecayAmount * fastDecayMultiplier;
//             Mood -= decayAmount;
//             Debug.Log($"[Monster5Z6T] Suhu global tinggi ({fac.Temperature:F1}°C >= {highTempThreshold}°C)! " +
//                       $"Mood turun CEPAT: -{decayAmount}");
//         }
//         else
//         {
//             // Decay normal
//             Mood -= moodDecayAmount;
//         }
//     }

//     protected override void OnMoodChange(int oldMood, int newMood)
//     {
//         if (newMood > moodThresholdForTemp && oldMood <= moodThresholdForTemp)
//         {
//             Debug.Log($"[Monster5Z6T] Mood melewati threshold {moodThresholdForTemp}! " +
//                       $"Mulai mempengaruhi suhu global.");
//         }

//         if (newMood <= moodThresholdForTemp && oldMood > moodThresholdForTemp)
//         {
//             Debug.Log($"[Monster5Z6T] Mood turun di bawah threshold. " +
//                       $"Pengaruh suhu global berhenti.");
//         }
//     }
// }
