using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tool Editor untuk mengeksport layout dari roomCreator dan langsung 
/// menguji (test play) pada scene Gameplay (GameplaySaveLoad).
/// </summary>
public class RoomCreatorTestTool
{
    [MenuItem("Tools/Export Room Layout & Test in Gameplay")]
    public static void ExportAndTestInGameplay()
    {
        // 1. Dapatkan instance RoomCreatorManager jika ada di scene aktif
        RoomCreatorManager manager = Object.FindFirstObjectByType<RoomCreatorManager>();
        if (manager != null)
        {
            manager.SaveLayoutToJson("room_layout_1.json");
            manager.SaveLayoutToJson("room_layout.json"); // Timpa default room_layout.json
        }

        // 2. Salin room_layout_1.json ke Resources/room_layout.json
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        string layout1Path = Path.Combine(resourcesDir, "room_layout_1.json");
        string defaultLayoutPath = Path.Combine(resourcesDir, "room_layout.json");

        if (File.Exists(layout1Path))
        {
            File.Copy(layout1Path, defaultLayoutPath, true);
            AssetDatabase.Refresh();
            Debug.Log("[RoomCreatorTestTool] Successfully copied room_layout_1.json to Resources/room_layout.json");
        }

        // 3. Pastikan Scene GameplaySaveLoad & RoomCreator terdaftar di Build Settings
        EnsureScenesInBuildSettings();

        // 4. Buka Scene GameplaySaveLoad
        string gameplayScenePath = "Assets/Scenes/GameplaySaveLoad.unity";
        if (File.Exists(gameplayScenePath))
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(gameplayScenePath);
            Debug.Log("[RoomCreatorTestTool] Opened GameplaySaveLoad scene. Enter Play Mode to test layout!");
        }
        else
        {
            Debug.LogError($"[RoomCreatorTestTool] Scene '{gameplayScenePath}' not found!");
        }
    }

    private static void EnsureScenesInBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        AddSceneIfMissing(scenes, "Assets/Scenes/RoomCreator.unity");
        AddSceneIfMissing(scenes, "Assets/Scenes/GameplaySaveLoad.unity");
        AddSceneIfMissing(scenes, "Assets/Scenes/Gameplay1.unity");

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        if (!File.Exists(scenePath)) return;

        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }
}
