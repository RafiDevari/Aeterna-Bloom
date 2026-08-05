using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup ID Card modern yang muncul saat Employee di-klik kanan.
/// Mengikuti format kartu identitas employee:
/// - Kiri Atas: Foto / Portrait Kepala Employee (Head)
/// - Kanan Atas: Nama Employee & Divisi
/// - Kanan Pojok: Tombol Close (✕)
/// - Bawah: Bagian "INFO LAIN" (HP, Mood, status, dan tombol aksi interaktif).
/// </summary>
public class EmployeePopup : PopupBase
{
    public static EmployeePopup Instance { get; private set; }

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text divisionText;

    [Header("Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text moodText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button takeCareButton;
    [SerializeField] private Button healSickButton;
    [SerializeField] private Button backToDivisionButton;

    [Header("Portrait (Muka)")]
    [Tooltip("Component yang menampilkan portrait kepala employee di popup.")]
    [SerializeField] private EmployeePortraitUI portraitUI;

    private Employee targetEmployee;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        EnsurePortraitUI();
        SetupCardLayout();

        if (closeButton != null)
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

        EnsurePortraitUI();
        SetupCardLayout();

        // 1. Nama Employee (Kanan Atas)
        if (nameText != null)
        {
            string nameStr = employee != null ? employee.EmployeeName : "-";
            if (employee != null && employee.IsSick) nameStr += " <color=#ef4444>[SICK]</color>";
            if (employee != null && employee.CurrentState == EmployeeState.Sleeping) nameStr += " <color=#38bdf8>[Resting]</color>";
            if (employee != null && IsMonitoring(employee)) nameStr += " <color=#fbbf24>[Monitoring]</color>";
            nameText.text = nameStr;
        }

        // 2. Divisi Badge (Kanan Atas di bawah Nama)
        if (divisionText != null)
        {
            if (employee != null)
            {
                string divName = employee.Division.ToString().ToUpper();
                string colorHex = GetDivisionColorHex(employee.Division);
                string icon = GetDivisionIcon(employee.Division);
                divisionText.text = $"<color={colorHex}>{icon} {divName} DIVISION</color>";
            }
            else
            {
                divisionText.text = "<color=#94a3b8>STAFF</color>";
            }
        }

        // 3. Info Lain: HP (Bawah)
        if (hpText != null)
        {
            if (employee != null)
            {
                float hpPercent = employee.MaxHp > 0 ? (float)employee.Hp / employee.MaxHp : 1f;
                string hpColor = hpPercent > 0.5f ? "#4ade80" : (hpPercent > 0.25f ? "#facc15" : "#ef4444");
                string hpStr = $"<color=#94a3b8>HP :</color>  <b><color={hpColor}>{employee.Hp}/{employee.MaxHp}</color></b>";
                if (employee.IsSick) hpStr += " <color=#ef4444>(SICK)</color>";
                if (employee.CurrentState == EmployeeState.Sleeping) hpStr += " <color=#38bdf8>(Resting)</color>";
                hpText.text = hpStr;
            }
            else
            {
                hpText.text = "<color=#94a3b8>HP :</color> -";
            }
        }

        // 4. Info Lain: Mood (Bawah)
        if (moodText != null)
        {
            if (employee != null)
            {
                string moodColor = employee.Mood >= 4 ? "#4ade80" : (employee.Mood >= 2 ? "#facc15" : "#ef4444");
                moodText.text = $"<color=#94a3b8>MOOD :</color>  <b><color={moodColor}>{employee.MoodName} ({employee.Mood}/5)</color></b>";
            }
            else
            {
                moodText.text = "<color=#94a3b8>MOOD :</color> -";
            }
        }

        // 5. Tombol Aksi Kondisional
        if (takeCareButton != null)
        {
            takeCareButton.gameObject.SetActive(employee != null && employee.CurrentState == EmployeeState.Hypnotized);
        }

        if (healSickButton != null)
        {
            healSickButton.gameObject.SetActive(employee != null && employee.IsSick);
        }

        if (backToDivisionButton != null)
        {
            backToDivisionButton.gameObject.SetActive(employee != null && IsMonitoring(employee));
        }

        // 6. Render Foto Muka / Portrait Kepala
        if (portraitUI != null)
        {
            portraitUI.SetEmployee(employee);
        }

        base.Open();
    }

    private static string GetDivisionColorHex(EmployeeDivision division)
    {
        return division switch
        {
            EmployeeDivision.Botanist => "#4ade80",   // Hijau
            EmployeeDivision.Researcher => "#38bdf8", // Cyan
            EmployeeDivision.Medic => "#c084fc",      // Ungu
            EmployeeDivision.Security => "#fb923c",   // Oranye
            EmployeeDivision.Engineer => "#facc15",   // Kuning
            EmployeeDivision.Clerk => "#94a3b8",      // Abu-abu
            _ => "#94a3b8"
        };
    }

    private static string GetDivisionIcon(EmployeeDivision division)
    {
        return division switch
        {
            EmployeeDivision.Botanist => "☘",
            EmployeeDivision.Researcher => "🔬",
            EmployeeDivision.Medic => "✚",
            EmployeeDivision.Security => "🛡",
            EmployeeDivision.Engineer => "⚡",
            EmployeeDivision.Clerk => "📋",
            _ => "●"
        };
    }

    private bool IsMonitoring(Employee employee)
    {
        if (employee == null) return false;
        if (employee.CurrentState != EmployeeState.Researching) return false;

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

        // Fallback GUI jika tombol aksi tidak di-link di Unity Inspector
        float w = 180f;
        float h = 30f;

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
            float y = Screen.height - 110f;
            if (GUI.Button(new Rect(x, y, w, h), "HEAL (OBATI)"))
            {
                OnHealSickClicked();
            }
        }

        if (IsMonitoring(targetEmployee) && backToDivisionButton == null)
        {
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 70f;
            if (GUI.Button(new Rect(x, y, w, h), "BACK TO DIVISION"))
            {
                OnBackToDivisionClicked();
            }
        }
    }

