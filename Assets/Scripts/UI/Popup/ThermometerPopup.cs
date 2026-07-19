using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThermometerPopup : PopupBase
{
    public static ThermometerPopup Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI temperatureText;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    private Room targetRoom;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        if (upButton != null)
            upButton.onClick.AddListener(() => ModifyTemp(1f));

        if (downButton != null)
            downButton.onClick.AddListener(() => ModifyTemp(-1f));
    }

    public void Open(Room room)
    {
        if (room == null)
            return;

        targetRoom = room;
        UpdateDisplay();
        base.Open();
    }

    private void ModifyTemp(float delta)
    {
        if (targetRoom == null)
            return;

        float newTemp = Mathf.Clamp(targetRoom.Temperature + delta, 0f, 100f);
        targetRoom.Temperature = newTemp;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (targetRoom == null)
            return;

        if (roomNameText != null)
            roomNameText.text = targetRoom.RoomName;

        if (temperatureText != null)
            temperatureText.text = $"{targetRoom.Temperature:F0}°";
    }

    protected override void OnClosed()
    {
        targetRoom = null;
    }
}
