using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Setup otomatis untuk Scene Tutorial.
/// Menjamin ketersediaan Camera, EventSystem, FacilityHUD, CameraDrag, CameraPanTutorialStep, dan TutorialTrigger di dalam scene.
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

        // Pastikan Camera memiliki script CameraDrag agar bisa digeser dengan Klik Kanan
        if (mainCam.GetComponent<CameraDrag>() == null)
        {
            mainCam.gameObject.AddComponent<CameraDrag>();
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

        // 4. Setup CameraPanTutorialStep jika belum ada
        if (FindFirstObjectByType<CameraPanTutorialStep>() == null)
        {
            GameObject stepObj = new GameObject("CameraPanTutorialStep");
            stepObj.AddComponent<CameraPanTutorialStep>();
        }

        // 5. Setup ContainmentUnitTutorialStep jika belum ada
        if (FindFirstObjectByType<ContainmentUnitTutorialStep>() == null)
        {
            GameObject step2Obj = new GameObject("ContainmentUnitTutorialStep");
            step2Obj.AddComponent<ContainmentUnitTutorialStep>();
        }

        // 6. Setup EmployeeMoveTutorialStep jika belum ada
        if (FindFirstObjectByType<EmployeeMoveTutorialStep>() == null)
        {
            GameObject step3Obj = new GameObject("EmployeeMoveTutorialStep");
            step3Obj.AddComponent<EmployeeMoveTutorialStep>();
        }

        // 7. Setup ResearchTutorialStep jika belum ada
        if (FindFirstObjectByType<ResearchTutorialStep>() == null)
        {
            GameObject step4Obj = new GameObject("ResearchTutorialStep");
            step4Obj.AddComponent<ResearchTutorialStep>();
        }

        // 8. Setup NutritionTutorialStep jika belum ada
        if (FindFirstObjectByType<NutritionTutorialStep>() == null)
        {
            GameObject step5Obj = new GameObject("NutritionTutorialStep");
            step5Obj.AddComponent<NutritionTutorialStep>();
        }

        // 9. Setup HarvestTutorialStep jika belum ada
        if (FindFirstObjectByType<HarvestTutorialStep>() == null)
        {
            GameObject step6Obj = new GameObject("HarvestTutorialStep");
            step6Obj.AddComponent<HarvestTutorialStep>();
        }

        // 10. Setup TutorialTrigger jika belum ada
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
        Debug.Log("[TutorialSetup] Scene Tutorial berhasil disetup dengan CameraPanTutorialStep, CameraDrag, FacilityHUD, dan TutorialTrigger.");
    }
#endif
}
