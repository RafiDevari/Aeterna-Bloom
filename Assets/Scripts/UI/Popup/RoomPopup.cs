using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomPopup : PopupBase
{
    public static RoomPopup Instance { get; private set; }

    private Room targetRoom;
    private RectTransform buttonContainer;

    protected override void Awake()
    {
        Instance = this;

        // Build UI dynamically if popupRoot is null
        if (popupRoot == null)
        {
            BuildDynamicUI();
        }

        base.Awake();
    }

    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            var popupManager = FindFirstObjectByType<PopupManager>();
            if (popupManager != null)
            {
                GameObject go = new GameObject("RoomPopup");
                go.transform.SetParent(popupManager.transform, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.AddComponent<RoomPopup>();
            }
        }
    }

    private void BuildDynamicUI()
    {
        // 1. Create Overlay Button
        GameObject overlayObj = new GameObject("RoomPopup_Overlay", typeof(RectTransform), typeof(Image), typeof(Button));
        overlayObj.transform.SetParent(this.transform, false);
        
        RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        
        Image overlayImg = overlayObj.GetComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.4f);
        
        // Reflection injection for overlayButton in PopupBase
        var field = typeof(PopupBase).GetField("overlayButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, overlayObj.GetComponent<Button>());

        // 2. Create Popup Root
        GameObject rootObj = new GameObject("RoomPopup_Root", typeof(RectTransform));
        rootObj.transform.SetParent(this.transform, false);
        
        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(250, 200);
        
        this.popupRoot = rootObj;
        
        // 3. Create Background Image
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(rootObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.12f, 0.15f, 0.2f, 0.98f);
        
        // 4. Container for Buttons
        GameObject containerObj = new GameObject("ButtonContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObj.transform.SetParent(rootObj.transform, false);
        buttonContainer = containerObj.GetComponent<RectTransform>();
        buttonContainer.anchorMin = Vector2.zero;
        buttonContainer.anchorMax = Vector2.one;
        buttonContainer.offsetMin = new Vector2(10, 10);
        buttonContainer.offsetMax = new Vector2(-10, -10);
        
        VerticalLayoutGroup vlg = containerObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
    }

    public void Open(Room room)
    {
        targetRoom = room;
        RefreshButtons();
        base.Open();
    }

    private void RefreshButtons()
    {
        if (buttonContainer == null) return;

        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        if (targetRoom == null) return;

        // 1. Lockdown Button (Available for all rooms)
        string lockText = targetRoom.IsLocked ? "Unlock Room" : "Lockdown Room";
        CreateButton(lockText, () =>
        {
            targetRoom.SetLocked(!targetRoom.IsLocked);
            Close();
        });

        // 2. Sterilize Button (If Poisoned)
        if (targetRoom.IsPoisoned)
        {
            CreateButton("Sterilize (Security)", () =>
            {
                if (EmployeeSelectPopup.Instance != null)
                {
                    EmployeeSelectPopup.Instance.Open(
                        employee => employee.GoSterilize(targetRoom),
                        typeof(DivisionSecurity)
                    );
                }
                Close();
            });
        }

        // 3. Monitor Button (If Containment Room with Monsters)
        if (targetRoom is ContainmentRoom cr && cr.HasMonsters)
        {
            CreateButton("Monitor (Researcher)", () =>
            {
                if (EmployeeSelectPopup.Instance != null)
                {
                    EmployeeSelectPopup.Instance.Open(
                        employee =>
                        {
                            var monitoringTask = new MonitoringTask(cr);
                            employee.EnqueueTask(monitoringTask);
                            cr.AssignMonitor(employee);
                        },
                        typeof(DivisionResearcher)
                    );
                }
                Close();
            });
        }

        // 4. Fix Electricity (If ElectricityRoom and Facility is Blackout)
        if (targetRoom is ElectricityRoom er && Facility.Instance != null && Facility.Instance.IsBlackout && !er.IsFixing)
        {
            CreateButton("Fix Electricity", () =>
            {
                if (EmployeeSelectPopup.Instance != null)
                {
                    EmployeeSelectPopup.Instance.Open(
                        employee => employee.GoFixElectricity(er)
                    );
                }
                Close();
            });
        }

        // Close Button at the bottom
        CreateButton("Close", Close, new Color(0.8f, 0.3f, 0.3f));
    }

    private void CreateButton(string text, UnityEngine.Events.UnityAction onClickAction, Color? bgColor = null)
    {
        GameObject btnObj = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(buttonContainer, false);
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 35);
        
        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor ?? new Color(0.2f, 0.25f, 0.35f);
        
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClickAction);
        
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}
