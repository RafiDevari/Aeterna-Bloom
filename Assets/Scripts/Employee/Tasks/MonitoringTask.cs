//==============================================================
// Task: Monitor a ContainmentRoom - researcher stays in the room
// and automatically adjusts temperature to average of all monsters'
// suitable temperatures every 10 seconds.
//==============================================================
using UnityEngine;
using System;

public class MonitoringTask : EmployeeTask
{
  private readonly ContainmentRoom targetRoom;
  private Employee employee;
  private System.Action onComplete;
  private System.Action onFail;
  private bool isMonitoring;
  private Vector3 monitoringPosition;
  private const float TEMPERATURE_UPDATE_INTERVAL = 10f;

  public MonitoringTask(ContainmentRoom room)
  {
    this.targetRoom = room;
  }

  public void Start(Employee employee, System.Action onComplete, System.Action onFail)
  {
    this.employee = employee;
    this.onComplete = onComplete;
    this.onFail = onFail;

    // Validate: employee must be a researcher
    if (employee.Division != EmployeeDivision.Researcher)
    {
      Debug.LogWarning($"[MonitoringTask] {employee.EmployeeName} is not a Researcher. Cannot monitor.");
      onFail?.Invoke();
      return;
    }

    // Validate: room must still have monsters
    if (!targetRoom.HasMonsters)
    {
      Debug.LogWarning($"[MonitoringTask] {targetRoom.RoomName} has no monsters. Cannot monitor.");
      onFail?.Invoke();
      return;
    }

    // Validate: room not already monitored by someone else
    if (targetRoom.HasMonitor && targetRoom.AssignedMonitor != employee)
    {
      Debug.LogWarning($"[MonitoringTask] {targetRoom.RoomName} already has a different monitor.");
      onFail?.Invoke();
      return;
    }

    // Set monitoring position (room center)
    monitoringPosition = targetRoom.transform.position;

    // Move to the room
    employee.MoveTo(monitoringPosition, OnArrivedAtRoom, OnMoveFailed);
  }

  private void OnArrivedAtRoom()
  {
    if (employee == null || employee.CurrentState == EmployeeState.Dead)
    {
      OnFailInternal();
      return;
    }

    // Start monitoring
    isMonitoring = true;
    employee.SetState(EmployeeState.Researching); // Use Researching state for monitoring
    Debug.Log($"[{employee.EmployeeName}] Started monitoring {targetRoom.RoomName}");

    // Start the first temperature update cycle
    ScheduleNextTemperatureUpdate();
  }

  private void OnMoveFailed()
  {
    Debug.LogWarning($"[{employee?.EmployeeName}] Failed to reach {targetRoom.RoomName} for monitoring.");
    OnFailInternal();
  }

  public void Cancel()
  {
    if (!isMonitoring && employee == null)
      return;

    isMonitoring = false;

    // Unassign from room
    if (targetRoom != null && targetRoom.AssignedMonitor == employee)
    {
      targetRoom.UnassignMonitor();
    }

    // Return to division
    if (employee != null && employee.CurrentState != EmployeeState.Dead)
    {
      employee.BackToDivision();
    }

    onComplete = null;
    onFail = null;
    employee = null;
  }

  /// <summary>
  /// Schedules the next temperature update using Employee's timed action system.
  /// This creates a self-repeating loop every 10 seconds.
  /// </summary>
  private void ScheduleNextTemperatureUpdate()
  {
    if (!isMonitoring || employee == null || employee.CurrentState == EmployeeState.Dead)
      return;

    // Check if room still valid for monitoring
    if (targetRoom == null || !targetRoom.HasMonsters)
    {
      Debug.Log($"[{employee.EmployeeName}] Room no longer valid for monitoring. Stopping.");
      CompleteMonitoring();
      return;
    }

    // Check if employee still assigned to this room
    if (targetRoom.AssignedMonitor != employee)
    {
      Debug.Log($"[{employee.EmployeeName}] No longer assigned to monitor {targetRoom.RoomName}. Stopping.");
      CompleteMonitoring();
      return;
    }

    // Schedule next update
    employee.StartTimedAction(TEMPERATURE_UPDATE_INTERVAL, OnTemperatureUpdateTick, OnTemperatureUpdateFail);
  }

  private void OnTemperatureUpdateTick()
  {
    if (!isMonitoring || employee == null || employee.CurrentState == EmployeeState.Dead)
      return;

    UpdateRoomTemperature();

    // Schedule next update
    ScheduleNextTemperatureUpdate();
  }

  private void OnTemperatureUpdateFail()
  {
    // Timed action failed (employee died, task cancelled, etc.)
    CompleteMonitoring();
  }

  private void UpdateRoomTemperature()
  {
    if (targetRoom == null || employee == null) return;

    float avgTemp = targetRoom.CalculateAverageSuitableTemperature();
    targetRoom.Temperature = avgTemp;
    Debug.Log($"[{employee.EmployeeName}] Adjusted {targetRoom.RoomName} temperature to {avgTemp:F1}°C (average of monsters' suitable temperatures)");
  }

  private void CompleteMonitoring()
  {
    isMonitoring = false;

    if (targetRoom != null && targetRoom.AssignedMonitor == employee)
    {
      targetRoom.UnassignMonitor();
    }

    var complete = onComplete;
    onComplete = null;
    onFail = null;
    employee = null;
    complete?.Invoke();
  }

  private void OnFailInternal()
  {
    isMonitoring = false;

    if (targetRoom != null && targetRoom.AssignedMonitor == employee)
    {
      targetRoom.UnassignMonitor();
    }

    var fail = onFail;
    onComplete = null;
    onFail = null;
    employee = null;
    fail?.Invoke();
  }

  public bool IsMonitoring => isMonitoring;
  public ContainmentRoom TargetRoom => targetRoom;
}