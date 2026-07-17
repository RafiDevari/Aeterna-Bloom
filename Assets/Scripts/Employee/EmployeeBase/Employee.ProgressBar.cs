using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bagian Employee yang mengurus progress bar visual di atas kepala employee,
/// ditampilkan otomatis setiap kali employee masuk ke state yang terdaftar di
/// ProgressStates (default: Feeding, Researching).
///
/// PENTING -- ini SENGAJA dibuat 100% procedural (SpriteRenderer + Texture 1x1
/// yang di-generate lewat kode saat runtime), BUKAN lewat prefab yang harus
/// di-assign manual satu-satu di tiap Employee prefab (EmployeeBotanist,
/// EmployeeResearcher, dst).
///
/// Kenapa bukan prefab: employee di-spawn otomatis oleh DivisionRoom dari
/// banyak prefab berbeda (lihat DivisionRoom.SpawnEmployees / employeesToSpawn),
/// dan ada banyak child class Employee. Kalau progress bar butuh field prefab
/// yang harus diisi manual di tiap prefab itu, gampang lupa/salah dan tidak
/// scalable. Dengan pendekatan ini, progress bar otomatis ada di SEMUA
/// Employee (base class maupun child class manapun, sekarang atau nanti)
/// tanpa setup apapun di Inspector -- cukup inherit dari Employee.
///
/// Cara pakai untuk sistem baru (mis. Harvest nanti) :
/// 1. Di method yang memulai aksi (mis. TryHarvest), setelah aksi dipastikan
///    SUKSES dimulai, panggil SetActionDuration(finalDurationInSeconds).
/// 2. Task yang bersangkutan tetap panggil employee.SetState(EmployeeState.XXX)
///    seperti pola FeedMonsterTask/ResearchMonsterTask -- progress bar otomatis
///    muncul & mulai menghitung dari titik state itu berubah.
/// 3. Daftarkan EmployeeState.XXX sebagai progress state, salah satu dari:
///    - tambahkan langsung ke HashSet ProgressStates di bawah (kalau nama
///      enum-nya sudah pasti), atau
///    - panggil Employee.RegisterProgressState(EmployeeState.XXX) dari luar
///      (mis. sekali saja di titik bootstrap game) supaya file ini tidak
///      perlu diedit ulang tiap ada sistem baru.
/// </summary>
public partial class Employee
{
    //────────────────────────────────────────────────────────
    // Progress state registry
    //────────────────────────────────────────────────────────

    private static readonly HashSet<EmployeeState> ProgressStates = new HashSet<EmployeeState>
    {
        EmployeeState.Feeding,
        EmployeeState.Researching,
        EmployeeState.Harvesting,
        EmployeeState.TakingStock,
        EmployeeState.FixingElectricity
    };

    /// <summary>Daftarkan state tambahan yang harus menampilkan progress bar (mis. Harvesting nanti).</summary>
    public static void RegisterProgressState(EmployeeState state) => ProgressStates.Add(state);

    //────────────────────────────────────────────────────────
    // Shared sprite -- SATU texture dipakai bareng oleh SEMUA employee,
    // bukan di-generate ulang per instance.
    //────────────────────────────────────────────────────────

    private static Sprite _barPixelSprite;
    private static Sprite BarPixelSprite
    {
        get
        {
            if (_barPixelSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();

                // pivot (0, 0.5) -> origin sprite ada di TEPI KIRI, tengah tinggi.
                // Supaya localScale.x bisa langsung dipakai buat "mengisi" bar dari
                // kiri ke kanan tanpa perlu geser posisi tiap frame.
                _barPixelSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
            }

            return _barPixelSprite;
        }
    }

    //────────────────────────────────────────────────────────
    // Config (boleh diubah per prefab lewat Inspector kalau mau beda ukuran/warna,
    // TAPI bukan wajib diisi -- ada default yang jalan tanpa disentuh sama sekali)
    //────────────────────────────────────────────────────────

    [Header("Progress Bar")]
    [SerializeField] private float progressBarWidth = 0.6f;
    [SerializeField] private float progressBarHeight = 0.08f;
    [SerializeField] private float progressBarYOffset = 0.7f;
    [SerializeField] private Color progressBarBackgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color progressBarFillColor = new Color(0.2f, 0.85f, 0.3f, 1f);

    //────────────────────────────────────────────────────────
    // Runtime state
    //────────────────────────────────────────────────────────

    private GameObject progressBarRoot;
    private Transform progressFillTransform;
    private float actionDuration;
    private float actionStartTime;

    /// <summary>Progress 0..1 dari aksi timed yang sedang berjalan (Feeding/Researching/dst).</summary>
    public float ActionProgress01
    {
        get
        {
            if (actionDuration <= 0f)
                return 1f;

            return Mathf.Clamp01((Time.time - actionStartTime) / actionDuration);
        }
    }

