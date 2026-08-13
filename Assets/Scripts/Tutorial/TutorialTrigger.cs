using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Trigger untuk scene Tutorial.
/// Memunculkan broadcast tutorial saat scene pertama kali dibuka/dijalankan.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [Header("Broadcast Settings")]
    [Tooltip("Jika true, broadcast akan otomatis ter-trigger saat scene berjalan.")]
    [SerializeField] private bool triggerOnStart = true;
    
    [Tooltip("Waktu jeda (dalam detik) sebelum broadcast pertama muncul saat Start.")]
    [SerializeField] private float initialDelay = 0.5f;
    
    [Tooltip("Nama pengirim broadcast.")]
    [SerializeField] private string senderName = "PT SAWIT";
    
    [Tooltip("Pesan broadcast utama yang akan ditampilkan.")]
    [TextArea(2, 5)]
    [SerializeField] private string initialMessage = "SELAMAT DATANG DI PT SAWIT, SAYA AKAN MEMBERI ANDA TUTORIAL.";
    
    [Tooltip("Durasi tampilnya broadcast di layar (detik).")]
    [SerializeField] private float broadcastDuration = 10f;

    [Header("Optional Sequence Broadcasts")]
    [Tooltip("Broadcast lanjutan jika tutorial memiliki beberapa tahapan pesan.")]
    [SerializeField] private List<TutorialSequenceItem> additionalMessages = new List<TutorialSequenceItem>();

    [System.Serializable]
    public class TutorialSequenceItem
    {
        public string sender = "PT SAWIT";
        [TextArea(2, 4)]
        public string message;
        public float delayBefore = 3f;
        public float duration = 10f;
    }

    private void Start()
    {
        if (triggerOnStart)
        {
            TriggerTutorialBroadcast();
        }
    }

    /// <summary>
    /// Panggil fungsi ini untuk memicu broadcast tutorial secara manual dari script/event/button.
    /// </summary>
    public void TriggerTutorialBroadcast()
    {
        StartCoroutine(RoutinePlayBroadcasts());
    }

    private IEnumerator RoutinePlayBroadcasts()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // Pastikan FacilityHUD tersedia
        FacilityHUD hud = FacilityHUD.Instance;

        // Tampilkan broadcast utama
        if (!string.IsNullOrEmpty(initialMessage))
        {
            FacilityHUD.ShowBroadcast(initialMessage, senderName, broadcastDuration);
        }

        // Tahan urutan broadcast berikutnya jika Step 1 (Geser Kamera) belum selesai dilakukan user
        CameraPanTutorialStep cameraStep = FindFirstObjectByType<CameraPanTutorialStep>();
        if (cameraStep != null && !cameraStep.IsCompleted)
        {
            if (!cameraStep.IsActive)
            {
                cameraStep.StartStep();
            }

            while (!cameraStep.IsCompleted)
            {
                yield return null;
            }

            // Jeda singkat setelah user berhasil menggeser kamera
            yield return new WaitForSeconds(1.0f);
        }

        // Tahan urutan broadcast berikutnya sampai Step 2 (Klik Containment Unit & Info) selesai
        ContainmentUnitTutorialStep step2 = FindFirstObjectByType<ContainmentUnitTutorialStep>();
        if (step2 != null && !step2.IsCompleted)
        {
            while (!step2.IsCompleted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // Tahan urutan broadcast berikutnya sampai Step 3 (Pindahkan & Return Employee) selesai
        EmployeeMoveTutorialStep step3 = FindFirstObjectByType<EmployeeMoveTutorialStep>();
        if (step3 != null && !step3.IsCompleted)
        {
            while (!step3.IsCompleted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // Tahan urutan broadcast berikutnya sampai Step 4 (Research Tanaman) selesai
        ResearchTutorialStep step4 = FindFirstObjectByType<ResearchTutorialStep>();
        if (step4 != null && !step4.IsCompleted)
        {
            while (!step4.IsCompleted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // Tahan urutan broadcast berikutnya sampai Step 5 (Nutrisi Kalium) selesai
        NutritionTutorialStep step5 = FindFirstObjectByType<NutritionTutorialStep>();
        if (step5 != null && !step5.IsCompleted)
        {
            while (!step5.IsCompleted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // Tahan urutan broadcast berikutnya sampai Step 6 (Harvest Monster Overgrowth) selesai
        HarvestTutorialStep step6 = FindFirstObjectByType<HarvestTutorialStep>();
        if (step6 != null && !step6.IsCompleted)
        {
            while (!step6.IsCompleted)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // Tampilkan broadcast tambahan jika ada
        if (additionalMessages != null && additionalMessages.Count > 0)
        {
            foreach (var item in additionalMessages)
            {
                if (item.delayBefore > 0f)
                {
                    yield return new WaitForSeconds(item.delayBefore);
                }

                if (!string.IsNullOrEmpty(item.message))
                {
                    string sender = string.IsNullOrEmpty(item.sender) ? senderName : item.sender;
                    FacilityHUD.ShowBroadcast(item.message, sender, item.duration);
                }
            }
        }
    }
}
