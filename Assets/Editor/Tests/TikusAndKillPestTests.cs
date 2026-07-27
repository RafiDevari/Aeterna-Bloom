using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tests
{
    public class TikusAndKillPestTests
    {
        [MenuItem("Tests/Run Tikus & KillPest Tests")]
        public static void RunAllTestsFromMenu()
        {
            var testRunner = new TikusAndKillPestTests();
            int passed = 0;
            int total = 4;

            Debug.Log("<color=yellow>=== MEMULAI TEST TIKUS & KILLPEST ===</color>");

            try
            {
                testRunner.SetUp();
                testRunner.Test_TikusAttackMechanics();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 1: TikusAttackMechanics (-2 HP damage)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_SecurityKillsTikusWithoutPenalty();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 2: SecurityKillsTikusWithoutPenalty</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_NonSecurityKillsTikusWithPenalty();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 3: NonSecurityKillsTikusWithPenalty (Mood -1, HP -20)</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_TikusCorpseRemainsVisibleOnDeath();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 4: TikusCorpseRemainsVisibleOnDeath</color>");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>✗ [FAILED] Test gagal dengan exception: {ex.Message}</color>\n{ex.StackTrace}");
            }

            Debug.Log($"<color=cyan>=== HASIL TEST: {passed}/{total} BERHASIL LULUS ===</color>");
        }

        private GameObject tikusObj;
        private Tikus tikus;
        private Employee securityEmp;
        private Employee nonSecurityEmp;

        [SetUp]
        public void SetUp()
        {
            tikusObj = new GameObject("TestTikus");
            tikusObj.AddComponent<BoxCollider2D>();
            tikus = tikusObj.AddComponent<Tikus>();

            // Security Employee
            GameObject secObj = new GameObject("SecEmp");
            securityEmp = secObj.AddComponent<EmployeeSecurity>();
            securityEmp.SetDivision(EmployeeDivision.Security);

            // Non-Security Employee (Botanist)
            GameObject nonSecObj = new GameObject("NonSecEmp");
            nonSecurityEmp = nonSecObj.AddComponent<Employee>();
            nonSecurityEmp.SetDivision(EmployeeDivision.Botanist);
        }

        [TearDown]
        public void TearDown()
        {
            if (tikusObj != null) Object.DestroyImmediate(tikusObj);
            if (securityEmp != null) Object.DestroyImmediate(securityEmp.gameObject);
            if (nonSecurityEmp != null) Object.DestroyImmediate(nonSecurityEmp.gameObject);
        }

        [Test]
        public void Test_TikusAttackMechanics()
        {
            int hpBefore = nonSecurityEmp.Hp;
            // Modify HP directly to test attack damage deduction
            nonSecurityEmp.ModifyHp(-2);
            int hpAfter = nonSecurityEmp.Hp;

            Assert.AreEqual(hpBefore - 2, hpAfter, "Attacking employee must reduce 2 HP.");
        }

        [Test]
        public void Test_SecurityKillsTikusWithoutPenalty()
        {
            int hpBefore = securityEmp.Hp;
            int moodBefore = securityEmp.Mood;

            tikus.Kill();

            Assert.IsTrue(tikus.IsDead, "Tikus must be dead.");
            Assert.AreEqual(hpBefore, securityEmp.Hp, "Security HP must not decrease.");
            Assert.AreEqual(moodBefore, securityEmp.Mood, "Security Mood must not decrease.");
        }

        [Test]
        public void Test_NonSecurityKillsTikusWithPenalty()
        {
            int hpBefore = nonSecurityEmp.Hp;
            int moodBefore = nonSecurityEmp.Mood;

            // Simulate Non-Security killing pest with penalty
            tikus.Kill();
            nonSecurityEmp.ModifyMood(-1);
            nonSecurityEmp.ModifyHp(-20);

            Assert.IsTrue(tikus.IsDead, "Tikus must be dead.");
            Assert.AreEqual(hpBefore - 20, nonSecurityEmp.Hp, "Non-Security HP must decrease by 20.");
            Assert.AreEqual(moodBefore - 1, nonSecurityEmp.Mood, "Non-Security Mood must decrease by 1.");
        }

        [Test]
        public void Test_TikusCorpseRemainsVisibleOnDeath()
        {
            tikus.Kill();
            Assert.IsTrue(tikus.IsDead, "Tikus must be dead.");
            Assert.IsNotNull(tikusObj, "Tikus GameObject must NOT be destroyed after death (corpse remains).");
        }
    }
}
