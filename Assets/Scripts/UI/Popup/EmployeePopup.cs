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

    private Employee targetEmployee;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        closeButton.onClick.AddListener(Close);
        if (takeCareButton != null)
            takeCareButton.onClick.AddListener(OnTakeCareClicked);

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
            nameText.text = employee != null ? employee.EmployeeName : "-";

        if (divisionText != null)
        {
            if (employee != null)
            {
                string text = employee.Division.ToString();
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
            hpText.text = employee != null ? $"HP: {employee.Hp}/{employee.MaxHp}" : "-";

        if (moodText != null)
            moodText.text = employee != null ? $"Mood: {employee.MoodName} ({employee.Mood}/5)" : "-";

        if (takeCareButton != null)
        {
            takeCareButton.gameObject.SetActive(employee != null && employee.CurrentState == EmployeeState.Hypnotized);
        }

        base.Open();
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

    private void OnGUI()
    {
        if (IsOpen && targetEmployee != null && targetEmployee.CurrentState == EmployeeState.Hypnotized && takeCareButton == null)
        {
            float w = 200f;
            float h = 40f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 150f;

            if (GUI.Button(new Rect(x, y, w, h), "TAKE CARE"))
            {
                OnTakeCareClicked();
            }
        }
    }

    protected override void OnClosed()
    {
        targetEmployee = null;
    }
}