    protected override void OnClosed()
    {
        if (portraitUI != null)
        {
            portraitUI.Clear();
        }

        targetEmployee = null;
    }

    //=========================================
    // UI Layout & Portrait Auto-Wiring
    //=========================================

    private void EnsurePortraitUI()
    {
        if (portraitUI != null) return;

        portraitUI = GetComponentInChildren<EmployeePortraitUI>(true);
        if (portraitUI == null && popupRoot != null)
        {
            portraitUI = popupRoot.GetComponentInChildren<EmployeePortraitUI>(true);
        }

        if (portraitUI == null)
        {
            portraitUI = gameObject.AddComponent<EmployeePortraitUI>();
        }
    }

    /// <summary>
    /// Menata struktur UI popup agar presisi mengikuti sketsa format ID Card:
    /// - Kiri Atas: PortraitBox (Foto muka employee)
    /// - Kanan Atas: Nama (Yanuar / EmployeeName) + Divisi Badge + Close Button (✕)
    /// - Bawah: Bagian "INFO LAIN" (HP, Mood, dan Tombol Aksi)
    /// </summary>
    private void SetupCardLayout()
    {
        if (popupRoot == null) return;

        // Cari panel utama di dalam popupRoot
        RectTransform panel = null;
        for (int i = 0; i < popupRoot.transform.childCount; i++)
        {
            Transform child = popupRoot.transform.GetChild(i);
            if (child.name.Contains("Panel") || child.name.Contains("Box") || child.name.Contains("Body"))
            {
                panel = child as RectTransform;
                break;
            }
        }

        if (panel == null && popupRoot.transform.childCount > 1)
        {
            panel = popupRoot.transform.GetChild(1) as RectTransform;
        }

        if (panel == null) return;

        // 1. Setup Card Panel (Kotak Kartu ID)
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(440f, 250f);

        if (panel.TryGetComponent<Image>(out var bgImg))
        {
            // Dark elegant sci-fi slate background
            bgImg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);
        }

