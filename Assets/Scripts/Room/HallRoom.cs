// HallRoom.cs
using UnityEngine;

public class HallRoom : Room
{
    [Header("HallRoom Effects")]
    [Tooltip("Daftar objek efek di ruangan. Jika kosong, akan otomatis mencari anak dari objek 'Effect'.")]
    [SerializeField] private GameObject[] effectObjects;

    [Tooltip("Peluang aktifnya masing-masing efek saat ruangan di-spawn (0.10 = 10%).")]
    [SerializeField] [Range(0f, 1f)] private float effectActivationChance = 0.20f;

    protected override void Start()
    {
        base.Start();
        InitializeRandomEffects();
    }

    private void InitializeRandomEffects()
    {
        // Jika list effectObjects belum diisi di inspector, cari otomatis anak dari objek "Effect"
        if (effectObjects == null || effectObjects.Length == 0)
        {
            Transform effectParent = transform.Find("Effect");
            if (effectParent != null)
            {
                int count = effectParent.childCount;
                effectObjects = new GameObject[count];
                for (int i = 0; i < count; i++)
                {
                    effectObjects[i] = effectParent.GetChild(i).gameObject;
                }
            }
        }

        if (effectObjects == null || effectObjects.Length == 0)
            return;

        foreach (GameObject effectObj in effectObjects)
        {
            if (effectObj != null)
            {
                bool activate = Random.value < effectActivationChance;
                effectObj.SetActive(activate);
            }
        }
    }
}