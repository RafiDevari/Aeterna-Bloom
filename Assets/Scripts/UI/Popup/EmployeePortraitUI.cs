using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan portrait "muka" employee di UI popup dengan cara merekonstruksi
/// komposit sprite kepala (Head + Hair + Eyes + Nose + Mouth + bagian aktif lainnya)
/// ke dalam UI Image yang ada di <see cref="portraitContainer"/>.
///
/// "Ngikutin kepala" = setiap frame, sprite & warna tiap bagian kepala di-refresh
/// dari SpriteRenderer asli di prefab employee, jadi kalau warna rambut diganti
/// (mis. buat tes lewat <see cref="CycleTestHairColor"/>), gambar di popup ikut
/// berubah real-time.
/// </summary>
public class EmployeePortraitUI : MonoBehaviour
{
    [Header("Portrait Container")]
    [Tooltip("Container RectTransform tempat portrait kepala dibangun. Beri ukuran tetap (mis. 100x100).")]
    [SerializeField] private RectTransform portraitContainer;

    [Header("Settings")]
    [Tooltip("Padding di sekitar portrait di dalam container (pixel).")]
    [SerializeField] private float padding = 6f;

    [Tooltip("Kalau true, update sprite & warna tiap frame supaya ngikutin perubahan (mis. warna rambut).")]
    [SerializeField] private bool liveUpdate = true;

    [Tooltip("Kalau true, flip portrait mengikuti arah hadap employee (facing left/right).")]
    [SerializeField] private bool followFacing = false;

    private Employee targetEmployee;
    private Transform cachedHeadTransform;
    private readonly List<SpriteRenderer> trackedRenderers = new();
    private readonly List<Image> portraitImages = new();
    private bool needsRebuild;

    // Cache facing untuk deteksi perubahan
    private bool cachedFacingLeft;

    // Cache warna & sprite untuk deteksi perubahan live
    private readonly List<Color> cachedColors = new();
    private readonly List<Sprite> cachedSprites = new();

    public RectTransform PortraitContainer
    {
        get => portraitContainer;
        set => portraitContainer = value;
    }

    //=========================================
    // Test Hair Colors (untuk tes ganti warna rambut)
    //=========================================
    private static readonly Color[] testHairColors =
    {
        Color.white,                                      // asli / putih
        new Color(0.15f, 0.12f, 0.10f),                   // hitam
        new Color(0.55f, 0.32f, 0.15f),                   // coklat
        new Color(0.95f, 0.78f, 0.20f),                   // pirang emas
        new Color(0.90f, 0.25f, 0.25f),                   // merah menyala
        new Color(0.20f, 0.60f, 0.95f),                   // biru neon
        new Color(0.30f, 0.85f, 0.40f),                   // hijau emerald
        new Color(0.85f, 0.35f, 0.85f),                   // ungu magenta
        new Color(1.00f, 0.55f, 0.20f),                   // oranye cerah
        new Color(0.20f, 0.85f, 0.85f),                   // cyan aqua
    };
    private int testColorIndex;

    public bool HasEmployee => targetEmployee != null;

    /// <summary>
    /// Set employee yang portrait-nya mau ditampilkan. Langsung rebuild portrait.
    /// </summary>
    public void SetEmployee(Employee employee)
    {
        targetEmployee = employee;
        testColorIndex = 0;
        needsRebuild = true;
        RebuildPortrait();
    }

    /// <summary>
    /// Bersihkan portrait (dipanggil saat popup ditutup).
    /// </summary>
    public void Clear()
    {
        targetEmployee = null;
        cachedHeadTransform = null;
        trackedRenderers.Clear();
        cachedColors.Clear();
        cachedSprites.Clear();
        ClearContainer();
    }