        // Outline border tipis profesional
        if (!panel.TryGetComponent<Outline>(out var cardOutline))
        {
            cardOutline = panel.gameObject.AddComponent<Outline>();
        }
        cardOutline.effectColor = new Color(0.24f, 0.30f, 0.42f, 0.8f);
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Layout vertikal di dalam kartu
        if (!panel.TryGetComponent<VerticalLayoutGroup>(out var vlg))
        {
            vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        vlg.padding = new RectOffset(16, 16, 14, 14);
        vlg.spacing = 10f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        if (panel.TryGetComponent<ContentSizeFitter>(out var csf))
        {
            csf.enabled = false;
        }

        //=========================================
        // 2. ATAS: HEADER (Kiri: Foto Muka, Kanan: Nama + Divisi, Pojok: Close)
        //=========================================
        RectTransform headerRow = GetOrCreateChildSection(panel, "Section_Header", 92f);
        headerRow.SetSiblingIndex(0);
        if (!headerRow.TryGetComponent<HorizontalLayoutGroup>(out var headerHlg))
        {
            headerHlg = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        headerHlg.padding = new RectOffset(0, 0, 0, 0);
        headerHlg.spacing = 14f;
        headerHlg.childControlWidth = false;
        headerHlg.childControlHeight = false;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = false;
        headerHlg.childAlignment = TextAnchor.MiddleLeft;

        // A. Kiri Atas: Portrait Box (Foto Muka)
        Transform portraitBox = headerRow.Find("PortraitBox");
        if (portraitBox == null)
        {
            GameObject pBoxGo = new GameObject("PortraitBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            pBoxGo.transform.SetParent(headerRow, false);
            portraitBox = pBoxGo.transform;

            RectTransform pBoxRt = pBoxGo.GetComponent<RectTransform>();
            pBoxRt.sizeDelta = new Vector2(88f, 88f);

            Image pBoxImg = pBoxGo.GetComponent<Image>();
            pBoxImg.color = new Color(0.04f, 0.05f, 0.08f, 1f); // Dark inner frame

            Outline pBoxOut = pBoxGo.GetComponent<Outline>();
            pBoxOut.effectColor = new Color(0.28f, 0.36f, 0.50f, 0.85f);
            pBoxOut.effectDistance = new Vector2(1f, -1f);
        }
        portraitBox.SetSiblingIndex(0);

        Transform pContainer = portraitBox.Find("PortraitContainer");
        if (pContainer == null)
        {
            GameObject pContGo = new GameObject("PortraitContainer", typeof(RectTransform));
            pContGo.transform.SetParent(portraitBox, false);
            pContainer = pContGo.transform;

            RectTransform pContRt = pContGo.GetComponent<RectTransform>();
            pContRt.anchorMin = new Vector2(0.5f, 0.5f);
            pContRt.anchorMax = new Vector2(0.5f, 0.5f);
            pContRt.pivot = new Vector2(0.5f, 0.5f);
            pContRt.sizeDelta = new Vector2(80f, 80f);
            pContRt.anchoredPosition = Vector2.zero;
        }

        if (portraitUI != null)
        {
            portraitUI.PortraitContainer = pContainer as RectTransform;
        }

        // B. Kanan Atas: Nama (Yanuar / EmployeeName) & Divisi Badge
        RectTransform infoCol = GetOrCreateChildSection(headerRow, "InfoColumn", 88f);
        infoCol.sizeDelta = new Vector2(268f, 88f);
        if (!infoCol.TryGetComponent<VerticalLayoutGroup>(out var infoVlg))
        {
            infoVlg = infoCol.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        infoVlg.padding = new RectOffset(2, 0, 4, 4);
        infoVlg.spacing = 4f;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = false;
        infoVlg.childForceExpandWidth = true;
        infoVlg.childForceExpandHeight = false;
        infoVlg.childAlignment = TextAnchor.MiddleLeft;

        if (nameText != null)
        {
            nameText.transform.SetParent(infoCol, false);
            nameText.rectTransform.sizeDelta = new Vector2(260f, 30f);
            nameText.color = Color.white;
            nameText.fontSize = 22f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
        }

        if (divisionText != null)
        {
            divisionText.transform.SetParent(infoCol, false);
            divisionText.rectTransform.sizeDelta = new Vector2(260f, 22f);
            divisionText.color = Color.white;
            divisionText.fontSize = 13.5f;
            divisionText.fontStyle = FontStyles.Bold;
            divisionText.alignment = TextAlignmentOptions.Left;
        }

        // C. Kanan Pojok: Tombol Close (✕)
        RectTransform closeHolder = GetOrCreateChildSection(headerRow, "CloseHolder", 88f);
        closeHolder.sizeDelta = new Vector2(26f, 88f);
        if (!closeHolder.TryGetComponent<VerticalLayoutGroup>(out var closeVlg))
        {
            closeVlg = closeHolder.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        closeVlg.childControlWidth = false;
        closeVlg.childControlHeight = false;
        closeVlg.childForceExpandWidth = false;
        closeVlg.childForceExpandHeight = false;
        closeVlg.childAlignment = TextAnchor.UpperRight;

        if (closeButton != null)
        {
            closeButton.transform.SetParent(closeHolder, false);
            StyleCloseButton(closeButton);
        }

        //=========================================
        // 3. DIVIDER LINE
        //=========================================
        Transform divider = panel.Find("Section_Divider");
        if (divider == null)
        {
            GameObject divGo = new GameObject("Section_Divider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(panel, false);
            divider = divGo.transform;

            RectTransform divRt = divGo.GetComponent<RectTransform>();
            divRt.sizeDelta = new Vector2(0f, 1.5f);

            Image divImg = divGo.GetComponent<Image>();
            divImg.color = new Color(0.20f, 0.26f, 0.38f, 0.75f);
        }
        divider.SetSiblingIndex(1);

        //=========================================
        // 4. BAWAH: "INFO LAIN" (Label + Stats HP & Mood + Actions)
        //=========================================
        RectTransform infoLainSection = GetOrCreateChildSection(panel, "Section_InfoLain", 95f);
        infoLainSection.SetSiblingIndex(2);
        if (!infoLainSection.TryGetComponent<VerticalLayoutGroup>(out var infoLainVlg))
        {
            infoLainVlg = infoLainSection.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        infoLainVlg.padding = new RectOffset(0, 0, 0, 0);
        infoLainVlg.spacing = 6f;
        infoLainVlg.childControlWidth = true;
        infoLainVlg.childControlHeight = false;
        infoLainVlg.childForceExpandWidth = true;
        infoLainVlg.childForceExpandHeight = false;
        infoLainVlg.childAlignment = TextAnchor.UpperLeft;

        // A. Header Teks "INFO LAIN"
        Transform infoLainTitle = infoLainSection.Find("InfoLainLabel");
        if (infoLainTitle == null)
        {
            GameObject labelGo = new GameObject("InfoLainLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(infoLainSection, false);
            infoLainTitle = labelGo.transform;

            TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.text = "INFO LAIN";
            labelTmp.fontSize = 11.5f;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = new Color(0.55f, 0.65f, 0.80f, 1f);
            labelTmp.alignment = TextAlignmentOptions.Left;

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(0f, 16f);
        }
        infoLainTitle.SetSiblingIndex(0);

        // B. Row Kartu Stat (HP & Mood)
        RectTransform statsRow = GetOrCreateChildSection(infoLainSection, "StatsRow", 36f);
        statsRow.SetSiblingIndex(1);
        if (!statsRow.TryGetComponent<HorizontalLayoutGroup>(out var statsHlg))
        {
            statsHlg = statsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        statsHlg.padding = new RectOffset(0, 0, 0, 0);
        statsHlg.spacing = 10f;
        statsHlg.childControlWidth = true;
        statsHlg.childControlHeight = true;
        statsHlg.childForceExpandWidth = true;
        statsHlg.childForceExpandHeight = true;
        statsHlg.childAlignment = TextAnchor.MiddleCenter;

        // Kartu HP
        RectTransform hpCard = GetOrCreateStatCard(statsRow, "HpCard");
        if (hpText != null)
        {
            hpText.transform.SetParent(hpCard, false);
            hpText.fontSize = 13.5f;
            hpText.color = Color.white;
            hpText.alignment = TextAlignmentOptions.Center;
        }

        // Kartu Mood
        RectTransform moodCard = GetOrCreateStatCard(statsRow, "MoodCard");
        if (moodText != null)
        {
            moodText.transform.SetParent(moodCard, false);
            moodText.fontSize = 13.5f;
            moodText.color = Color.white;
            moodText.alignment = TextAlignmentOptions.Center;
        }

        // C. Row Tombol Aksi (Heal, Care, Return) jika aktif
        RectTransform actionsRow = GetOrCreateChildSection(infoLainSection, "ActionsRow", 30f);
        actionsRow.SetSiblingIndex(2);
        if (!actionsRow.TryGetComponent<HorizontalLayoutGroup>(out var actHlg))
        {
            actHlg = actionsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        actHlg.padding = new RectOffset(0, 0, 0, 0);
        actHlg.spacing = 8f;
        actHlg.childControlWidth = false;
        actHlg.childControlHeight = false;
        actHlg.childForceExpandWidth = false;
        actHlg.childForceExpandHeight = false;
        actHlg.childAlignment = TextAnchor.MiddleRight;

        if (healSickButton != null)
        {
            healSickButton.transform.SetParent(actionsRow, false);
            StyleModernButton(healSickButton, new Color(0.06f, 0.58f, 0.40f), new Color(0.10f, 0.72f, 0.50f), "✦ HEAL");
        }

        if (takeCareButton != null)
        {
            takeCareButton.transform.SetParent(actionsRow, false);
            StyleModernButton(takeCareButton, new Color(0.48f, 0.28f, 0.88f), new Color(0.58f, 0.38f, 0.98f), "❤ CARE");
        }

        if (backToDivisionButton != null)
        {
            backToDivisionButton.transform.SetParent(actionsRow, false);
            StyleModernButton(backToDivisionButton, new Color(0.85f, 0.52f, 0.12f), new Color(0.95f, 0.62f, 0.22f), "↩ RETURN");
        }

        //=========================================
        // 5. Cleanup sisa child lama yang tidak terpakai
        //=========================================
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child.name != "Section_Header" && 
                child.name != "Section_Divider" && 
                child.name != "Section_InfoLain")
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static RectTransform GetOrCreateChildSection(RectTransform parent, string name, float height)
    {
        Transform found = parent.Find(name);
        if (found == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            found = go.transform;
        }

        found.gameObject.SetActive(true);
        RectTransform rt = found as RectTransform;
        rt.sizeDelta = new Vector2(0f, height);
        return rt;
    }

    private static RectTransform GetOrCreateStatCard(RectTransform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            found = go.transform;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f); // Sleek dark card pill

            Outline outl = go.GetComponent<Outline>();
            outl.effectColor = new Color(0.22f, 0.28f, 0.40f, 0.65f);
            outl.effectDistance = new Vector2(1f, -1f);

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childAlignment = TextAnchor.MiddleCenter;
        }

        found.gameObject.SetActive(true);
        return found as RectTransform;
    }

    private static void StyleCloseButton(Button btn)
    {
        if (btn == null) return;
        var rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(26f, 26f);
        }

        Color normal = new Color(0.18f, 0.22f, 0.32f, 0.9f);
        Color hover = new Color(0.88f, 0.22f, 0.22f, 1f);

        if (btn.TryGetComponent<Image>(out var img))
        {
            img.color = normal;
        }

        var cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = hover * 0.8f;
        cb.selectedColor = normal;
        btn.colors = cb;

        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = "✕";
            tmp.fontSize = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void StyleModernButton(Button btn, Color bgNormal, Color bgHover, string labelText)
    {
        if (btn == null) return;

        var rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(95f, 28f);
        }

        if (btn.TryGetComponent<Image>(out var img))
        {
            img.color = bgNormal;
            img.raycastTarget = true;
        }

        var cb = btn.colors;
        cb.normalColor = bgNormal;
        cb.highlightedColor = bgHover;
        cb.pressedColor = bgHover * 0.8f;
        cb.selectedColor = bgNormal;
        cb.disabledColor = new Color(bgNormal.r, bgNormal.g, bgNormal.b, 0.35f);
        btn.colors = cb;

        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            if (!string.IsNullOrEmpty(labelText)) tmp.text = labelText;
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}