using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EmployeeAssignmentEditorTool
{
    [MenuItem("Tools/Setup Employee Assignment Scene")]
    public static void SetupSceneInEditor()
    {
        // Ensure we are in the EmployeeAssignment scene before running setup to prevent messing up other scenes
        string currentScene = EditorSceneManager.GetActiveScene().name;
        if (currentScene != "EmployeeAssignment")
        {
            if (!EditorUtility.DisplayDialog("Warning", "You are not in the 'EmployeeAssignment' scene. Are you sure you want to run the setup here?", "Yes", "Cancel"))
            {
                return;
            }
        }

        EmployeeAssignmentSetup.SetupEmployeeAssignmentScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Employee Assignment Scene successfully set up in Editor!");
    }
}
