using UnityEngine;

/// <summary>
/// Mengatur popup mana yang sedang terbuka.
/// Memastikan cuma ada 1 popup aktif dalam satu waktu — kalau popup baru
/// dibuka, popup lama otomatis ditutup dulu.
///
/// Taruh 1 GameObject dengan component ini di scene (misal di Canvas utama).
/// </summary>
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    private PopupBase currentPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RequestOpen(PopupBase popup)
    {
        if (currentPopup != null && currentPopup != popup)
        {
            currentPopup.Close();
        }

        currentPopup = popup;
    }

    public void NotifyClosed(PopupBase popup)
    {
        if (currentPopup == popup)
            currentPopup = null;
    }

    public void CloseCurrent()
    {
        currentPopup?.Close();
        currentPopup = null;
    }
}