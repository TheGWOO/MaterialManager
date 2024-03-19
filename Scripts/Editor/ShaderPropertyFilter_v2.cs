using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class ShaderPropertyFilter_v2 : EditorWindow
{
    private Shader m_shader;
    private Action<string, string> m_onSelectProperty;
    private bool m_keepWindowOpen;
    
    // UI ELEMENTS
    private Button m_optionsMenuButton;
    private ScrollView m_propertiesScrollview;

    public static void ShowWindow(Object shader, Action<string, string> onSelectProperty)
    {
        ShaderPropertyFilter_v2 wnd = GetWindow<ShaderPropertyFilter_v2>("Properties");
        wnd.m_shader = shader as Shader;
        wnd.m_onSelectProperty = onSelectProperty;
        wnd.Show();
        wnd.PopulatePropertiesScrollView();
    }

    public void CreateGUI()
    {
        // Charge le document UXML depuis le dossier Resources
        var visualTree = Resources.Load<VisualTreeAsset>("UIDocument/ShaderPropertyFilter");
        visualTree.CloneTree(rootVisualElement);

        // Assignations
        m_optionsMenuButton = rootVisualElement.Q<Button>("options-menu-button");
        m_propertiesScrollview = rootVisualElement.Q<ScrollView>("properties-scrollview");

        // Configurations
        SetupOptionsMenuButton();
    }

    private void SetupOptionsMenuButton()
    {
        m_optionsMenuButton.clicked += ShowOptionsMenu;
    }

    private void PopulatePropertiesScrollView()
    {
        if (m_shader == null)
        {
            Debug.LogWarning("ShaderPropertyFilterWindow: Shader is null.");
            return;
        }
        
        m_propertiesScrollview.Clear();

        int propertyCount = ShaderUtil.GetPropertyCount(m_shader);
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = ShaderUtil.GetPropertyName(m_shader, i);
            string propertyDisplayName = ShaderUtil.GetPropertyDescription(m_shader, i);
            var propertyButton = new Button(() => NewMethod(propertyName, propertyDisplayName))
            {
                text = propertyDisplayName,
                focusable = false
            };
            m_propertiesScrollview.Add(propertyButton);
        }
    }

    private void NewMethod(string propertyName, string propertyDisplayName)
    {
        m_onSelectProperty?.Invoke(propertyName, propertyDisplayName);
        if (!m_keepWindowOpen)
        {
            Close();
        }
    }

    private void ShowOptionsMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Keep window open"), m_keepWindowOpen, () => 
        {
            m_keepWindowOpen = !m_keepWindowOpen;
        });
        menu.ShowAsContext();
    }
}