    /// <summary>
    /// Dipanggil dari sistem yang memulai aksi timed (FeedMonster, TryResearch, nanti
    /// TryHarvest dst) TEPAT SAAT aksi itu dipastikan sukses -- simpan durasi finalnya.
    /// actionStartTime baru benar-benar di-set saat state berubah ke salah satu
    /// ProgressStates (lihat HandleStateChangedForProgressBar), jadi urutan panggil
    /// SetActionDuration vs SetState tidak perlu presisi selama masih di frame yang sama.
    /// </summary>
    protected internal void SetActionDuration(float duration)
    {
        actionDuration = duration;
    }

    //────────────────────────────────────────────────────────
    // Setup
    //────────────────────────────────────────────────────────

    // Awake() belum dipakai di file Employee manapun (Employee.cs pakai Start()),
    // jadi aman ditambahkan di sini tanpa bentrok / tanpa perlu edit file lain.
    //
    // CATATAN: bar TIDAK dibuat di sini. Bar dibuat on-demand (lazy) tepat saat
    // dibutuhkan (masuk ProgressStates) dan di-DESTROY (bukan cuma disembunyikan)
    // begitu keluar dari ProgressStates -- lihat HandleStateChangedForProgressBar.
    private void Awake()
    {
        OnStateChanged += HandleStateChangedForProgressBar;
    }

    private void CreateProgressBarInstance()
    {
        progressBarRoot = new GameObject("ProgressBar");
        progressBarRoot.transform.SetParent(transform, false);
        progressBarRoot.transform.localPosition = new Vector3(0f, progressBarYOffset, 0f);

        float left = -progressBarWidth * 0.5f;

        // Background -- lebar penuh, statis, cuma dibuat sekali.
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(progressBarRoot.transform, false);
        bgGO.transform.localPosition = new Vector3(left, 0f, 0f);
        bgGO.transform.localScale = new Vector3(progressBarWidth, progressBarHeight, 1f);

        var bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = BarPixelSprite;
        bgRenderer.color = progressBarBackgroundColor;
        bgRenderer.sortingOrder = 5000;

        // Fill -- di-scale tiap frame sesuai progress oleh ProgressBarFillUpdater.
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(progressBarRoot.transform, false);
        fillGO.transform.localPosition = new Vector3(left, 0f, 0f);
        fillGO.transform.localScale = new Vector3(0f, progressBarHeight, 1f);

        var fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = BarPixelSprite;
        fillRenderer.color = progressBarFillColor;
        fillRenderer.sortingOrder = 5001;

        progressFillTransform = fillGO.transform;

        // Komponen kecil terpisah yang jalan sendiri tiap frame SELAMA root aktif --
        // sengaja dipisah dari Employee.Update() yang sudah ada supaya file Employee.cs
        // tidak perlu diubah, dan supaya nol biaya waktu bar sedang disembunyikan
        // (GameObject inactive -> Update()-nya tidak dipanggil Unity sama sekali).
        var updater = progressBarRoot.AddComponent<ProgressBarFillUpdater>();
        updater.Owner = this;
        updater.FillTransform = progressFillTransform;
        updater.FullWidth = progressBarWidth;
        updater.Height = progressBarHeight;

        // Dibuat langsung dalam keadaan aktif -- CreateProgressBarInstance() cuma
        // dipanggil dari HandleStateChangedForProgressBar TEPAT saat sedang butuh.
    }

    private void HandleStateChangedForProgressBar(EmployeeState newState)
    {
        if (ProgressStates.Contains(newState))
        {
            actionStartTime = Time.time;

            if (progressBarRoot == null)
            {
                // Belum ada instance (mis. baru pertama kali action, atau instance
                // sebelumnya sudah di-destroy) -> bikin baru.
                CreateProgressBarInstance();
            }
            // Kalau sudah ada (mis. lompat dari Feeding langsung ke Researching tanpa
            // sempat balik Idle), tinggal pakai ulang objeknya, cuma actionStartTime di-reset.
        }
        else
        {
            // Selesai / dibatalkan -- BENAR-BENAR destroy, bukan cuma disembunyikan,
            // supaya tidak ada kemungkinan bar "nyangkut" ketinggalan aktif.
            if (progressBarRoot != null)
            {
                Destroy(progressBarRoot);
                progressBarRoot = null;
                progressFillTransform = null;
            }

            actionDuration = 0f;
        }
    }

    /// <summary>
    /// Hidup di GameObject "ProgressBar" (child employee). Satu-satunya tugasnya
    /// update lebar Fill tiap frame selagi bar sedang aktif/terlihat.
    /// </summary>
    private class ProgressBarFillUpdater : MonoBehaviour
    {
        public Employee Owner;
        public Transform FillTransform;
        public float FullWidth;
        public float Height;

        private void Update()
        {
            if (Owner == null || FillTransform == null)
                return;

            float progress = Owner.ActionProgress01;
            FillTransform.localScale = new Vector3(FullWidth * progress, Height, 1f);
        }
    }
}