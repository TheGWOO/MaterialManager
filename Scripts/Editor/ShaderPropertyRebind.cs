using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GWOO.Tools
{
    public class ShaderPropertyRebind : ShaderPropertyFilter
    {
        private Shader m_shaderSource;
        private Shader m_shaderTarget;
        public static Dictionary<string, string> ReboundRefs { get; private set; } = new();
        
        // UI ELEMENTS
        private VisualElement m_propertiesScrollviewContainer;
        private ScrollView m_propertiesScrollviewTarget;
        private Label m_headerLabel;
        
        public static void ShowWindow(Object sourceShader, Object targetShader)
        {
            ShaderPropertyRebind wnd = GetWindow<ShaderPropertyRebind>("Properties Rebind");
            wnd.m_shaderSource = sourceShader as Shader;
            wnd.m_shaderTarget = targetShader as Shader;
            wnd.Show();
            wnd.PopulatePropertiesScrollView();
        }

        // protected override void CreateGUI()
        // {
        // }

        protected override void VisualTreeAssignations(VisualElement root)
        {
            base.VisualTreeAssignations(root);
            
            // SCROLLVIEW CONTAINER
            m_propertiesScrollviewContainer = PropertiesScrollView.contentContainer;
            m_propertiesScrollviewContainer.style.flexDirection = FlexDirection.Row;
            // HEADER
            m_headerLabel = root.Q<Label>("header-label");
            m_headerLabel.text = "Rebind properties";
        }

        protected override void PopulatePropertiesScrollView()
        {
            if (m_shaderSource == null || m_shaderTarget == null)
            {
                Debug.LogWarning("source : ", m_shaderSource);
                Debug.LogWarning("Target : ", m_shaderTarget);
                return;
            }
        
            m_propertiesScrollviewContainer.Clear();
            ReboundRefs.Clear();
            VisualElement scrollViewContainerSource = new();
            scrollViewContainerSource.style.flexGrow = 1;
            VisualElement scrollViewContainerTarget = new();
            
            m_propertiesScrollviewContainer.Add(scrollViewContainerSource);
            m_propertiesScrollviewContainer.Add(scrollViewContainerTarget);
            
            int propertyCount = ShaderUtil.GetPropertyCount(m_shaderSource);
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyDisplayName = ShaderUtil.GetPropertyDescription(m_shaderSource, i);
                if (!ShowProperty(propertyDisplayName)) continue;
                string propertyName = ShaderUtil.GetPropertyName(m_shaderSource, i);
                ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(m_shaderSource, i);

                // SOURCE PROPERTY BUTTON
                Button propertyButton = new() { text = propertyDisplayName, focusable = false };
                propertyButton.clicked += PropertySourceClicked;
                scrollViewContainerSource.Add(propertyButton);

                // TARGET PROPERTY BUTTON
                Button propertyButtonTarget = new() { text = "", focusable = false };
                propertyButtonTarget.clicked += () => PropertyTargetClicked(propertyButtonTarget, propertyType, propertyName);
                int targetIndex = FindPropertyIndex(m_shaderTarget, propertyName);
                if (targetIndex != -1)
                {
                    string propertyDisplayNameTarget =
                        ShaderUtil.GetPropertyDescription(m_shaderTarget, targetIndex);
                    propertyButtonTarget.text = propertyDisplayNameTarget;
                }

                ReboundRefs.Add(propertyDisplayName, propertyDisplayName);
                scrollViewContainerTarget.Add(propertyButtonTarget);
            }
        }
        
        private static int FindPropertyIndex(Shader shader, string reference)
        {
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                if (propertyName == reference) return i;
            }
            return -1;
        }
        
        private static void PropertySourceClicked()
        {
        }
        private void PropertyTargetClicked(Button button, ShaderUtil.ShaderPropertyType type, string initialRef)
        {
            // Debug.Log(type);
            GenericMenu menu = new();
            
            menu.AddItem(new GUIContent("---"), false, () => SetPropertyBind(button, ""));
            
            int propertyCount = ShaderUtil.GetPropertyCount(m_shaderTarget);
            for (int i = 0; i < propertyCount; i++)
            {
                if (!IsTypeMatching(type, ShaderUtil.GetPropertyType(m_shaderTarget, i))) continue;
                string propertyDisplayName = ShaderUtil.GetPropertyDescription(m_shaderTarget, i);
                if (!ShowProperty(propertyDisplayName)) continue;
                string propertyName = ShaderUtil.GetPropertyName(m_shaderTarget, i);
                menu.AddItem(new GUIContent(propertyDisplayName), false, () => SetPropertyBind(button, propertyDisplayName));
                ReboundRefs[initialRef] = propertyName;
            }
            menu.ShowAsContext();
        }

        private static void SetPropertyBind(Button button, string displayName)
        {
            button.text = displayName;
        }
        
        protected override void ShowOptionsMenu()
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Show all properties"), ShowAllProperties, () => 
            {
                ShowAllProperties = !ShowAllProperties;
                PopulatePropertiesScrollView();
            });
            menu.ShowAsContext();
        }

        private static bool IsTypeMatching(ShaderUtil.ShaderPropertyType sourceType, ShaderUtil.ShaderPropertyType targetType)
        {
            return sourceType == targetType ||
                   (sourceType == ShaderUtil.ShaderPropertyType.Float && targetType == ShaderUtil.ShaderPropertyType.Range) ||
                   (sourceType == ShaderUtil.ShaderPropertyType.Range && targetType == ShaderUtil.ShaderPropertyType.Float);
        }
    }
}