    /// <summary>
    /// Tes: ganti warna rambut employee target ke warna berikutnya.
    /// Panggil dari tombol test atau debug key (mis. F8) di EmployeePopup.
    /// </summary>
    public void CycleTestHairColor()
    {
        if (targetEmployee == null) return;
        var appearance = targetEmployee.Appearance;
        if (appearance == null || appearance.HairRenderer == null)
        {
            Debug.LogWarning("[EmployeePortraitUI] Tidak bisa ganti hair color: HairRenderer tidak ada.");
            return;
        }

        testColorIndex = (testColorIndex + 1) % testHairColors.Length;
        Color c = testHairColors[testColorIndex];
        appearance.SetHairColor(c);
        Debug.Log($"[EmployeePortraitUI] Ganti warna rambut {targetEmployee.EmployeeName} -> {c}");

        // Segera refresh portrait
        RefreshPortrait();
    }

    private void Update()
    {
        if (targetEmployee == null) return;

        if (needsRebuild)
        {
            RebuildPortrait();
            return;
        }

        if (liveUpdate)
        {
            RefreshPortrait();
        }
    }

    //=========================================
    // Portrait Building
    //=========================================

    /// <summary>
    /// Membangun ulang struktur UI Image berdasarkan SpriteRenderer aktif di bawah Head.
    /// </summary>
    public void RebuildPortrait()
    {
        ClearContainer();
        trackedRenderers.Clear();
        portraitImages.Clear();
        cachedColors.Clear();
        cachedSprites.Clear();

        if (targetEmployee == null)
        {
            needsRebuild = false;
            return;
        }

        EnsureContainer();

        if (portraitContainer == null)
        {
            needsRebuild = false;
            return;
        }

        cachedHeadTransform = FindHeadTransform(targetEmployee);
        if (cachedHeadTransform == null)
        {
            Debug.LogWarning($"[EmployeePortraitUI] Head transform tidak ditemukan untuk {targetEmployee.EmployeeName}");
            needsRebuild = false;
            return;
        }

        CollectRenderers(cachedHeadTransform, trackedRenderers);

        if (trackedRenderers.Count == 0)
        {
            needsRebuild = false;
            return;
        }

        // Sort by sortingOrder supaya draw order di UI benar (belakang ke depan)
        trackedRenderers.Sort((a, b) =>
        {
            int cmp = a.sortingOrder.CompareTo(b.sortingOrder);
            if (cmp != 0) return cmp;
            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        });

        // Cek ukuran container — kalau belum di-layout (0), tunggu frame berikutnya
        Vector2 containerSize = portraitContainer.rect.size;
        if (containerSize.x < 1f || containerSize.y < 1f)
        {
            needsRebuild = true;
            return;
        }

        needsRebuild = false;

        // Hitung combined bounds di local space Head
        Bounds combined = ComputeCombinedBounds(cachedHeadTransform, trackedRenderers);
        if (combined.size.x < 0.001f || combined.size.y < 0.001f)
        {
            return;
        }

        // Hitung scale untuk fit container (maintain aspect ratio)
        float availW = Mathf.Max(1f, containerSize.x - padding * 2);
        float availH = Mathf.Max(1f, containerSize.y - padding * 2);
        float scaleX = availW / combined.size.x;
        float scaleY = availH / combined.size.y;
        float scale = Mathf.Min(scaleX, scaleY);

        // Cache facing awal
        cachedFacingLeft = IsFacingLeft(targetEmployee);
        if (followFacing)
        {
            portraitContainer.localScale = new Vector3(cachedFacingLeft ? -1f : 1f, 1f, 1f);
        }
        else
        {
            portraitContainer.localScale = Vector3.one;
        }

        // Buat UI Image per renderer
        Vector3 headLossyScale = cachedHeadTransform.lossyScale;
        float absHeadScaleX = Mathf.Max(0.0001f, Mathf.Abs(headLossyScale.x));
        float absHeadScaleY = Mathf.Max(0.0001f, Mathf.Abs(headLossyScale.y));

        for (int i = 0; i < trackedRenderers.Count; i++)
        {
            SpriteRenderer sr = trackedRenderers[i];
            if (sr == null || sr.sprite == null) continue;

            GameObject go = new GameObject($"Portrait_{sr.name}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(portraitContainer, false);

            Image img = go.GetComponent<Image>();
            img.sprite = sr.sprite;
            img.color = sr.color;
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Posisi: offset dari combined center di local space Head, di-scale ke UI pixels
            Vector3 partCenterLocal = cachedHeadTransform.InverseTransformPoint(sr.bounds.center);
            Vector3 offset = partCenterLocal - combined.center;
            float posX = offset.x * scale;
            float posY = offset.y * scale;

            rt.anchoredPosition = new Vector2(posX, posY);

            // Size: sprite bounds (local) * scale
            Vector2 localSize = new Vector2(
                sr.bounds.size.x / absHeadScaleX,
                sr.bounds.size.y / absHeadScaleY
            );
            rt.sizeDelta = new Vector2(localSize.x * scale, localSize.y * scale);

            // Handle flip
            float flipScaleX = sr.flipX ? -1f : 1f;
            float flipScaleY = sr.flipY ? -1f : 1f;
            rt.localScale = new Vector3(flipScaleX, flipScaleY, 1f);

            portraitImages.Add(img);
            cachedColors.Add(sr.color);
            cachedSprites.Add(sr.sprite);
        }
    }

    /// <summary>
    /// Refresh sprite & warna tiap UI Image dari SpriteRenderer asli.
    /// Dipanggil tiap frame supaya portrait "ngikutin" perubahan warna rambut / ekspresi.
    /// </summary>
    public void RefreshPortrait()
    {
        if (cachedHeadTransform == null || targetEmployee == null) return;

        // Deteksi perubahan facing
        if (followFacing)
        {
            bool facingLeft = IsFacingLeft(targetEmployee);
            if (facingLeft != cachedFacingLeft)
            {
                cachedFacingLeft = facingLeft;
                if (portraitContainer != null)
                {
                    portraitContainer.localScale = new Vector3(cachedFacingLeft ? -1f : 1f, 1f, 1f);
                }
            }
        }

        // Cek apakah jumlah renderer atau struktur berubah
        if (portraitImages.Count != trackedRenderers.Count)
        {
            RebuildPortrait();
            return;
        }

        for (int i = 0; i < portraitImages.Count && i < trackedRenderers.Count; i++)
        {
            Image img = portraitImages[i];
            SpriteRenderer sr = trackedRenderers[i];
            if (img == null || sr == null)
            {
                RebuildPortrait();
                return;
            }

            // Update sprite kalau berubah
            if (img.sprite != sr.sprite)
            {
                img.sprite = sr.sprite;
                cachedSprites[i] = sr.sprite;
            }

            // Update color (mis. hair color diganti)
            if (img.color != sr.color)
            {
                img.color = sr.color;
                cachedColors[i] = sr.color;
            }

            // Update active state
            bool shouldBeActive = sr.gameObject.activeInHierarchy && sr.enabled && sr.sprite != null;
            if (img.gameObject.activeSelf != shouldBeActive)
            {
                img.gameObject.SetActive(shouldBeActive);
            }
        }
    }

    //=========================================
    // Head Finding
    //=========================================

    /// <summary>
    /// Cari Transform "Head" dari employee. Prioritas:
    /// 1. Lewat EmployeeAppearance.HeadRenderer
    /// 2. Cari child bernama "Head" secara recursive (Botanist, Researcher, dll.)
    /// 3. Fallback: jika prefab sederhana tanpa objek Head terpisah, gunakan transform employee yang memiliki SpriteRenderer.
    /// </summary>
    private Transform FindHeadTransform(Employee employee)
    {
        if (employee == null) return null;

        // Coba lewat Appearance.HeadRenderer
        var appearance = employee.Appearance;
        if (appearance != null && appearance.HeadRenderer != null)
        {
            return appearance.HeadRenderer.transform;
        }

        // Fallback 1: cari by name "Head"
        Transform head = FindChildRecursive(employee.transform, "Head");
        if (head != null) return head;

        // Fallback 2: dukung semua employee / prefab sederhana yang punya SpriteRenderer langsung
        var sr = employee.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return employee.transform;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    //=========================================
    // Renderer Collection & Bounds
    //=========================================

    /// <summary>
    /// Kumpulkan semua SpriteRenderer aktif di bawah Head (Head sendiri + Hair + Eyes + Nose + Mouth + dll).
    /// </summary>
    private void CollectRenderers(Transform headTransform, List<SpriteRenderer> list)
    {
        // Head sendiri
        var headSR = headTransform.GetComponent<SpriteRenderer>();
        if (headSR != null && headSR.enabled && headSR.sprite != null)
            list.Add(headSR);

        // Children langsung & bertingkat yang aktif
        CollectRenderersRecursive(headTransform, list);
    }

    private void CollectRenderersRecursive(Transform parent, List<SpriteRenderer> list)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled && sr.sprite != null && !list.Contains(sr))
            {
                list.Add(sr);
            }

            if (child.childCount > 0)
            {
                CollectRenderersRecursive(child, list);
            }
        }
    }

    /// <summary>
    /// Hitung combined bounds dari semua renderer di local space Head.
    /// </summary>
    private Bounds ComputeCombinedBounds(Transform headTransform, List<SpriteRenderer> renderers)
    {
        Bounds combined = new Bounds(Vector3.zero, Vector3.zero);
        bool initialized = false;
        Vector3 headLossyScale = headTransform.lossyScale;

        float absHeadX = Mathf.Max(0.0001f, Mathf.Abs(headLossyScale.x));
        float absHeadY = Mathf.Max(0.0001f, Mathf.Abs(headLossyScale.y));
        float absHeadZ = Mathf.Max(0.0001f, Mathf.Abs(headLossyScale.z));

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;

            Vector3 center = headTransform.InverseTransformPoint(sr.bounds.center);
            Vector3 size = new Vector3(
                sr.bounds.size.x / absHeadX,
                sr.bounds.size.y / absHeadY,
                sr.bounds.size.z / absHeadZ
            );

            Bounds b = new Bounds(center, size);
            if (!initialized)
            {
                combined = b;
                initialized = true;
            }
            else
            {
                combined.Encapsulate(b);
            }
        }

        return combined;
    }

    //=========================================
    // Facing Detection
    //=========================================

    private static bool IsFacingLeft(Employee employee)
    {
        if (employee == null) return false;

        if (employee.transform.lossyScale.x < 0f) return true;

        Transform visuals = employee.transform.Find("Visuals");
        if (visuals != null && visuals.lossyScale.x < 0f) return true;

        return false;
    }

    //=========================================
    // Container Management
    //=========================================

    private void EnsureContainer()
    {
        if (portraitContainer != null) return;

        // Coba cari child bernama "PortraitContainer" atau "Portrait"
        Transform found = transform.Find("PortraitContainer");
        if (found == null) found = transform.Find("Portrait");
        if (found != null && found is RectTransform rt)
        {
            portraitContainer = rt;
            return;
        }

        // Kalau transform ini sendiri adalah RectTransform dan cocok
        if (transform is RectTransform thisRt && thisRt.sizeDelta.x > 20 && thisRt.sizeDelta.y > 20)
        {
            portraitContainer = thisRt;
            return;
        }

        // Buat GameObject baru sebagai container
        GameObject go = new GameObject("PortraitContainer", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        portraitContainer = go.GetComponent<RectTransform>();
        portraitContainer.anchorMin = new Vector2(0.5f, 0.5f);
        portraitContainer.anchorMax = new Vector2(0.5f, 0.5f);
        portraitContainer.pivot = new Vector2(0.5f, 0.5f);
        portraitContainer.sizeDelta = new Vector2(100f, 100f);
        portraitContainer.anchoredPosition = Vector2.zero;
    }

    private void ClearContainer()
    {
        if (portraitContainer == null) return;
        for (int i = portraitContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(portraitContainer.GetChild(i).gameObject);
        }
        portraitImages.Clear();
    }

    private void OnDestroy()
    {
        ClearContainer();
    }
}