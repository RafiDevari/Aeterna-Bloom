using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Partial class Employee yang menangani mekanik bersosialisasi/mengobrol antar employee saat idle.
/// Fitur Lengkap:
/// 1. Berjalan halus (menggunakan animasi jalan) saat melakukan penyesuaian posisi jarak X.
/// 2. Cooldown sosial setelah baru tiba dari pergerakan/job agar posisi mengendap halus dulu.
/// 3. Jika salah satu employee diberi job, obrolan partner langsung dibatalkan seketika.
/// 4. Memilih & membentuk GRUP SOSIAL otomatis jika ada lebih dari 2 employee (multi-employee support).
/// </summary>
public partial class Employee
{
    [Header("Social & Conversation Settings")]
    [Tooltip("Apakah employee dapat bersosialisasi/mengobrol saat idle.")]
    [SerializeField] private bool enableSocializing = true;

    [Tooltip("Jarak X ideal antar employee saat mengobrol.")]
    [SerializeField] private float conversationDistance = 0.85f;

    [Tooltip("Durasi mengobrol (xxx detik) yang dibutuhkan untuk menambah Mood +1.")]
    [SerializeField] private float conversationMoodDuration = 5f;

    [Tooltip("Cooldown sosial (detik) setelah tiba dari pergerakan/job sebelum mulai bersosialisasi.")]
    [SerializeField] private float postMoveSocialCooldown = 1.2f;

    private float conversationTimer = 0f;
    private float socialCooldownTimer = 0f;
    private readonly List<Employee> socialGroupMembers = new List<Employee>();

    public void SetSocialCooldown(float duration)
    {
        socialCooldownTimer = duration;
    }

    /// <summary>
    /// Dipanggil saat partner keluar dari obrolan (misal karena dapat job baru atau dipindahkan).
    /// </summary>
    public void OnPartnerLeftConversation(Employee partner)
    {
        socialGroupMembers.Remove(partner);
        if (socialGroupMembers.Count == 0)
        {
            EndConversation();
        }
    }

    /// <summary>
    /// Update harian logika sosial dari Employee.Update()
    /// </summary>
    private void UpdateSocializing()
    {
        // Tunggu cooldown setelah baru selesai bergerak
        if (socialCooldownTimer > 0f)
        {
            socialCooldownTimer -= Time.deltaTime;
            return;
        }

        if (!enableSocializing)
        {
            EndConversation();
            return;
        }

        // Tidak bersosialisasi jika mati, terhipnotis, atau tidur
        if (currentState == EmployeeState.Dead || currentState == EmployeeState.Hypnotized || currentState == EmployeeState.Sleeping)
        {
            EndConversation();
            return;
        }

        // Hanya bersosialisasi saat Idle/Conversing dan tidak sedang berjalan atau sibuk task
        if (IsBusy || isMoving)
        {
            EndConversation();
            return;
        }

        if (currentState != EmployeeState.Idle && currentState != EmployeeState.Conversing)
        {
            EndConversation();
            return;
        }

        // Cari semua employee lain di room yang sama yang sedang idle & eligible
        List<Employee> eligibleRoomEmployees = GetEligibleRoomEmployees();

        if (eligibleRoomEmployees.Count == 0)
        {
            EndConversation();
            return;
        }

        // Pembentukan Grup Sosial (Urutkan posisi X seluruh anggota idle di room)
        List<Employee> allGroup = new List<Employee>(eligibleRoomEmployees) { this };
        allGroup = allGroup.OrderBy(e => e.transform.position.x).ToList();

        int myIndex = allGroup.IndexOf(this);
        int totalGroup = allGroup.Count;

        // Hitung target posisi X ideal dalam barisan grup (berjarak X satu sama lain)
        float groupCenterX = allGroup.Average(e => e.transform.position.x);
        float startX = groupCenterX - ((totalGroup - 1) * conversationDistance * 0.5f);
        float targetX = startX + (myIndex * conversationDistance);
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);

        float distToTargetX = Mathf.Abs(transform.position.x - targetX);

        // KONDISI 1: Perlu penyesuaian posisi (jarak dari target barisan > 0.15m)
        if (distToTargetX > 0.15f)
        {
            if (currentState == EmployeeState.Conversing)
            {
                SetState(EmployeeState.Idle);
            }

            // MoveTowards dengan kecepatan berjalan halus
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * 0.5f * Time.deltaTime);

            // Set animasi & arah hadap berjalan
            bool walkLeft = targetPos.x < transform.position.x;
            var animator = GetComponent<EmployeeAnimator>();
            if (animator != null)
            {
                animator.SetFacingLeft(walkLeft);
            }

            conversationTimer = 0f;
            return;
        }

        // KONDISI 2: Berada pada posisi ideal -> Masuk ke state Conversing & Mengobrol!
        if (currentState != EmployeeState.Conversing)
        {
            SetState(EmployeeState.Conversing);
        }

        // Hadap ke titik pusat grup obrolan
        FaceTowardsPosition(new Vector3(groupCenterX, transform.position.y, transform.position.z));

        // Perbarui list anggota grup
        socialGroupMembers.Clear();
        foreach (var emp in eligibleRoomEmployees)
        {
            socialGroupMembers.Add(emp);
        }

        // Timer percakapan & Mood boost +1
        conversationTimer += Time.deltaTime;
        if (conversationTimer >= conversationMoodDuration)
        {
            conversationTimer = 0f;
            if (mood < maxMood)
            {
                ModifyMood(1);
                Debug.Log($"[{EmployeeName}] mengobrol dalam grup ({totalGroup} orang) selama {conversationMoodDuration}s -> Mood +1 ({mood}/{maxMood})");
            }
        }
    }

    private void FaceTowardsPosition(Vector3 targetPos)
    {
        bool faceLeft = (targetPos.x < transform.position.x);
        var animator = GetComponent<EmployeeAnimator>();
        if (animator != null)
        {
            animator.SetFacingLeft(faceLeft);
        }
    }

    private List<Employee> GetEligibleRoomEmployees()
    {
        List<Employee> result = new List<Employee>();
        if (Facility.Instance == null) return result;

        Room myRoom = GetCurrentRoom();

        foreach (var other in Facility.Instance.Employees)
        {
            if (other == null || other == this) continue;
            if (other.currentState == EmployeeState.Dead || other.currentState == EmployeeState.Hypnotized || other.currentState == EmployeeState.Sleeping) continue;
            if (other.IsBusy || other.isMoving) continue;
            if (other.currentState != EmployeeState.Idle && other.currentState != EmployeeState.Conversing) continue;

            Room otherRoom = other.GetCurrentRoom();

            // SANGAT KETAT: Kedua employee HARUS berada di dalam Room yang SAMA!
            if (myRoom == null || otherRoom == null || myRoom != otherRoom) continue;

            // Radius interaksi sosial maksimal (hanya dalam jarak dekat)
            if (Vector3.Distance(transform.position, other.transform.position) > (conversationDistance * 2.5f)) continue;

            result.Add(other);
        }

        return result;
    }

    private void EndConversation()
    {
        if (currentState == EmployeeState.Conversing)
        {
            currentState = EmployeeState.Idle;
            OnStateChanged?.Invoke(currentState);
        }

        // Beritahu anggota grup bahwa employee ini keluar dari obrolan
        if (socialGroupMembers.Count > 0)
        {
            var copy = new List<Employee>(socialGroupMembers);
            socialGroupMembers.Clear();
            foreach (var partner in copy)
            {
                partner?.OnPartnerLeftConversation(this);
            }
        }

        conversationTimer = 0f;
    }
}
