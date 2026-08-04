using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup sederhana yang muncul saat Employee di-klik (kanan).
/// Menampilkan nama + divisi employee. Berisi pilihan: Close.
///
/// PENTING: sama seperti ContainmentPopup, script ini harus ada di
/// GameObject yang SELALU AKTIF (misalnya langsung di Canvas), BUKAN
/// di GameObject visual popup (popupRoot) yang di-toggle aktif/nonaktif.
/// Lihat komentar di PopupBase.cs untuk alasannya.
/// </summary>
public class EmployeePopup : PopupBase
{
    public static EmployeePopup Instance { get; private set; }

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text divisionText;

    [Header("Stats (Optional)")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text moodText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button takeCareButton;
    [SerializeField] private Button healSickButton;
    [SerializeField] private Button backToDivisionButton;

    private Employee targetEmployee;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        closeButton.onClick.AddListener(Close);
        if (takeCareButton != null)
            takeCareButton.onClick.AddListener(OnTakeCareClicked);
        if (healSickButton != null)
            healSickButton.onClick.AddListener(OnHealSickClicked);
        if (backToDivisionButton != null)
            backToDivisionButton.onClick.AddListener(OnBackToDivisionClicked);

        Employee.OnAnyEmployeeRightClicked += HandleEmployeeRightClicked;
    }

    private void OnDestroy()
    {
        Employee.OnAnyEmployeeRightClicked -= HandleEmployeeRightClicked;
    }

    private void HandleEmployeeRightClicked(Employee employee)
    {
        Open(employee);
    }

    public void Open(Employee employee)
    {
        targetEmployee = employee;

        if (nameText != null)
        {
            string nameStr = employee != null ? employee.EmployeeName : "-";
            if (employee != null && employee.IsSick) nameStr += " (SICK)";
            if (employee != null && employee.CurrentState == EmployeeState.Sleeping) nameStr += " (Resting)";
            // Show monitoring state
            if (employee != null && IsMonitoring(employee)) nameStr += " (Monitoring)";
            nameText.text = nameStr;
        }

        if (divisionText != null)
        {
            if (employee != null)
            {
                string text = employee.Division.ToString();
                if (employee.IsSick) text += " [SICK]";
                if (employee.CurrentState == EmployeeState.Sleeping) text += " [Resting]";
                if (IsMonitoring(employee)) text += " [Monitoring]";
                if (hpText == null || moodText == null)
                {
                    text += $"\nHP: {employee.Hp}/{employee.MaxHp}\nMood: {employee.MoodName} ({employee.Mood}/5)";
                }
                divisionText.text = text;
            }
            else
            {
                divisionText.text = "-";
            }
        }

        if (hpText != null)
        {
            string hpStr = employee != null ? $"HP: {employee.Hp}/{employee.MaxHp}" : "-";
            if (employee != null && employee.IsSick) hpStr += " (SICK)";
            if (employee != null && employee.CurrentState == EmployeeState.Sleeping) hpStr += " (Resting)";
            hpText.text = hpStr;
        }

        if (moodText != null)
            moodText.text = employee != null ? $"Mood: {employee.MoodName} ({employee.Mood}/5)" : "-";

        if (takeCareButton != null)
        {
            takeCareButton.gameObject.SetActive(employee != null && employee.CurrentState == EmployeeState.Hypnotized);
        }

        if (healSickButton != null)
        {
            healSickButton.gameObject.SetActive(employee != null && employee.IsSick);
        }

        // Show Back to Division button when employee is monitoring a containment room
        if (backToDivisionButton != null)
        {
            backToDivisionButton.gameObject.SetActive(employee != null && IsMonitoring(employee));
        }

        base.Open();
    }

    /// <summary>
    /// Check if employee is currently monitoring a containment room.
    /// We consider an employee as monitoring if they're in Researching state
    /// and their current task is a MonitoringTask.
    /// </summary>
    private bool IsMonitoring(Employee employee)
    {
        if (employee == null) return false;
        if (employee.CurrentState != EmployeeState.Researching) return false;

        // Check if employee is in a containment room that has a monitor assigned
        var currentRoom = employee.CurrentRoom;
        if (currentRoom is ContainmentRoom containmentRoom)
        {
            return containmentRoom.HasMonitor && containmentRoom.AssignedMonitor == employee;
        }
        return false;
    }

    private void OnTakeCareClicked()
    {
        Employee capturedTarget = targetEmployee;

        Close();

        EmployeeSelectPopup.Instance.Open(healer =>
        {
            if (healer != null && capturedTarget != null)
            {
                healer.EnqueueTask(new TakeCareTask(capturedTarget));
                Debug.Log($"[EmployeePopup] {healer.EmployeeName} ditugaskan untuk merawat {capturedTarget.EmployeeName}");
            }
        }, typeof(DivisionMedic));
    }

    private void OnHealSickClicked()
    {
        Employee capturedTarget = targetEmployee;

        Close();

        EmployeeSelectPopup.Instance.Open(healer =>
        {
            if (healer != null && capturedTarget != null)
            {
                healer.EnqueueTask(new HealSickTask(capturedTarget));
                Debug.Log($"[EmployeePopup] {healer.EmployeeName} ditugaskan untuk mengobati {capturedTarget.EmployeeName}");
            }
        }, typeof(DivisionMedic));
    }

    private void OnBackToDivisionClicked()
    {
        Employee capturedTarget = targetEmployee;
        Close();

        if (capturedTarget != null)
        {
            capturedTarget.BackToDivision();
            Debug.Log($"[EmployeePopup] {capturedTarget.EmployeeName} ordered to return to division.");
        }
    }

    private void OnGUI()
    {
        if (!IsOpen || targetEmployee == null) return;

        float w = 200f;
        float h = 40f;

        if (targetEmployee.CurrentState == EmployeeState.Hypnotized && takeCareButton == null)
        {
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 150f;

            if (GUI.Button(new Rect(x, y, w, h), "TAKE CARE"))
            {
                OnTakeCareClicked();
            }
        }

        if (targetEmployee.IsSick && healSickButton == null)
        {
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 100f;

            if (GUI.Button(new Rect(x, y, w, h), "HEAL (OBATI)"))
            {
                OnHealSickClicked();
            }
        }

        // Fallback GUI button for Back to Division if no button assigned
        if (IsMonitoring(targetEmployee) && backToDivisionButton == null)
        {
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 50f;

            if (GUI.Button(new Rect(x, y, w, h), "BACK TO DIVISION"))
            {
                OnBackToDivisionClicked();
            }
        }
    }

    protected override void OnClosed()
    {
        targetEmployee = null;
    }
}