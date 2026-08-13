using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Setup otomatis untuk Scene Tutorial.
/// Menjamin ketersediaan Camera, EventSystem, FacilityHUD, dan TutorialTrigger di dalam scene.
/// </summary>
[ExecuteAlways]
public class TutorialSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupTutorialScene();
    }

    public static void SetupTutorialScene()
    {
        // 1. Setup Camera jika belum ada
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);
            mainCam.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
        }

        // 2. Setup EventSystem jika belum ada
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 3. Setup FacilityHUD jika belum ada
        if (FindFirstObjectByType<FacilityHUD>() == null)
        {
            GameObject hudObj = new GameObject("FacilityHUD");
            hudObj.AddComponent<FacilityHUD>();
        }

        // 4. Setup TutorialTrigger jika belum ada
        if (FindFirstObjectByType<TutorialTrigger>() == null)
        {
            GameObject triggerObj = new GameObject("TutorialTrigger");
            triggerObj.AddComponent<TutorialTrigger>();
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Aeterna Bloom/Setup Tutorial Scene")]
    public static void MenuSetupTutorialScene()
    {
        SetupTutorialScene();
        Debug.Log("[TutorialSetup] Scene Tutorial berhasil disetup dengan FacilityHUD dan TutorialTrigger.");
    }
#endif
}
