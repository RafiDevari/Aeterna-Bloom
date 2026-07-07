using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class untuk semua popup UI (ContainmentUnit, Monster, Employee, dst).
///
/// PENTING - taruh component turunan PopupBase ini di GameObject yang SELALU
/// AKTIF (misalnya di Canvas langsung, atau GameObject controller kosong yang
/// tidak pernah di-nonaktifkan). JANGAN taruh script ini di GameObject visual
/// popup yang ikut di-SetActive(false) sendiri lewat field popupRoot --
/// GameObject yang nonaktif tidak bisa menjalankan Coroutine, dan itu
/// dibutuhkan untuk fix bug "buka lalu langsung nutup" di bawah ini.
///
/// popupRoot = GameObject visual (Overlay + kotak popup) yang di-toggle
/// aktif/nonaktif. Wajib GameObject LAIN, bukan GameObject script ini sendiri.
/// </summary>
public abstract class PopupBase : MonoBehaviour
{
    [Header("Popup Base")]
    [Tooltip("GameObject visual (Overlay + popup box) yang di-enable/disable. Harus GameObject LAIN, bukan GameObject script ini sendiri.")]
    [SerializeField] protected GameObject popupRoot;

    [Header("Overlay (opsional)")]
    [Tooltip("Button transparan full-layar di belakang popup box. Diklik = popup close (klik di luar area popup).")]
    [SerializeField] private Button overlayButton;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private Coroutine openRoutine;

    protected virtual void Awake()
    {
        if (popupRoot == null)
        {
            Debug.LogError($"[{name}] popupRoot belum di-assign di Inspector!");
            return;
        }

        if (overlayButton != null)
            overlayButton.onClick.AddListener(Close);

        popupRoot.SetActive(false);
    }

    public virtual void Open()
    {
        if (PopupManager.Instance != null)
            PopupManager.Instance.RequestOpen(this);

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenNextFrame());
    }

    private IEnumerator OpenNextFrame()
    {
        // Tunggu 1 frame sebelum benar-benar menampilkan popup.
        //
        // Kenapa: klik yang men-trigger Open() ini (mouse-down di
        // ContainmentUnit) diproses SEBELUM sistem UI Unity sempat
        // mengecek "apa yang lagi ditekan" di frame yang sama. Kalau
        // Overlay langsung aktif di frame itu juga, sistem UI keburu
        // menganggap Overlay itu targetnya. Begitu mouse dilepas
        // (walau di frame berikutnya), itu dianggap klik yang sah ke
        // Overlay -> popup nutup sendiri padahal baru saja dibuka.
        //
        // Menunda 1 frame memastikan Overlay belum ada sama sekali
        // saat momen "tekan" dari klik pembuka ini diproses.
        yield return null;

        popupRoot.SetActive(true);
        openRoutine = null;
        OnOpened();
    }

    public virtual void Close()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (PopupManager.Instance != null)
            PopupManager.Instance.NotifyClosed(this);

        OnClosed();
    }

    /// <summary>Dipanggil setiap kali popup ini benar-benar tampil.</summary>
    protected virtual void OnOpened() { }

    /// <summary>Dipanggil setiap kali popup ini ditutup.</summary>
    protected virtual void OnClosed() { }
}   