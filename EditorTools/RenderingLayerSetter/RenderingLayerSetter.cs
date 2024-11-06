using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using GWOO.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using GWOO.Editor.Utils;

namespace GWOO.Editor.Tools
{
    public class RenderingLayerSetter : EditorWindow
    {
        private readonly bool _useCustomDropdown = true;
        private RenderingLayerMaskField _nativeLayerDropdown; // Native UIElement dropdown
        private PopupField<string> _customLayerDropdown; // Custom dropdown
        private const int DEFAULT_LAYER_INDEX = 2; // Default layer

        [MenuItem("Tools/GWOO/Rendering Layer Setter")]
        public static void ShowWindow()
        {
            RenderingLayerSetter window = GetWindow<RenderingLayerSetter>("Rendering Layer Setter");
            window.minSize = new Vector2(250, 120);
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            // Set custom StyleSheets
            EditorCustomStyles.SetCustomStyleSheet(rootVisualElement);
            EditorCustomStyles.SetCustomTheme(rootVisualElement);

            if (_useCustomDropdown)
            {
                int layerCount = RenderingLayerMask.GetDefinedRenderingLayerCount();
                List<string> renderingLayerNames = new();
                for (int i = 0; i < layerCount; i++)
                {
                    renderingLayerNames.Add(RenderingLayerMask.RenderingLayerToName(i));
                }
                
                _customLayerDropdown = new()
                {
                    label = "Rendering Layer:",
                    choices = renderingLayerNames,
                    index = DEFAULT_LAYER_INDEX,
                    style = { marginTop = 10},
                };
                rootVisualElement.Add(_customLayerDropdown);
            }
            else
            {
                _nativeLayerDropdown = new()
                {
                    label = "Rendering Layer:",
                    value = 1 << DEFAULT_LAYER_INDEX,
                    style = { marginTop = 10},
                };
                rootVisualElement.Add(_nativeLayerDropdown);
            }
            
            // Add spaces
            rootVisualElement.Add(new Separator("spaced"));
            
            // Create "Set to Selection" button with green style
            CustomButton setButton = new("big", "primary-color") { text = "SET to Selection", Width = 60};
            rootVisualElement.Add(setButton);
            setButton.clicked += ApplyRenderingLayerToSelected;

            // Add spaces
            setButton.style.marginBottom = new StyleLength(new Length(5));
            
            // Create "Unset from Selection" button with red style
            CustomButton unsetButton = new("big", "secondary-color") { text = "UNSET from Selection", Width = 60};
            rootVisualElement.Add(unsetButton);
            unsetButton.clicked += RemoveRenderingLayerFromSelected;
        }
        
        private void ApplyRenderingLayerToSelected()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No game objects selected!");
                return;
            }

            uint currentLayer = GetCurrentLayer();

            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                MeshRenderer[] meshRenderers = selectedObject.GetComponentsInChildren<MeshRenderer>();
                SpriteRenderer[] spriteRenderers = selectedObject.GetComponentsInChildren<SpriteRenderer>();
                
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    Undo.RecordObject(renderer, "Change Rendering Layer Mask");
                    renderer.renderingLayerMask |= currentLayer;
                }

                foreach (SpriteRenderer renderer in spriteRenderers)
                {
                    Undo.RecordObject(renderer, "Change Rendering Layer Mask");
                    renderer.renderingLayerMask |= currentLayer;
                }

                EditorSceneManager.MarkSceneDirty(selectedObject.scene);
            }

            Debug.Log($"Added rendering layer {currentLayer} to selected objects' MeshRenderers and SpriteRenderers, preserving existing layers.");
        }

        private void RemoveRenderingLayerFromSelected()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No game objects selected!");
                return;
            }
            
            uint currentLayer = GetCurrentLayer();
            
            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                MeshRenderer[] meshRenderers = selectedObject.GetComponentsInChildren<MeshRenderer>();
                SpriteRenderer[] spriteRenderers = selectedObject.GetComponentsInChildren<SpriteRenderer>();
                
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    Undo.RecordObject(renderer, "Remove Rendering Layer Mask");
                    renderer.renderingLayerMask &= ~currentLayer;
                }

                foreach (SpriteRenderer renderer in spriteRenderers)
                {
                    Undo.RecordObject(renderer, "Remove Rendering Layer Mask");
                    renderer.renderingLayerMask &= ~currentLayer;
                }

                EditorSceneManager.MarkSceneDirty(selectedObject.scene);
            }

            Debug.Log($"Removed rendering layer {currentLayer} from selected objects' MeshRenderers and SpriteRenderers.");
        }

        private uint GetCurrentLayer()
        {
            return _useCustomDropdown ? (uint)(1 << _customLayerDropdown.index) : _nativeLayerDropdown.value;
        }
    }
}