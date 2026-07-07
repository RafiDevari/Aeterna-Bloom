    using UnityEngine;

    /// <summary>
    /// Contoh monster.
    /// - Jika suhu ruangan berbeda lebih dari allowedDifference,
    ///   mood turun setiap moodInterval detik.
    /// - Saat mood mencapai 0:
    ///     - Naikkan suhu ruangan
    ///     - Panggil 1 employee
    /// - Efek hanya bisa aktif lagi setelah cooldown.
    /// </summary>
    public class MonsterTest1234 : MonsterBase
    {
        [Header("Temperature")]
        [SerializeField] private float allowedDifference = 5f;

        [Tooltip("Interval penurunan mood jika suhu tidak sesuai.")]
        [SerializeField] private float moodInterval = 30f;

        [Header("Mood 0 Effect")]
        [SerializeField] private float roomTempIncrease = 2f;

        [SerializeField] private float triggerCooldown = 10f;

        private float temperatureTimer;
        private float cooldownTimer;

        private bool hasTriggeredAtMoodZero;

        private void Awake()
        {
            base.Awake();   
            MonsterName = "MonsterTest1234";
        }

        protected override void OnMonsterUpdate()
        {
            if (Context == null || Context.CurrentRoom == null)
                return;

            //--------------------------------------
            // Mood turun karena suhu
            //--------------------------------------

            float difference = Mathf.Abs(
                Context.CurrentRoom.Temperature - SuitableTemperature);

            if (difference <= allowedDifference)
            {
                temperatureTimer = 0f;
            }
            else
            {
                if (Every(ref temperatureTimer, moodInterval))
                {
                    ModifyMood(-1);

                    Debug.Log(
                        $"[{MonsterName}] Mood turun karena suhu. Selisih = {difference:F1}");
                }
            }

            //--------------------------------------
            // Cooldown efek Mood 0
            //--------------------------------------

            if (hasTriggeredAtMoodZero)
            {
                if (Every(ref cooldownTimer, triggerCooldown))
                {
                    hasTriggeredAtMoodZero = false;

                    Debug.Log($"[{MonsterName}] Cooldown selesai.");
                }
            }
        }

        protected override void OnMoodChange(int oldMood, int newMood)
        {
            if (newMood == 0 && !hasTriggeredAtMoodZero)
            {
                TriggerMoodZeroEffect();
            }
        }

        private void TriggerMoodZeroEffect()
        {
            hasTriggeredAtMoodZero = true;

            Debug.Log($"[{MonsterName}] Mood mencapai 0.");

            Context.ChangeRoomTemperature(roomTempIncrease);

            Context.SummonRandomEmployee();
        }
    }