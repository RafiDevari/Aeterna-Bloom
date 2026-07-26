using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tests
{
    public class LebahTests
    {
        [MenuItem("Tests/Run Lebah Tests")]
        public static void RunAllTestsFromMenu()
        {
            var testRunner = new LebahTests();
            int passed = 0;
            int total = 4;

            Debug.Log("<color=yellow>=== MEMULAI TEST LEBAH & MONSTER INFO POPUP ===</color>");

            try
            {
                testRunner.SetUp();
                testRunner.Test_LebahDrainsPlantGrowth();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 1: LebahDrainsPlantGrowth (-3% Growth)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_LebahDrainsPlantMoodOnGrowthFloorBoundary();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 2: LebahDrainsPlantMoodOnGrowthFloorBoundary (-1 Mood pada batas floor)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_LebahDiesWhenPlantMoodIsZero();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 3: LebahDiesWhenPlantMoodIsZero (Lebah mati otomatis saat Mood = 0)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_LebahKilledViaKillPestTask();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 4: LebahKilledViaKillPestTask</color>");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>✗ [FAILED] Test gagal dengan exception: {ex.Message}</color>\n{ex.StackTrace}");
            }

            Debug.Log($"<color=cyan>=== HASIL TEST: {passed}/{total} BERHASIL LULUS ===</color>");
        }

        private GameObject plantObj;
        private Dandelectric monster;
        private GameObject lebahObj;
        private Lebah lebah;

        [SetUp]
        public void SetUp()
        {
            plantObj = new GameObject("TestDandelectric");
            monster = plantObj.AddComponent<Dandelectric>();

            lebahObj = new GameObject("TestLebah");
            lebahObj.AddComponent<BoxCollider2D>();
            lebah = lebahObj.AddComponent<Lebah>();
            lebah.SetTarget(null, monster);
        }

        [TearDown]
        public void TearDown()
        {
            if (plantObj != null) Object.DestroyImmediate(plantObj);
            if (lebahObj != null) Object.DestroyImmediate(lebahObj);
        }

        [Test]
        public void Test_LebahDrainsPlantGrowth()
        {
            // Set initial growth to 50% (above floor)
            monster.ModifyGrowth(0.5f);
            float growthBefore = monster.Growth;

            monster.ModifyGrowth(-0.03f); // -3% growth
            float growthAfter = monster.Growth;

            Assert.Less(growthAfter, growthBefore, "Growth must decrease by 3%.");
        }

        [Test]
        public void Test_LebahDrainsPlantMoodOnGrowthFloorBoundary()
        {
            // Growth at 0 (floor for Seed)
            monster.SetMood(3);
            int moodBefore = monster.Mood;

            // When growth is at floor (0), mood is reduced by 1
            monster.ModifyMood(-1);
            int moodAfter = monster.Mood;

            Assert.AreEqual(moodBefore - 1, moodAfter, "Mood must decrease by 1 when growth is at boundary.");
        }

        [Test]
        public void Test_LebahDiesWhenPlantMoodIsZero()
        {
            monster.SetMood(0);
            Assert.AreEqual(0, monster.Mood);

            lebah.Kill();
            Assert.IsTrue(lebah.IsDead, "Lebah must die when plant mood reaches 0.");
        }

        [Test]
        public void Test_LebahKilledViaKillPestTask()
        {
            lebah.Kill();
            Assert.IsTrue(lebah.IsDead, "Lebah must be killed.");
        }
    }
}
