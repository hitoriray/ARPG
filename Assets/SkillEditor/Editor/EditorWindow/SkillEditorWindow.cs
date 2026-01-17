using Config.Skill;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class SkillEditorWindow : EditorWindow
{
    private VisualElement root;
    
    [MenuItem("Skill Editor/Skill Editor Window")]
    public static void ShowExample()
    {
        SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("技能编辑器");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SkillEditor/Editor/EditorWindow/SkillEditorWindow.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
        
        InitTopMenu();
    }
    
    #region TopMenu

    private const string skillEditorScenePath = "Assets/SkillEditor/SkillEditorScene.unity";
    private const string previewCharacterParentPath = "PreviewCharacterRoot";
    private string oldScenePath;
    private Button LoadEditorSceneBtn;
    private Button LoadOldSceneBtn;
    private Button SkillBasicBtn;
    private ObjectField PreviewCharacterPrefabObjectField;
    private ObjectField SkillConfigObjectField;
    
    private GameObject currentPreviewCharacterObj;
    
    private void InitTopMenu()
    {
        InitTopMenuObjectFields();
        
        BindTopMenuEvents();
    }

    private void BindTopMenuEvents()
    {
        LoadEditorSceneBtn = root.Q<Button>(nameof(LoadEditorSceneBtn));
        LoadEditorSceneBtn.clicked += OnLoadEditorSceneBtnClicked;
        LoadOldSceneBtn = root.Q<Button>(nameof(LoadOldSceneBtn));
        LoadOldSceneBtn.clicked += OnLoadOldSceneBtnClicked;
        SkillBasicBtn = root.Q<Button>(nameof(SkillBasicBtn));
        SkillBasicBtn.clicked += OnSkillBasicBtnClicked;

        PreviewCharacterPrefabObjectField.RegisterValueChangedCallback(OnPreviewCharacterPrefabValueChanged);
        SkillConfigObjectField.RegisterValueChangedCallback(OnSkillConfigValueChanged);
    }

    private void InitTopMenuObjectFields()
    {
        var topMenu = root.Q<VisualElement>("Top");
        if (topMenu == null)
        {
            Debug.LogError("Top Menu not found in UXML.");
            return;
        }

        PreviewCharacterPrefabObjectField = new ObjectField("演示角色预制体")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false,
        };
        PreviewCharacterPrefabObjectField.AddToClassList("compact-object-field");
        PreviewCharacterPrefabObjectField.style.flexGrow = 1;
        PreviewCharacterPrefabObjectField.style.flexShrink = 1;
        PreviewCharacterPrefabObjectField.style.minWidth = 0;
        PreviewCharacterPrefabObjectField.style.alignItems = new StyleEnum<Align>(Align.Center);
        
        SkillConfigObjectField = new ObjectField("技能配置文件")
        {
            objectType = typeof(SkillConfig),
            allowSceneObjects = false,
        };
        SkillConfigObjectField.AddToClassList("compact-object-field");
        SkillConfigObjectField.style.flexGrow = 1;
        SkillConfigObjectField.style.flexShrink = 1;
        SkillConfigObjectField.style.minWidth = 0;
        SkillConfigObjectField.style.alignItems = new StyleEnum<Align>(Align.Center);
        
        topMenu.Add(PreviewCharacterPrefabObjectField);
        topMenu.Add(SkillConfigObjectField);
    }
    
    /// <summary>
    /// 加载编辑器场景
    /// </summary>
    private void OnLoadEditorSceneBtnClicked()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (currentScenePath == skillEditorScenePath)
            return;
        oldScenePath = currentScenePath;
        EditorSceneManager.OpenScene(skillEditorScenePath);
    }

    // 返回上一个场景
    private void OnLoadOldSceneBtnClicked()
    {
        if (string.IsNullOrEmpty(oldScenePath))
            return;
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (currentScenePath == oldScenePath)
            return;
        EditorSceneManager.OpenScene(oldScenePath);
        oldScenePath = currentScenePath;
    }

    // 加载技能基本信息
    private void OnSkillBasicBtnClicked()
    {
        if (skillConfig != null)
        {
            Selection.activeObject = skillConfig;
        }
    }
    
    private void OnPreviewCharacterPrefabValueChanged(ChangeEvent<Object> evt)
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (currentScenePath != skillEditorScenePath)
            return;
        
        if (currentPreviewCharacterObj != null)
            DestroyImmediate(currentPreviewCharacterObj);
        
        Transform parent = GameObject.Find(previewCharacterParentPath).transform;
        if (parent != null && parent.childCount > 0)
        {
            DestroyImmediate(parent.GetChild(0).gameObject);
        }

        if (evt.newValue != null)
        {
            currentPreviewCharacterObj = Instantiate(evt.newValue as GameObject, parent);
        }
    }
    
    private void OnSkillConfigValueChanged(ChangeEvent<Object> evt)
    {
        skillConfig = evt.newValue as SkillConfig;
        // TODO: re draw
    }
    
    #endregion
    
    #region Config
    private SkillConfig skillConfig;
    #endregion
}