using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
  public class ContainmentRoomMonitoringTests
  {
    [MenuItem("Tests/Run ContainmentRoom Monitoring Tests")]
    public static void RunAllTestsFromMenu()
    {
      var testRunner = new ContainmentRoomMonitoringTests();
      int passed = 0;
      int total = 7;

      Debug.Log("<color=yellow>=== MEMULAI TEST CONTAINMENT ROOM MONITORING ===</color>");

      try
      {
        testRunner.SetUp();
        testRunner.Test_ContainmentRoomHasMonstersReturnsTrueWhenUnitHasMonster();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 1: HasMonsters returns true when unit has monster</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_ContainmentRoomHasMonstersReturnsFalseWhenNoMonsters();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 2: HasMonsters returns false when no monsters</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_CalculateAverageSuitableTemperatureWithMultipleMonsters();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 3: CalculateAverageSuitableTemperature averages correctly</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_CalculateAverageSuitableTemperatureWithSingleMonster();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 4: CalculateAverageSuitableTemperature works with single monster</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_AssignAndUnassignMonitor();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 5: AssignMonitor and UnassignMonitor work correctly</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_MonitoringTaskValidatesResearcherOnly();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 6: MonitoringTask validates Researcher division only</color>");
        passed++;

        testRunner.SetUp();
        testRunner.Test_MonitoringTaskUpdatesRoomTemperature();
        testRunner.TearDown();
        Debug.Log("<color=green>✓ [PASSED] Test 7: MonitoringTask updates room temperature to average</color>");
        passed++;
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"<color=red>✗ [FAILED] Test gagal dengan exception: {ex.Message}</color>\n{ex.StackTrace}");
      }

      Debug.Log($"<color=cyan>=== HASIL TEST: {passed}/{total} BERHASIL LULUS ===</color>");
    }

    private GameObject containmentRoomObj;
    private ContainmentRoom containmentRoom;
    private GameObject unitObj1;
    private GameObject unitObj2;
    private ContainmentUnit unit1;
    private ContainmentUnit unit2;
    private GameObject monsterObj1;
    private GameObject monsterObj2;
    private Dandelectric monster1;
    private Dandelectric monster2;
    private GameObject researcherObj;
    private EmployeeResearcher researcher;
    private GameObject botanistObj;
    private EmployeeBotanist botanist;

    [SetUp]
    public void SetUp()
    {
      // Create ContainmentRoom
      containmentRoomObj = new GameObject("TestContainmentRoom");
      containmentRoomObj.AddComponent<BoxCollider2D>();
      containmentRoom = containmentRoomObj.AddComponent<ContainmentRoom>();
      containmentRoom.RoomName = "Test Containment Room";

      // Create ContainmentUnit 1
      unitObj1 = new GameObject("TestUnit1");
      unitObj1.AddComponent<BoxCollider2D>();
      unit1 = unitObj1.AddComponent<ContainmentUnit>();
      unit1.UnitName = "Unit 1";
      unitObj1.transform.SetParent(containmentRoomObj.transform);

      // Create ContainmentUnit 2
      unitObj2 = new GameObject("TestUnit2");
      unitObj2.AddComponent<BoxCollider2D>();
      unit2 = unitObj2.AddComponent<ContainmentUnit>();
      unit2.UnitName = "Unit 2";
      unitObj2.transform.SetParent(containmentRoomObj.transform);

      // Add units to room via reflection (since containmentUnits is private)
      var unitsField = typeof(ContainmentRoom).GetField("containmentUnits",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      var unitsList = (List<ContainmentUnit>)unitsField.GetValue(containmentRoom);
      unitsList.Add(unit1);
      unitsList.Add(unit2);

      // Create Monster 1 (Dandelectric with suitable temp 40)
      monsterObj1 = new GameObject("TestMonster1");
      monster1 = monsterObj1.AddComponent<Dandelectric>();
      // Use reflection to set protected properties
      typeof(MonsterBase).GetField("monsterName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(monster1, "Monster A");
      monster1.ModifySuitableTemperature(40f - monster1.SuitableTemperature); // Adjust from default (20) to 40
      monsterObj1.transform.SetParent(unitObj1.transform);
      unit1.AssignMonster(monster1);

      // Create Monster 2 (Dandelectric with suitable temp 30)
      monsterObj2 = new GameObject("TestMonster2");
      monster2 = monsterObj2.AddComponent<Dandelectric>();
      typeof(MonsterBase).GetField("monsterName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(monster2, "Monster B");
      monster2.ModifySuitableTemperature(30f - monster2.SuitableTemperature); // Adjust from default (20) to 30
      monsterObj2.transform.SetParent(unitObj2.transform);
      unit2.AssignMonster(monster2);

      // Create Researcher Employee
      researcherObj = new GameObject("TestResearcher");
      researcherObj.AddComponent<BoxCollider2D>();
      researcher = researcherObj.AddComponent<EmployeeResearcher>();
      researcher.EmployeeName = "Researcher Test";
      researcher.SetDivision(EmployeeDivision.Researcher);

      // Create Botanist Employee (non-researcher)
      botanistObj = new GameObject("TestBotanist");
      botanistObj.AddComponent<BoxCollider2D>();
      botanist = botanistObj.AddComponent<EmployeeBotanist>();
      botanist.EmployeeName = "Botanist Test";
      botanist.SetDivision(EmployeeDivision.Botanist);
    }

    [TearDown]
    public void TearDown()
    {
      if (containmentRoomObj != null) Object.DestroyImmediate(containmentRoomObj);
      if (unitObj1 != null) Object.DestroyImmediate(unitObj1);
      if (unitObj2 != null) Object.DestroyImmediate(unitObj2);
      if (monsterObj1 != null) Object.DestroyImmediate(monsterObj1);
      if (monsterObj2 != null) Object.DestroyImmediate(monsterObj2);
      if (researcherObj != null) Object.DestroyImmediate(researcherObj);
      if (botanistObj != null) Object.DestroyImmediate(botanistObj);
    }

    [Test]
    public void Test_ContainmentRoomHasMonstersReturnsTrueWhenUnitHasMonster()
    {
      // Both units have monsters
      Assert.IsTrue(containmentRoom.HasMonsters, "HasMonsters should return true when units have monsters");
    }

    [Test]
    public void Test_ContainmentRoomHasMonstersReturnsFalseWhenNoMonsters()
    {
      // Remove monsters from both units
      unit1.RemoveMonster();
      unit2.RemoveMonster();

      Assert.IsFalse(containmentRoom.HasMonsters, "HasMonsters should return false when no units have monsters");
    }

    [Test]
    public void Test_CalculateAverageSuitableTemperatureWithMultipleMonsters()
    {
      // Monster A: 40°C, Monster B: 30°C -> Average = 35°C
      float avgTemp = containmentRoom.CalculateAverageSuitableTemperature();

      Assert.AreEqual(35f, avgTemp, 0.01f, "Average temperature should be (40 + 30) / 2 = 35");
    }

    [Test]
    public void Test_CalculateAverageSuitableTemperatureWithSingleMonster()
    {
      // Remove monster from unit2
      unit2.RemoveMonster();

      // Only Monster A (40°C) remains
      float avgTemp = containmentRoom.CalculateAverageSuitableTemperature();

      Assert.AreEqual(40f, avgTemp, 0.01f, "Average temperature should be 40 when only one monster");
    }

    [Test]
    public void Test_AssignAndUnassignMonitor()
    {
      // Initially no monitor
      Assert.IsFalse(containmentRoom.HasMonitor, "Should not have monitor initially");
      Assert.IsNull(containmentRoom.AssignedMonitor, "AssignedMonitor should be null initially");

      // Assign researcher
      containmentRoom.AssignMonitor(researcher);

      Assert.IsTrue(containmentRoom.HasMonitor, "Should have monitor after assignment");
      Assert.AreEqual(researcher, containmentRoom.AssignedMonitor, "AssignedMonitor should be the researcher");

      // Unassign
      containmentRoom.UnassignMonitor();

      Assert.IsFalse(containmentRoom.HasMonitor, "Should not have monitor after unassignment");
      Assert.IsNull(containmentRoom.AssignedMonitor, "AssignedMonitor should be null after unassignment");
    }

    [Test]
    public void Test_MonitoringTaskValidatesResearcherOnly()
    {
      var monitoringTask = new MonitoringTask(containmentRoom);
      bool onFailCalled = false;
      bool onCompleteCalled = false;

      // Test with Botanist (non-researcher) - should fail immediately due to division validation
      monitoringTask.Start(botanist,
          () => onCompleteCalled = true,
          () => onFailCalled = true);

      Assert.IsTrue(onFailCalled, "OnFail should be called for non-Researcher");
      Assert.IsFalse(onCompleteCalled, "OnComplete should not be called for non-Researcher");

      // Reset
      onFailCalled = false;
      onCompleteCalled = false;

      // Test with Researcher - division validation should pass.
      // In edit mode, MoveTo may fail synchronously (no Facility/RoomPathfinder setup),
      // which would call onFail from OnMoveFailed -- that's a pathfinding issue, NOT
      // a division validation issue. So we capture log messages to specifically check
      // that the "is not a Researcher" warning was NOT logged for the Researcher.
      bool divisionValidationFailed = false;
      Application.LogCallback logHandler = (logString, stackTrace, logType) =>
      {
        if (logString != null && logString.Contains("is not a Researcher"))
          divisionValidationFailed = true;
      };
      Application.logMessageReceived += logHandler;

      try
      {
        monitoringTask.Start(researcher,
            () => onCompleteCalled = true,
            () => onFailCalled = true);
      }
      finally
      {
        Application.logMessageReceived -= logHandler;
      }

      // The key test: division validation passes for Researcher.
      // (onFail may be called from MoveTo pathfinding failure in edit mode,
      // but that is a separate concern from division validation)
      Assert.IsFalse(divisionValidationFailed,
          "Division validation should pass for Researcher (no 'not a Researcher' warning logged)");
    }

    [Test]
    public void Test_MonitoringTaskUpdatesRoomTemperature()
    {
      // Set initial room temperature to something different
      containmentRoom.Temperature = 20f;
      Assert.AreEqual(20f, containmentRoom.Temperature, "Initial temperature should be 20");

      // Create monitoring task
      var monitoringTask = new MonitoringTask(containmentRoom);

      // Use reflection to set up internal state without calling Start().
      // Start() triggers MoveTo which requires Facility/RoomPathfinder (not available in edit mode).
      // We directly set the employee field so UpdateRoomTemperature can run.
      var employeeField = typeof(MonitoringTask).GetField("employee",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      employeeField.SetValue(monitoringTask, researcher);

      // Call UpdateRoomTemperature via reflection
      var updateTempMethod = typeof(MonitoringTask).GetMethod("UpdateRoomTemperature",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      updateTempMethod.Invoke(monitoringTask, null);

      // Temperature should now be average (35°C)
      Assert.AreEqual(35f, containmentRoom.Temperature, 0.01f,
          "Room temperature should be updated to average of monsters' suitable temperatures (35°C)");
    }
  }
}