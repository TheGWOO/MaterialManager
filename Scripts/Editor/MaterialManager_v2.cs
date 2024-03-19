using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class MaterialManager_v2 : EditorWindow
{
    #region DATA VARIABLES
    
    private List<Material> m_materialsFound = new List<Material>();
    private List<Material> m_toRefresh = new List<Material>();
    private List<Material> m_materialsVisible = new List<Material>();
    private List<Material> m_matAncestors = new List<Material>();
    private Material m_SGchild;
    private string m_searchQuery;
    private string m_filteredProperty;
    private string m_filteredPropertyName;
    private int m_materialsFoundCount;
    private int m_materialsReparentedCount;
    private int m_variantsFilteredCount;
    private string m_shaderAssetPath;
    
    #endregion
    #region STATES VARIABLES

    private bool m_showReparentedMaterials;
    private bool m_ignoreSearchField = true;

    #endregion
    #region SETTINGS VARIABLES

    private double m_delayStartTime;
    private const double DelayDuration = 1;
    private const string DefaultSearchField = "Search materials...";
    private const string DefaultFolderPath = "Assets";
    
    #endregion
    #region UI-TOOLKIT VARIABLES

    // OBJECT FIELD
    private ObjectField m_shaderField;
    private ObjectField m_newShaderField;
    // TEXT FIELD
    private TextField m_searchField;
    // LABEL
    private Label m_materialsCountLabel;
    private Label m_variantsCountLabel;
    private Label m_reparentCountLabel;
    private Label m_folderPathLabel;
    // BUTTON
    private Button m_findMaterialsButton;
    private Button m_clearSearchButton;
    private Button m_selectVisibleButton;
    private Button m_selectAllButton;
    private Button m_filterPropertyButton;
    private Button m_clearFilteredPropertyButton;
    private Button m_reparentButton;
    private Button m_folderBrowseButton;
    private Button m_folderDefaultButton;
    // TOGGLE
    private Toggle m_showVariantsToggle;
    private Toggle m_reparentToggle;
    // VISUAL ELEMENT
    private VisualElement m_statsSection;
    private VisualElement m_reparentButtonSection;
    private VisualElement m_searchFieldText;
    private VisualElement m_filtersSection;
    private VisualElement m_folderPathSection;
    // OTHER
    private ScrollView m_materialsScrollview;
    private DropdownField m_searchInDropdown;
    
    #endregion
    
    [MenuItem("OSG/Material Manager %m", false, 1)]
    public static void OpenEditorWindow()
    {
        MaterialManager_v2 wnd = GetWindow<MaterialManager_v2>();
        wnd.titleContent = new GUIContent("Material Manager v2");
        wnd.minSize = new Vector2(220, 0);
    }

    void OnDisable()
    {
        SaveSettings();
    }
    
    private void CreateGUI()
    {
        if (!EditorApplication.isUpdating) // Required to prevent import errors via PackageManager
        {
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("UIDocument/MaterialManager_EditorWindow");
            visualTree.CloneTree(rootVisualElement);
            VisualElement root = rootVisualElement.ElementAt(0);

            // ASSIGN ELEMENTS
            m_shaderField = root.Q<ObjectField>("shader-field");
            m_findMaterialsButton = root.Q<Button>("find-materials-button");
            m_materialsScrollview = root.Q<ScrollView>("materials-scrollview");
            m_searchField = root.Q<TextField>("search-field");
            m_showVariantsToggle = root.Q<Toggle>("show-variants-toggle");
            m_clearSearchButton = root.Q<Button>("clear-search-button");
            m_reparentButton = root.Q<Button>("reparent-button");
            m_materialsCountLabel = root.Q<Label>("stat-materials-count-label");
            m_variantsCountLabel = root.Q<Label>("stat-variant-count-label");
            m_reparentCountLabel = root.Q<Label>("stat-reparent-count-label");
            m_statsSection = root.Q<VisualElement>("stats-section");
            m_selectAllButton = root.Q<Button>("select-all-button");
            m_selectVisibleButton = root.Q<Button>("select-visible-button");
            m_filterPropertyButton = root.Q<Button>("filter-property-button");
            m_clearFilteredPropertyButton = root.Q<Button>("clear-filter-property-button");
            m_filtersSection = root.Q<VisualElement>("filters-section");
            m_searchInDropdown = root.Q<DropdownField>("search-in-dropdown");
            m_folderPathSection = root.Q<VisualElement>("folder-path-section");
            m_reparentButtonSection = root.Q<VisualElement>("reparent-button-section");
            m_newShaderField = root.Q<ObjectField>("new-shader-field");
            m_reparentToggle = root.Q<Toggle>("reparent-toggle");
            m_folderBrowseButton = root.Q<Button>("folder-browse-button");
            m_folderDefaultButton = root.Q<Button>("folder-default-button");
            m_folderPathLabel = root.Q<Label>("folder-path-label");

            // CALLBCKS
            m_shaderField.RegisterValueChangedCallback(ShaderField_Changed);
            m_newShaderField.RegisterValueChangedCallback(NewShaderField_Changed);
            m_searchField.RegisterValueChangedCallback(SearchField_Changed);
            m_showVariantsToggle.RegisterValueChangedCallback(ShowVariantsToggle_Changed);
            m_searchInDropdown.RegisterValueChangedCallback(SearchIn_Changed);

            // EVENTS
            m_findMaterialsButton.clicked += FindMaterials;
            m_reparentButton.clicked += ReparentMaterialsToNewShader;
            m_clearSearchButton.clicked += ClearSearchButton;
            m_selectVisibleButton.clicked += SelectVisibleMaterialsButton;
            m_selectAllButton.clicked += SelectAllMaterialsButton;
            m_filterPropertyButton.clicked += OpenPropertyButtonFilter;
            m_clearFilteredPropertyButton.clicked += ClearFilteredPropertyButton;
            m_folderBrowseButton.clicked += BrowseFolderPathButton;
            m_folderDefaultButton.clicked += DefaultFolderPathButton;

            m_searchField.RegisterCallback<FocusInEvent>(SearchField_FocusIn);
            m_searchField.RegisterCallback<FocusOutEvent>(SearchField_FocusOut);

            //INIT
            InitGUI();
        }
    }

    private void InitGUI()
    {
        LoadSettings();
        
        // SHADER TO FIND
        if (m_shaderField.value != null) // FindMaterials on tool start if any shader had been saved
        {
            FindMaterials();
        }
        else // If no shader had been saved sets everything has disabled by default
        {
            m_findMaterialsButton.SetEnabled(false);
            m_selectAllButton.SetEnabled(false);
            m_selectVisibleButton.SetEnabled(false);
            m_filtersSection.SetEnabled(false);
        }
        
        // SEARCH FIELD
        m_searchField.value = DefaultSearchField;
        // Find internal child and sets up its default class as placeholder
        m_searchFieldText = FindChild(m_searchField, "unity-text-element");
        m_searchFieldText.RemoveFromClassList("unity-text-element");
        m_searchFieldText.AddToClassList("search-field-placeholder");
    }
    
    #region CALLBACKS

    private void NewShaderField_Changed(ChangeEvent<Object> evt)
    {
        DisplayUIElement(m_reparentButtonSection, m_newShaderField.value != null && m_shaderField.value != null);
    }

    private void SearchIn_Changed(ChangeEvent<string> evt)
    {
        switch (m_searchInDropdown.index)
        {
            case 0:
                m_folderPathSection.style.display = DisplayStyle.Flex;
                break;
            case 1:
                m_folderPathSection.style.display = DisplayStyle.None;
                break;
        }
    }
    
    private void ShaderField_Changed(ChangeEvent<Object> evt)
    {
        m_findMaterialsButton.SetEnabled(m_shaderField.value != null);
        DisplayUIElement(m_reparentButtonSection, m_newShaderField.value != null && m_shaderField.value != null);
        FindMaterial_Clear();
    }
    
    private void SearchField_Changed(ChangeEvent<String> evt)
    {
        if (m_ignoreSearchField)
        {
            m_ignoreSearchField = false;
        }
        else
        {
            m_searchQuery = evt.newValue;
            DisplayMaterialsList();
        }
    }
    private void ShowVariantsToggle_Changed(ChangeEvent<bool> evt)
    {
        DisplayMaterialsList();
    }

    #endregion
    
    #region UI EVENTS

    private void SearchField_FocusIn(FocusInEvent evt)
    {
        if (m_searchField.value == DefaultSearchField) // On FocusIn check if there is default text or user search
        {
            // If default search then remove it and sets its class to user search one
            m_ignoreSearchField = true;
            m_searchField.value = "";
            m_searchFieldText.RemoveFromClassList("search-field-placeholder");
            m_searchFieldText.AddToClassList("unity-text-element");
        }
    }

    private void SearchField_FocusOut(FocusOutEvent evt)
    {
        if (string.IsNullOrEmpty(m_searchField.value))  // On FocusOut check if search field is blank
        {
            // If blank then resets to default class and value
            m_ignoreSearchField = true;
            m_searchField.value = DefaultSearchField;
            m_searchFieldText.AddToClassList("search-field-placeholder");
            m_searchFieldText.RemoveFromClassList("unity-text-element");
        }
    }

    private void ClearSearchButton()
    {
        if (m_searchField.value != DefaultSearchField)
        {
            m_ignoreSearchField = true;
            m_searchField.value = DefaultSearchField;
            m_searchQuery = null;
            m_searchFieldText.AddToClassList("search-field-placeholder");
            m_searchFieldText.RemoveFromClassList("unity-text-element");

            DisplayMaterialsList();
        }
    }
    
    private void SelectAllMaterialsButton()
    {
        Selection.objects = m_materialsFound.ToArray();
    }

    private void SelectVisibleMaterialsButton()
    {
        VisibleMaterials();
        Selection.objects = m_materialsVisible.ToArray();
    }
    
    private void BrowseFolderPathButton()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Search Folder", "", "");
        if (!string.IsNullOrEmpty(folderPath))
        {
            string relativePath = folderPath.Replace(Application.dataPath, "Assets");
            m_folderPathLabel.text = relativePath;
        }
    }
    private void DefaultFolderPathButton()
    {
        m_folderPathLabel.text = DefaultFolderPath;
    }
    
    private void ClearFilteredPropertyButton()
    {
        m_filterPropertyButton.text = "Filter by property override";
        m_filteredProperty = null;
        m_filteredPropertyName = null;
        m_clearFilteredPropertyButton.style.display = DisplayStyle.None;
        DisplayMaterialsList();
    }

    private void OpenPropertyButtonFilter()
    {
        ShaderPropertyFilter_v2.ShowWindow(m_shaderField.value, (propertyName, propertyDisplayName) =>
        {
            m_filteredProperty = propertyName;
            m_filteredPropertyName = propertyDisplayName;
            m_filterPropertyButton.text = m_filteredPropertyName;
            m_clearFilteredPropertyButton.style.display = DisplayStyle.Flex;
            DisplayMaterialsList();
        });
    }

    #endregion
    
    #region DISPLAY GUI

    private void DisplayToggledMaterialField(Material mat)
    {
        VisualElement container = new VisualElement();
        ObjectField materialField = new ObjectField
        {
            value = mat,
            objectType = typeof(Material),
            allowSceneObjects = true,
            focusable = false
        };
        
        VisualElement button = FindChild(materialField, "unity-object-field__selector");
        if (button != null)
        {
            button.style.display = DisplayStyle.None;
        }

        container.Add(materialField);
        m_materialsScrollview.Add(container);

        container.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 1) // RIGHT CLICK EVENT
            {
                materialField.SetEnabled(!materialField.enabledSelf);
                evt.StopPropagation();
            }
        });
    }

    private void DisplayMaterialsList()
    {
        m_materialsScrollview.Clear();

        foreach (Material mat in m_materialsFound)
        {
            if (FilterVariants(mat) || FilterSearchField(mat) || FilterPropertyOverride(mat))
            {
                continue;
            }

            DisplayToggledMaterialField(mat);
        }
        
        m_selectVisibleButton.SetEnabled(m_materialsScrollview.contentContainer.childCount > 0);
        m_filtersSection.SetEnabled(true);
        DisplayStatsSection(true);
    }

    private void DisplayStatsSection(bool showStats)
    {
        if (showStats)
        {
            m_statsSection.style.display = DisplayStyle.Flex;
            m_materialsCountLabel.text = "Materials Found: " + m_materialsFoundCount.ToString();

            if (m_variantsFilteredCount > 0)
            {
                m_variantsCountLabel.text = "Variant Children: " + m_variantsFilteredCount.ToString();
                m_variantsCountLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_variantsCountLabel.style.display = DisplayStyle.None;
            }
            if (m_showReparentedMaterials)
            {
                m_reparentCountLabel.text = "Materials Reparented: " + m_materialsReparentedCount.ToString();
                m_reparentCountLabel.style.display = DisplayStyle.Flex;
                m_showReparentedMaterials = false;
            }
            else
            {
                m_reparentCountLabel.style.display = DisplayStyle.None;
            }
        }
        else
        {
            m_statsSection.style.display = DisplayStyle.None;
        }
    }

    #endregion
    
    private VisualElement FindChild(VisualElement parent, string ussClass)
    {
        Stack<VisualElement> stack = new Stack<VisualElement>();
        stack.Push(parent);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.ClassListContains(ussClass))
            {
                return current;
            }

            foreach (var child in current.Children())
            {
                stack.Push(child);
            }
        }

        return null;
    }
    
    private void DisplayUIElement(VisualElement element, bool displayed)
    {
        if (element != null)
        {
            element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    #region MATERIAL FILTERS

    private bool FilterVariants(Material mat)
    {
        return !m_showVariantsToggle.value && mat.isVariant && mat.parent != m_SGchild;
    }
    private bool FilterSearchField(Material mat)
    {
        return !string.IsNullOrEmpty(m_searchQuery) && !mat.name.ToLower().Contains(m_searchQuery.ToLower());
    }
    private bool FilterPropertyOverride(Material mat)
    {
        return !string.IsNullOrEmpty(m_filteredProperty) && !mat.IsPropertyOverriden(m_filteredProperty);
    }

    private void VisibleMaterials()
    {
        m_materialsVisible.Clear();

        foreach (VisualElement container in m_materialsScrollview.Children())
        {
            if (container.childCount > 0 && container[0] is ObjectField materialField && materialField.value is Material mat)
            {
                bool isVariantFilteredOut = !m_showVariantsToggle.value && mat.isVariant && mat.parent != m_SGchild;
                bool isSearchFilteredOut = !string.IsNullOrEmpty(m_searchQuery) && !mat.name.ToLower().Contains(m_searchQuery.ToLower());
                bool isFieldEnabled = materialField.enabledSelf;

                if (!isVariantFilteredOut && !isSearchFilteredOut && isFieldEnabled)
                {
                    m_materialsVisible.Add(mat);
                }
            }
        }
    }
    
    #endregion
    
    #region FIND MATERIALS

    private void FindMaterials()
    {
        AssetDatabase.Refresh();
        FindMaterial_Clear();

        m_shaderAssetPath = AssetDatabase.GetAssetPath(m_shaderField.value);
        m_SGchild = AssetDatabase.LoadAssetAtPath<Material>(m_shaderAssetPath);
        
        switch (m_searchInDropdown.index)
        {
            case 0:
                FindMaterials_Path();
                break;
            case 1:
                FindMaterials_Scene();
                break;
        }
        
        m_materialsFoundCount = m_materialsFound.Count;
        
        m_selectAllButton.SetEnabled(m_materialsFoundCount > 0);
        DisplayMaterialsList();
    }
    
    private void FindMaterials_Scene()
    {
        foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Renderer[] renderers = rootGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    FindMaterials_AddToList(material);
                }
            }
        }
    }

    private void FindMaterials_Path()
    {
        string searchPath = !string.IsNullOrEmpty(m_folderPathLabel.text) ? m_folderPathLabel.text : "Assets";
        string[] guids = AssetDatabase.FindAssets("t:material", new[] { searchPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            FindMaterials_AddToList(material);
        }
    }
    
    private void FindMaterials_AddToList(Material material)
    {
        if (material != null && material.shader == m_shaderField.value && !m_materialsFound.Contains(material) && material != m_SGchild)
        {
            if (material.isVariant && material.parent != m_SGchild)
            {
                m_toRefresh.Add(material);
                m_variantsFilteredCount++;
            }
            m_materialsFound.Add(material);
        }
    }
    
    private void FindMaterial_Clear()
    {
        m_materialsFound.Clear();
        m_toRefresh.Clear();
        m_materialsVisible.Clear();
        m_variantsFilteredCount = 0;
        m_filteredProperty = null;
        m_filteredPropertyName = null;
        m_materialsScrollview.Clear();
        ClearSearchButton();
        DisplayStatsSection(false);
        m_selectAllButton.SetEnabled(false);
        m_selectVisibleButton.SetEnabled(false);
        m_filtersSection.SetEnabled(false);
    }

    #endregion
    
    #region REPARENT SHADER

    private void ReparentMaterialsToNewShader()
    {
        m_materialsReparentedCount = 0;

        if (m_reparentToggle.value)
        {
            VisibleMaterials();
        }
        
        FindAncestors();
        ReparentAncestors();
        
        Selection.objects = m_toRefresh.ToArray();
        m_showReparentedMaterials = true;
        m_delayStartTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += DelayRefresh;
    }

    private void FindAncestors()
    {
        m_matAncestors.Clear();
        foreach (Material mat in m_reparentToggle.value ? m_materialsVisible : m_materialsFound)
        {
            Material currentMat = mat;
            while (currentMat.isVariant && currentMat.parent != m_SGchild)
            {
                currentMat = currentMat.parent;
            }

            if (!m_matAncestors.Contains(currentMat))
            {
                m_matAncestors.Add(currentMat);
            }
        }
    }
    private void ReparentAncestors()
    {
        Material newShaderMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(m_newShaderField.value));
        
        foreach (Material mat in m_matAncestors)
        {
            if (newShaderMaterial != null) // NEW SHADER IS SHADER GRAPH
            {
                mat.parent = newShaderMaterial;
            }
            else // NEW SHADER ISN'T SHADER GRAPH
            {
                mat.parent = null;
                mat.shader = newShaderMaterial.shader;
            }
                
            m_materialsReparentedCount++;
        }
    }

    private void DelayRefresh()
    {
        double currentTime = EditorApplication.timeSinceStartup;

        if (currentTime - m_delayStartTime >= DelayDuration)
        {
            EditorApplication.update -= DelayRefresh;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Materials have been reparented to new shader. Total Reparented: " + m_materialsReparentedCount);
            
            FindMaterials();
        }
    }

    #endregion

    #region SAVE/LOAD

    void SaveSettings()
    {
        if (!string.IsNullOrEmpty(m_folderPathLabel.text))
        {
            EditorPrefs.SetString("ShaderManager_SearchFolder", m_folderPathLabel.text);
        }

        EditorPrefs.SetString("ShaderManager_shaderField.value", AssetDatabase.GetAssetPath(m_shaderField.value));

        EditorPrefs.SetBool("ShaderManager_FilterState", m_showVariantsToggle.value);
        
        EditorPrefs.SetInt("ShaderManager_SearchInOption", m_searchInDropdown.index);
    }
    void LoadSettings()
    {
        m_folderPathLabel.text = EditorPrefs.GetString("ShaderManager_SearchFolder", DefaultFolderPath);

        string shaderPath = EditorPrefs.GetString("ShaderManager_shaderField.value", "");
        m_shaderField.value = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

        m_showVariantsToggle.value = EditorPrefs.GetBool("ShaderManager_FilterState", true);
        
        m_searchInDropdown.index = EditorPrefs.GetInt("ShaderManager_SearchInOption", 0);
    }

    #endregion
}