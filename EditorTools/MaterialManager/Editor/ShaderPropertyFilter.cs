using System;
using GWOO.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GWOO.Editor.Tools
{
    public class ShaderPropertyFilter : EditorWindow
    {
        private Shader m_shader;
        private Action<string, string> m_onSelectProperty;
    
        // UI ELEMENTS
        private Button m_optionsMenuButton;
        protected ScrollView PropertiesScrollView;
        
        // OPTION MENU
        private bool m_keepWindowOpen;
        protected bool ShowAllProperties;

        public static void ShowWindow(Object shader, Action<string, string> onSelectProperty)
        {
            ShaderPropertyFilter wnd = GetWindow<ShaderPropertyFilter>("Properties");
            wnd.m_shader = shader as Shader;
            wnd.m_onSelectProperty = onSelectProperty;
            wnd.Show();
            wnd.PopulatePropertiesScrollView();
        }

        protected virtual void CreateGUI()
        {
            if (EditorApplication.isUpdating) return;
            // Charge le document UXML depuis le dossier Resources
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("UIDocument/ShaderPropertyFilter");
            visualTree.CloneTree(rootVisualElement);
            
            EditorCustomStyles.SetCustomStyleSheet(rootVisualElement);
            EditorCustomStyles.SetCustomTheme(rootVisualElement);

            // Assignations
            VisualTreeAssignations(rootVisualElement);

            // Configurations
            SetupOptionsMenuButton();
        }


        protected virtual void VisualTreeAssignations(VisualElement root)
        {
            m_optionsMenuButton = rootVisualElement.Q<Button>("options-menu-button");
            PropertiesScrollView = rootVisualElement.Q<ScrollView>("properties-scrollview");
        }
        private void SetupOptionsMenuButton()
        {
            m_optionsMenuButton.clicked += ShowOptionsMenu;
        }

        protected virtual void PopulatePropertiesScrollView()
        {
            if (m_shader == null)
            {
                Debug.LogWarning("ShaderPropertyFilterWindow: Shader is null.");
                return;
            }
        
            PropertiesScrollView.Clear();

            int propertyCount = ShaderUtil.GetPropertyCount(m_shader);
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(m_shader, i);
                string propertyDisplayName = ShaderUtil.GetPropertyDescription(m_shader, i);
                if (!ShowProperty(propertyDisplayName)) continue;
                Button propertyButton = new(() => PropertyClicked(propertyName, propertyDisplayName))
                {
                    text = propertyDisplayName,
                    focusable = false
                };
                PropertiesScrollView.Add(propertyButton);
            }
        }

        private void PropertyClicked(string propertyName, string propertyDisplayName)
        {
            m_onSelectProperty?.Invoke(propertyName, propertyDisplayName);
            if (!m_keepWindowOpen)
            {
                Close();
            }
        }

        protected bool ShowProperty(string propertyDisplayName)
        {
            return ShowAllProperties ||
                   (!propertyDisplayName.StartsWith("_") && !propertyDisplayName.StartsWith("unity"));
        }

        protected virtual void ShowOptionsMenu()
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Keep window open"), m_keepWindowOpen, () => 
            {
                m_keepWindowOpen = !m_keepWindowOpen;
            });
            menu.AddItem(new GUIContent("Show all properties"), ShowAllProperties, () => 
            {
                ShowAllProperties = !ShowAllProperties;
                PopulatePropertiesScrollView();
            });
            menu.ShowAsContext();
        }
    }
}