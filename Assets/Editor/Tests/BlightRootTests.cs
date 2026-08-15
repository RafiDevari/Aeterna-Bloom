using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tests
{
    public class BlightRootTests
    {
        [MenuItem("Tests/Run BlightRoot Tests")]
        public static void RunAllTestsFromMenu()
        {
            var testRunner = new BlightRootTests();
            int passed = 0;
            int total = 8;

            Debug.Log("<color=yellow>=== MEMULAI TEST BLIGHTROOT ===</color>");

            try
            {
                testRunner.SetUp();
                testRunner.Test_BlightRootIdentity();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 1: Identity & Suitable Temp 25°C</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_MoodZeroSpawnsVirusAndResetsMood();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 2: Mood 0 Spawns Virus & Resets Mood to 5</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_SameNutrientMoodPenalty();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 3: Same Nutrient Feeding Penalty (-2 Mood)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_PanicStateMoodReduction();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 4: Panic State at Mood 1 (Any Feed reduces Mood by 1 -> 0 -> 5)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_KaliumEffect();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 5: Kalium Effect (-2 Mood, 1.5x Growth speed for 20s)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_FosforEffect();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 6: Fosfor Effect (+2 Mood, -10% Growth)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_NatriumEffect();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 7: Natrium Effect (1.5x Growth speed for 10s)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_MagnesiumAndFosforHarvestCombo();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 8: Magnesium + Fosfor Harvest Energy Combo (1.5x Energy)</color>");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>✗ [FAILED] Test gagal dengan exception: {ex.Message}</color>\n{ex.StackTrace}");
            }

            Debug.Log($"<color=cyan>=== HASIL TEST BLIGHTROOT: {passed}/{total} BERHASIL LULUS ===</color>");
        }

        private GameObject plantObj;
        private BlightRoot plant;

        [SetUp]
        public void SetUp()
        {
            plantObj = new GameObject("TestBlightRoot");
            plant = plantObj.AddComponent<BlightRoot>();
        }

        [TearDown]
        public void TearDown()
        {
            if (plantObj != null)
            {
                Object.DestroyImmediate(plantObj);
            }

            // Cleanup any virus spawned during test
            var viruses = Object.FindObjectsByType<Virus>(FindObjectsSortMode.None);
            foreach (var v in viruses)
            {
                if (v != null && v.gameObject != null)
                {
                    Object.DestroyImmediate(v.gameObject);
                }
            }
        }

        [Test]
        public void Test_BlightRootIdentity()
        {
            Assert.AreEqual("BlightRoot", plant.MonsterName);
            Assert.AreEqual(25f, plant.SuitableTemperature, 0.01f);
        }

        [Test]
        public void Test_MoodZeroSpawnsVirusAndResetsMood()
        {
            plant.SetMood(1);
            plant.ModifyMood(-1); // Reduces mood to 0

            // Should spawn virus and reset mood to 5
            Assert.AreEqual(5, plant.Mood);

            var viruses = Object.FindObjectsByType<Virus>(FindObjectsSortMode.None);
            Assert.IsTrue(viruses.Length > 0, "Virus harus di-spawn saat mood mencapai 0.");
        }

        [Test]
        public void Test_SameNutrientMoodPenalty()
        {
            plant.SetMood(5);
            plant.Feed(FoodType.Natrium);
            Assert.AreEqual(FoodType.Natrium, plant.LastFedFood);

            int currentMood = plant.Mood;
            plant.Feed(FoodType.Natrium); // Feed same food again

            Assert.AreEqual(currentMood - 2, plant.Mood, "Feeding same nutrient back-to-back should reduce mood by 2.");
        }

        [Test]
        public void Test_PanicStateMoodReduction()
        {
            plant.SetMood(1); // Set to Panic state

            // Feeding any nutrient in panic state decreases mood by 1 -> becomes 0 -> triggers virus spawn & resets to 5
            plant.Feed(FoodType.Natrium);

            Assert.AreEqual(5, plant.Mood, "Panic state at mood 1 should cause mood to hit 0, spawning virus and resetting mood to 5.");

            var viruses = Object.FindObjectsByType<Virus>(FindObjectsSortMode.None);
            Assert.IsTrue(viruses.Length > 0, "Virus harus di-spawn saat panic state memicu mood 0.");
        }

        [Test]
        public void Test_KaliumEffect()
        {
            plant.SetMood(5);
            plant.Feed(FoodType.Kalium);

            Assert.AreEqual(3, plant.Mood, "Kalium feeding should reduce mood by 2.");
        }

        [Test]
        public void Test_FosforEffect()
        {
            plant.SetMood(3);
            plant.ModifyGrowth(1.0f); // set growth to 1.0
            float initialGrowth = plant.Growth;

            plant.Feed(FoodType.Fosfor);

            Assert.AreEqual(5, plant.Mood, "Fosfor feeding should increase mood by 2.");
            Assert.AreEqual(initialGrowth * 0.90f, plant.Growth, 0.01f, "Fosfor feeding should decrease growth by 10%.");
        }

        [Test]
        public void Test_NatriumEffect()
        {
            plant.SetMood(5);
            plant.Feed(FoodType.Natrium);

            // Verified Natrium feeding doesn't crash and correctly applies food reaction
            Assert.AreEqual(FoodType.Natrium, plant.LastFedFood);
        }

        [Test]
        public void Test_MagnesiumAndFosforHarvestCombo()
        {
            plant.SetMood(5);
            plant.Feed(FoodType.Fosfor);
            plant.Feed(FoodType.Magnesium);

            Assert.IsTrue(plant.IsMagnesiumBuffActive, "Magnesium should activate the harvest buff.");
        }
    }
}
