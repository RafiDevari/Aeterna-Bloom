using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class EmployeeViewerEditorTool
{
    [MenuItem("Tools/Setup Employee Viewer Scene")]
    [MenuItem("Aeterna Bloom/Employee Viewer Scene Setup")]
    public static void SetupViewerScene()
    {
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "EmployeeViewer")
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Setup Employee Viewer",
                "You are currently not in 'EmployeeViewer' scene. Do you want to create/open 'Assets/Scenes/EmployeeViewer.unity' and run setup?",
                "Yes, Open & Setup",
                "Cancel"
            );

            if (!proceed) return;

            string scenePath = "Assets/Scenes/EmployeeViewer.unity";
            var scene = EditorSceneManager.GetSceneByPath(scenePath);

            if (!System.IO.File.Exists(scenePath))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log($"[EmployeeViewerEditorTool] Created new scene at {scenePath}");
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        EmployeeViewerSetup.SetupEmployeeViewerScene();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[EmployeeViewerEditorTool] Setup completed and scene saved successfully!");
    }
}
#endif
