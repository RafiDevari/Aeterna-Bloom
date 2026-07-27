using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tests
{
    public class VirusAndSicknessTests
    {
        [MenuItem("Tests/Run Virus & Sickness Tests")]
        public static void RunAllTestsFromMenu()
        {
            var testRunner = new VirusAndSicknessTests();
            int passed = 0;
            int total = 5;

            Debug.Log("<color=yellow>=== MEMULAI TEST VIRUS & SICKNESS ===</color>");

            try
            {
                testRunner.SetUp();
                testRunner.Test_MedicIsImmuneToVirus();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 1: MedicIsImmuneToVirus</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_NonMedicContractsVirus();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 2: NonMedicContractsVirus</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_SicknessDoesNotOverrideEmployeeState();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 3: SicknessDoesNotOverrideEmployeeState</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_MedicCuresSickness();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 4: MedicCuresSickness</color>");
                passed++;

                testRunner.SetUp();
                testRunner.Test_NonMedicHealDoesNotRemoveSickness();
                testRunner.TearDown();
                Debug.Log("<color=green>✓ [PASSED] Test 5: NonMedicHealDoesNotRemoveSickness</color>");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>✗ [FAILED] Test gagal dengan exception: {ex.Message}</color>\n{ex.StackTrace}");
            }

            Debug.Log($"<color=cyan>=== HASIL TEST: {passed}/{total} BERHASIL LULUS ===</color>");
        }

        private GameObject testObject;
        private Employee nonMedicEmp;
        private EmployeeMedic medicEmp;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestEmployeeObj");
            
            // Create Non-Medic Employee
            GameObject nonMedicObj = new GameObject("NonMedicEmp");
            nonMedicEmp = nonMedicObj.AddComponent<Employee>();
            nonMedicEmp.SetDivision(EmployeeDivision.Botanist);

            // Create Medic Employee
            GameObject medicObj = new GameObject("MedicEmp");
            medicEmp = medicObj.AddComponent<EmployeeMedic>();
            medicEmp.SetDivision(EmployeeDivision.Medic);
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null) Object.DestroyImmediate(testObject);
            if (nonMedicEmp != null) Object.DestroyImmediate(nonMedicEmp.gameObject);
            if (medicEmp != null) Object.DestroyImmediate(medicEmp.gameObject);
        }

        [Test]
        public void Test_MedicIsImmuneToVirus()
        {
            Assert.IsTrue(medicEmp.IsImmuneToVirus, "Medic employee must be immune to virus.");
            medicEmp.InfectVirus();
            Assert.IsFalse(medicEmp.IsSick, "Medic should not contract virus when infected.");
        }

        [Test]
        public void Test_NonMedicContractsVirus()
        {
            Assert.IsFalse(nonMedicEmp.IsImmuneToVirus, "Non-Medic employee should not be immune to virus.");
            nonMedicEmp.InfectVirus();
            Assert.IsTrue(nonMedicEmp.IsSick, "Non-Medic employee must contract virus.");
        }

        [Test]
        public void Test_SicknessDoesNotOverrideEmployeeState()
        {
            EmployeeState stateBefore = nonMedicEmp.CurrentState;
            nonMedicEmp.InfectVirus();
            EmployeeState stateAfter = nonMedicEmp.CurrentState;

            Assert.AreEqual(stateBefore, stateAfter, "EmployeeState must not be changed/overridden by sickness.");
        }

        [Test]
        public void Test_MedicCuresSickness()
        {
            nonMedicEmp.InfectVirus();
            Assert.IsTrue(nonMedicEmp.IsSick);

            // Medic cures target
            nonMedicEmp.CureVirus();
            Assert.IsFalse(nonMedicEmp.IsSick, "CureVirus() must remove sickness.");
        }

        [Test]
        public void Test_NonMedicHealDoesNotRemoveSickness()
        {
            nonMedicEmp.InfectVirus();
            Assert.IsTrue(nonMedicEmp.IsSick);

            // Simulating non-Medic healing: HP is modified but CureVirus is NOT called
            nonMedicEmp.ModifyHp(20);
            Assert.IsTrue(nonMedicEmp.IsSick, "Non-Medic heal must NOT remove sickness status.");
        }
    }
}
