using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class ShopEditorTool
{
    [MenuItem("Tools/Setup Shop Scene")]
    [MenuItem("Aeterna Bloom/Shop Scene Setup")]
    public static void SetupShopScene()
    {
        string currentScene = EditorSceneManager.GetActiveScene().name;
        string scenePath = "Assets/Scenes/Shop.unity";

        if (currentScene != "Shop")
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Setup Shop Scene",
                "You are currently not in 'Shop' scene. Do you want to create/open 'Assets/Scenes/Shop.unity' and run setup?",
                "Yes, Open & Setup",
                "Cancel"
            );

            if (!proceed) return;

            if (!System.IO.File.Exists(scenePath))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log($"[ShopEditorTool] Created new scene at {scenePath}");
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        ShopSetup.SetupShopScene();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[ShopEditorTool] Shop Scene setup completed and saved successfully!");
    }
}
#endif
