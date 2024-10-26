using System;
using System.Collections.Generic;
using GWOO.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GWOO.Editor.Tools
{
    public class MaterialManager : EditorWindow
    {
        #region DATA VARIABLES
    
        private List<Material> _materialsFound = new();
        private List<Material> _toRefresh = new();
        private List<Material> _materialsVisible = new();
        private List<Material> _matAncestors = new();
        private Material _SGchild;
        private string _searchQuery;
        private string _filteredProperty;
        private string _filteredPropertyName;
        private int _materialsFoundCount;
        private int _materialsReparentedCount;
        private int _variantsFilteredCount;
        private string _shaderAssetPath;

        private Dictionary<string, object> _TestPropertiesDic;
    
        #endregion
        #region STATES VARIABLES

        private bool _showReparentedMaterials;
        private bool _ignoreSearchField = true;
        private bool _skipClear;

        #endregion
        #region SETTINGS VARIABLES

        private double _delayStartTime;
        private const double DelayDuration = 1;
        private const string DefaultSearchField = "Search materials...";
        private const string DefaultFolderPath = "Assets";
    
        #endregion
        #region UI-TOOLKIT VARIABLES

        // OBJECT FIELD
        private ObjectField _shaderField;
        private ObjectField _newShaderField;
        // TEXT FIELD
        private TextField _searchField;
        // LABEL
        private Label _materialsCountLabel;
        private Label _variantsCountLabel;
        private Label _reparentCountLabel;
        private Label _folderPathLabel;
        // BUTTON
        private Button _findMaterialsButton;
        private Button _clearSearchButton;
        private Button _selectVisibleButton;
        private Button _selectAllButton;
        private Button _filterPropertyButton;
        private Button _clearFilteredPropertyButton;
        private Button _reparentButton;
        private Button _folderBrowseButton;
        private Button _folderDefaultButton;
        private Button _rebindPropertiesButton;
        // TOGGLE
        private Toggle _showVariantsToggle;
        private Toggle _reparentToggle;
        // VISUAL ELEMENT
        private VisualElement _statsSection;
        private VisualElement _reparentButtonSection;
        private VisualElement _searchFieldText;
        private VisualElement _filtersSection;
        private VisualElement _folderPathSection;
        // OTHER
        private ScrollView _materialsScrollview;
        private DropdownField _searchInDropdown;
    
        #endregion
    
        [MenuItem("Tools/Material Manager %m", false, 1)]
        public static void OpenEditorWindow()
        {
            MaterialManager wnd = GetWindow<MaterialManager>();
            wnd.titleContent = new GUIContent("Material Manager");
            wnd.minSize = new Vector2(220, 0);
        }

        void OnDisable()
        {
            SaveSettings();
        }
        
        private void CreateGUI()
        {
            if (EditorApplication.isUpdating == false) // Required to prevent import errors via PackageManager
            {
                LoadUIElements();
                RegisterCallbacks();
                SubscribeToEvents();

                InitGUI();
            }
        }

        #region INIT GUI

                private void LoadUIElements()
        {
                VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("UIDocument/MaterialManager_EditorWindow");
                visualTree.CloneTree(rootVisualElement);
                VisualElement root = rootVisualElement.ElementAt(0);
                
                EditorCustomStyles.SetCustomStyleSheet(root);
                EditorCustomStyles.SetCustomTheme(root);

                // ASSIGN ELEMENTS
                _shaderField = root.Q<ObjectField>("shader-field");
                _findMaterialsButton = root.Q<Button>("find-materials-button");
                _materialsScrollview = root.Q<ScrollView>("materials-scrollview");
                _searchField = root.Q<TextField>("search-field");
                _showVariantsToggle = root.Q<Toggle>("show-variants-toggle");
                _clearSearchButton = root.Q<Button>("clear-search-button");
                _reparentButton = root.Q<Button>("reparent-button");
                _materialsCountLabel = root.Q<Label>("stat-materials-count-label");
                _variantsCountLabel = root.Q<Label>("stat-variant-count-label");
                _reparentCountLabel = root.Q<Label>("stat-reparent-count-label");
                _statsSection = root.Q<VisualElement>("stats-section");
                _selectAllButton = root.Q<Button>("select-all-button");
                _selectVisibleButton = root.Q<Button>("select-visible-button");
                _filterPropertyButton = root.Q<Button>("filter-property-button");
                _clearFilteredPropertyButton = root.Q<Button>("clear-filter-property-button");
                _filtersSection = root.Q<VisualElement>("filters-section");
                _searchInDropdown = root.Q<DropdownField>("search-in-dropdown");
                _folderPathSection = root.Q<VisualElement>("folder-path-section");
                _reparentButtonSection = root.Q<VisualElement>("reparent-button-section");
                _newShaderField = root.Q<ObjectField>("new-shader-field");
                _reparentToggle = root.Q<Toggle>("reparent-toggle");
                _folderBrowseButton = root.Q<Button>("folder-browse-button");
                _folderDefaultButton = root.Q<Button>("folder-default-button");
                _folderPathLabel = root.Q<Label>("folder-path-label");
                _rebindPropertiesButton = root.Q<Button>("rebind-properties-button");
        }

        private void RegisterCallbacks()
        {
            _shaderField.RegisterValueChangedCallback(ShaderField_Changed);
            _newShaderField.RegisterValueChangedCallback(NewShaderField_Changed);
            _searchField.RegisterValueChangedCallback(SearchField_Changed);
            _showVariantsToggle.RegisterValueChangedCallback(ShowVariantsToggle_Changed);
            _searchInDropdown.RegisterValueChangedCallback(SearchIn_Changed);
            
            _searchField.RegisterCallback<FocusInEvent>(SearchField_FocusIn);
            _searchField.RegisterCallback<FocusOutEvent>(SearchField_FocusOut);
        }

        private void SubscribeToEvents()
        {
            _findMaterialsButton.clicked += FindMaterials;
            _reparentButton.clicked += ReparentMaterialsToNewShader;
            _clearSearchButton.clicked += ClearSearchButton;
            _selectVisibleButton.clicked += SelectVisibleMaterialsButton;
            _selectAllButton.clicked += SelectAllMaterialsButton;
            _filterPropertyButton.clicked += OpenPropertyButtonFilter;
            _clearFilteredPropertyButton.clicked += ClearFilteredPropertyButton;
            _folderBrowseButton.clicked += BrowseFolderPathButton;
            _folderDefaultButton.clicked += DefaultFolderPathButton;
            _rebindPropertiesButton.clicked += RebindPropertiesButton;
        }

        private void InitGUI()
        {
            LoadSettings();
        
            // SHADER TO FIND
            if (_shaderField.value != null) // FindMaterials on tool start if any shader had been saved
            {
                FindMaterials();
            }
            else // If no shader had been saved sets everything as disabled by default
            {
                _findMaterialsButton.SetEnabled(false);
                _selectAllButton.SetEnabled(false);
                _selectVisibleButton.SetEnabled(false);
                _filtersSection.SetEnabled(false);
            }
        
            // SEARCH FIELD
            _searchField.value = DefaultSearchField;
            // Find internal child and sets up its default class as placeholder
            _searchFieldText = FindChild(_searchField, "unity-text-element");
            _searchFieldText.RemoveFromClassList("unity-text-element");
            _searchFieldText.AddToClassList("search-field-placeholder");
            
            // TODO : Enable rebind button once working
            _rebindPropertiesButton.SetEnabled(false);
        }

        #endregion INIT GUI
    
        #region CALLBACKS

        private void NewShaderField_Changed(ChangeEvent<Object> evt)
        {
            DisplayUIElement(_reparentButtonSection, _newShaderField.value != null && _shaderField.value != null);
        }

        private void SearchIn_Changed(ChangeEvent<string> evt)
        {
            switch (_searchInDropdown.index)
            {
                case 0:
                    _folderPathSection.style.display = DisplayStyle.Flex;
                    break;
                case 1:
                    _folderPathSection.style.display = DisplayStyle.None;
                    break;
            }
        }
    
        private void ShaderField_Changed(ChangeEvent<Object> evt)
        {
            _findMaterialsButton.SetEnabled(_shaderField.value != null);
            DisplayUIElement(_reparentButtonSection, _newShaderField.value != null && _shaderField.value != null);
            if (_skipClear)
            {
                _skipClear = false;
                return;
            }
            FindMaterial_Clear();
        }
    
        private void SearchField_Changed(ChangeEvent<String> evt)
        {
            if (_ignoreSearchField)
            {
                _ignoreSearchField = false;
            }
            else
            {
                _searchQuery = evt.newValue;
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
            if (_searchField.value == DefaultSearchField) // On FocusIn check if there is default text or user search
            {
                // If default search then remove it and sets its class to user search one
                _ignoreSearchField = true;
                _searchField.value = "";
                _searchFieldText.RemoveFromClassList("search-field-placeholder");
                _searchFieldText.AddToClassList("unity-text-element");
            }
        }

        private void SearchField_FocusOut(FocusOutEvent evt)
        {
            if (string.IsNullOrEmpty(_searchField.value))  // On FocusOut check if search field is blank
            {
                // If blank then resets to default class and value
                _ignoreSearchField = true;
                _searchField.value = DefaultSearchField;
                _searchFieldText.AddToClassList("search-field-placeholder");
                _searchFieldText.RemoveFromClassList("unity-text-element");
            }
        }

        private void ClearSearchButton()
        {
            if (_searchField.value != DefaultSearchField)
            {
                _ignoreSearchField = true;
                _searchField.value = DefaultSearchField;
                _searchQuery = null;
                _searchFieldText.AddToClassList("search-field-placeholder");
                _searchFieldText.RemoveFromClassList("unity-text-element");

                DisplayMaterialsList();
            }
        }
    
        private void SelectAllMaterialsButton()
        {
            Selection.objects = _materialsFound.ToArray();
        }

        private void SelectVisibleMaterialsButton()
        {
            VisibleMaterials();
            Selection.objects = _materialsVisible.ToArray();
        }
    
        private void BrowseFolderPathButton()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Search Folder", "", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                string relativePath = folderPath.Replace(Application.dataPath, "Assets");
                _folderPathLabel.text = relativePath;
            }
        }
        private void DefaultFolderPathButton()
        {
            _folderPathLabel.text = DefaultFolderPath;
        }
    
        private void ClearFilteredPropertyButton()
        {
            _filterPropertyButton.text = "Filter by property override";
            _filteredProperty = null;
            _filteredPropertyName = null;
            _clearFilteredPropertyButton.style.display = DisplayStyle.None;
            DisplayMaterialsList();
        }

        private void OpenPropertyButtonFilter()
        {
            ShaderPropertyFilter.ShowWindow(_shaderField.value, (propertyName, propertyDisplayName) =>
            {
                _filteredProperty = propertyName;
                _filteredPropertyName = propertyDisplayName;
                _filterPropertyButton.text = _filteredPropertyName;
                _clearFilteredPropertyButton.style.display = DisplayStyle.Flex;
                DisplayMaterialsList();
            });
        }

        #endregion
    
        #region DISPLAY GUI

        private void DisplayToggledMaterialField(Material mat)
        {
            VisualElement container = new();
            ObjectField materialField = new()
            {
                value = mat,
                objectType = typeof(Material),
                allowSceneObjects = true,
                focusable = true
            };
        
            VisualElement button = FindChild(materialField, "unity-object-field__selector");
            if (button != null)
            {
                button.style.display = DisplayStyle.None;
            }

            container.Add(materialField);
            _materialsScrollview.Add(container);

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
            _materialsScrollview.Clear();

            foreach (Material mat in _materialsFound)
            {
                if (FilterVariants(mat) || FilterSearchField(mat) || FilterPropertyOverride(mat))
                {
                    continue;
                }

                DisplayToggledMaterialField(mat);
            }
        
            _selectVisibleButton.SetEnabled(_materialsScrollview.contentContainer.childCount > 0);
            _filtersSection.SetEnabled(true);
            DisplayStatsSection(true);
        }

        private void DisplayStatsSection(bool showStats)
        {
            if (showStats)
            {
                _statsSection.style.display = DisplayStyle.Flex;
                _materialsCountLabel.text = "Materials Found: " + _materialsFoundCount.ToString();

                if (_variantsFilteredCount > 0)
                {
                    _variantsCountLabel.text = "Variant Children: " + _variantsFilteredCount.ToString();
                    _variantsCountLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _variantsCountLabel.style.display = DisplayStyle.None;
                }
                if (_showReparentedMaterials)
                {
                    _reparentCountLabel.text = "Materials Reparented: " + _materialsReparentedCount.ToString();
                    _reparentCountLabel.style.display = DisplayStyle.Flex;
                    _showReparentedMaterials = false;
                }
                else
                {
                    _reparentCountLabel.style.display = DisplayStyle.None;
                }
            }
            else
            {
                _statsSection.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region VISUAL ELEMENTS

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

                foreach (VisualElement child in current.Children())
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

        #endregion
    
        #region MATERIAL FILTERS

        private bool FilterVariants(Material mat)
        {
            return !_showVariantsToggle.value && mat.isVariant && mat.parent != _SGchild;
        }
        private bool FilterSearchField(Material mat)
        {
            return !string.IsNullOrEmpty(_searchQuery) && !mat.name.ToLower().Contains(_searchQuery.ToLower());
        }
        private bool FilterPropertyOverride(Material mat)
        {
            return !string.IsNullOrEmpty(_filteredProperty) && !mat.IsPropertyOverriden(_filteredProperty);
        }

        private void VisibleMaterials()
        {
            _materialsVisible.Clear();

            foreach (VisualElement container in _materialsScrollview.Children())
            {
                if (container.childCount > 0 && container[0] is ObjectField materialField && materialField.value is Material mat)
                {
                    bool isVariantFilteredOut = !_showVariantsToggle.value && mat.isVariant && mat.parent != _SGchild;
                    bool isSearchFilteredOut = !string.IsNullOrEmpty(_searchQuery) && !mat.name.ToLower().Contains(_searchQuery.ToLower());
                    bool isFieldEnabled = materialField.enabledSelf;

                    if (!isVariantFilteredOut && !isSearchFilteredOut && isFieldEnabled)
                    {
                        _materialsVisible.Add(mat);
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

            _shaderAssetPath = AssetDatabase.GetAssetPath(_shaderField.value);
            _SGchild = AssetDatabase.LoadAssetAtPath<Material>(_shaderAssetPath);
        
            switch (_searchInDropdown.index)
            {
                case 0:
                    FindMaterials_Path();
                    break;
                case 1:
                    FindMaterials_Scene();
                    break;
            }
        
            _materialsFoundCount = _materialsFound.Count;
        
            _selectAllButton.SetEnabled(_materialsFoundCount > 0);
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
            string searchPath = !string.IsNullOrEmpty(_folderPathLabel.text) ? _folderPathLabel.text : "Assets";
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
            if (material != null && material.shader == _shaderField.value && !_materialsFound.Contains(material) && material != _SGchild)
            {
                if (material.isVariant && material.parent != _SGchild)
                {
                    _toRefresh.Add(material);
                    _variantsFilteredCount++;
                }
                _materialsFound.Add(material);
            }
        }
    
        private void FindMaterial_Clear()
        {
            _materialsFound.Clear();
            _toRefresh.Clear();
            _materialsVisible.Clear();
            _variantsFilteredCount = 0;
            _filteredProperty = null;
            _filteredPropertyName = null;
            _materialsScrollview.Clear();
            ClearSearchButton();
            DisplayStatsSection(false);
            _selectAllButton.SetEnabled(false);
            _selectVisibleButton.SetEnabled(false);
            _filtersSection.SetEnabled(false);
        }

        #endregion
    
        #region REPARENT SHADER

        private void ReparentMaterialsToNewShader()
        {
            _materialsReparentedCount = 0;
            
            if (_reparentToggle.value)
            {
                VisibleMaterials();
            }
            
            FindAncestors();
            ReparentAncestors();
            
            Selection.objects = _toRefresh.ToArray();
            _showReparentedMaterials = true;
            _delayStartTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += DelayRefresh;
        }

        // TODO : Implement rebind for ancestors and overidden properties of variants materials
        private void RebindPropertiesButton()
        {
            ShaderPropertyRebind.ShowWindow(_shaderField.value, _newShaderField.value);
            Debug.Log("Found " + ShaderPropertyRebind.ReboundRefs.Count + " properties to rebind");
        }

        private void FindAncestors()
        {
            _matAncestors.Clear();
            foreach (Material mat in _reparentToggle.value ? _materialsVisible : _materialsFound)
            {
                Material currentMat = mat;
                while (currentMat.isVariant && currentMat.parent != _SGchild)
                {
                    currentMat = currentMat.parent;
                }

                if (!_matAncestors.Contains(currentMat))
                {
                    _matAncestors.Add(currentMat);
                }
            }
        }
        
        private void ReparentAncestors()
        {
            Material newShaderMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(_newShaderField.value));
        
            foreach (Material mat in _matAncestors)
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
                
                _materialsReparentedCount++;
            }
        }

        private void DelayRefresh()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            if (currentTime - _delayStartTime >= DelayDuration)
            {
                EditorApplication.update -= DelayRefresh;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Materials have been reparented to new shader. Total Reparented: " + _materialsReparentedCount);
            
                FindMaterials();
            }
        }

        #endregion

        #region SAVE/LOAD

        void SaveSettings()
        {
            if (!string.IsNullOrEmpty(_folderPathLabel.text))
            {
                EditorPrefs.SetString("ShaderManager_SearchFolder", _folderPathLabel.text);
            }

            EditorPrefs.SetString("ShaderManager_shaderField.value", AssetDatabase.GetAssetPath(_shaderField.value));

            EditorPrefs.SetBool("ShaderManager_FilterState", _showVariantsToggle.value);
        
            EditorPrefs.SetInt("ShaderManager_SearchInOption", _searchInDropdown.index);
        }
        void LoadSettings()
        {
            _folderPathLabel.text = EditorPrefs.GetString("ShaderManager_SearchFolder", DefaultFolderPath);

            string shaderPath = EditorPrefs.GetString("ShaderManager_shaderField.value", "");
            _shaderField.value = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

            _showVariantsToggle.value = EditorPrefs.GetBool("ShaderManager_FilterState", true);
        
            _searchInDropdown.index = EditorPrefs.GetInt("ShaderManager_SearchInOption", 0);
        }

        #endregion

        #region PUBLIC METHODS

        public void FindByShader(Shader shader)
        {
            if (shader == null) return;
            
            _searchInDropdown.index = 1;
            
            if (_shaderField.value != shader)
            {
                _skipClear = true;
                _shaderField.value = shader;
            }

            FindMaterials();
        }

        #endregion
    }
}