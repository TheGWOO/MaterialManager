using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GWOO.Editor.Extensions
{
    [InitializeOnLoad]
    public class CustomSceneDrawModes
    {
        static CustomSceneDrawModes()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
    
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (Application.isPlaying == false)
            {
                UpdateFogKeyword();
                CheckPrefabStage();
            }
        }
    
        private static void CheckPrefabStage()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
            {
                Shader.DisableKeyword("DRAWMODE_PREFAB_ON");
            }
            else
            {
                Shader.EnableKeyword("DRAWMODE_PREFAB_ON");
            }
        }
    
        private static bool IsFogEnabledInSceneView()
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.sceneViewState != null)
            {
                return SceneView.lastActiveSceneView.sceneViewState.fogEnabled;
            }
    
            return false;
        }
    
        private static void UpdateFogKeyword()
        {
            if (IsFogEnabledInSceneView())
            {
                Shader.DisableKeyword("DRAWMODE_FOG_OFF");
            }
            else
            {
                Shader.EnableKeyword("DRAWMODE_FOG_OFF");
            }
        }
    }
}