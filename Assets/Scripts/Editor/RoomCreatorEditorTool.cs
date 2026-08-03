using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RoomCreatorEditorTool
{
    [MenuItem("Tools/Setup Room Creator Scene")]
    public static void SetupSceneInEditor()
    {
        RoomCreatorSetup.SetupRoomCreatorScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Room Creator Scene successfully setup in Editor!");
    }
}
