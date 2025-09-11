using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

[InitializeOnLoad]
public class CustomToolbarButton
{
    static CustomToolbarButton()
    {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("▷현재씬")))
        {
            EditorSceneManager.playModeStartScene = null;
            UnityEditor.EditorApplication.isPlaying = true;
        }

        if (GUILayout.Button(new GUIContent("▶메인씬")))
        {
            var pathOfMainMenuScene = "Assets/01.Scenes/TitleScene.unity";
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfMainMenuScene);
            EditorSceneManager.playModeStartScene = sceneAsset;
            UnityEditor.EditorApplication.isPlaying = true;
        }
    }
}