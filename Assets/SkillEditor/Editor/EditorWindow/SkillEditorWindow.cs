using System.Collections.Generic;
using Config.Skill;
using SkillEditor.Editor.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using SkillEditor.Editor.Track.AnimationTrack;

public class SkillEditorWindow : EditorWindow
{
    public static SkillEditorWindow Instance;
    
    private VisualElement root;

    [MenuItem("Skill Editor/Skill Editor Window")]
    public static void ShowExample()
    {
        SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("技能编辑器");
    }

    public void CreateGUI()
    {
        Instance = this;
            
        root = rootVisualElement;

        // Import UXML
        var visualTree =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/SkillEditor/Editor/EditorWindow/SkillEditorWindow.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);

        InitTopMenu();
        InitTimerShaft();
        InitConsole();
        InitContent();

        if (skillConfig != null)
        {
            SkillConfigObjectField.value = skillConfig;
            CurrentFrameCount = skillConfig.FrameCount;
        }
        else
        {
            CurrentFrameCount = 100;
        }
        CurrentSelectFrameIndex = 0;
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

    /// <summary>
    /// 技能配置修改事件
    /// </summary>
    private void OnSkillConfigValueChanged(ChangeEvent<Object> evt)
    {
        skillConfig = evt.newValue as SkillConfig;
        
        // 重新绘制
        ResetTrack();
        CurrentSelectFrameIndex = 0;
        if (skillConfig == null)
        {
            CurrentFrameCount = 100;
            return;
        }
        CurrentFrameCount = skillConfig.FrameCount;
    }

    #endregion

    #region TimerShaft

    private IMGUIContainer TimerShaft;
    private IMGUIContainer SelectLine;
    private VisualElement contentContainer;
    private VisualElement contentViewport;
    private int currentSelectFrameIndex = -1;
    private int CurrentSelectFrameIndex
    {
        get => currentSelectFrameIndex;
        set
        {
            // 如果超出范围，更新最大帧
            if (value > CurrentFrameCount)
                CurrentFrameCount = value;
            currentSelectFrameIndex = Mathf.Clamp(value, 0, CurrentFrameCount);
            CurrentFrameText.value = currentSelectFrameIndex.ToString();
            UpdateTimerShaftView();
        }
    }

    private int currentFrameCount;

    public int CurrentFrameCount
    {
        get => currentFrameCount;
        set
        {
            currentFrameCount = value;
            FrameCountText.value = currentFrameCount.ToString();
            // 同步给SkillConfig
            if (skillConfig != null)
            {
                skillConfig.FrameCount = currentFrameCount;
                SaveSkillConfig();
            }
            
            // Content size change
            UpdateContentSize();
        }
    }
    
    private float CurrentSelectFramePosX => currentSelectFrameIndex * skillEditorConfig.currentFrameUnitWidth;
    
    private float ContentOffsetPosX => Mathf.Abs(contentContainer.transform.position.x);

    private bool isTimerShaftMouseEnter = false;
    
    private void InitTimerShaft()
    {
        ScrollView mainContentView = root.Q<ScrollView>("MainContentView");
        contentContainer = mainContentView.Q<VisualElement>("unity-content-container");
        contentViewport = mainContentView.Q<VisualElement>("unity-content-viewport");
        
        TimerShaft = root.Q<IMGUIContainer>(nameof(TimerShaft));
        TimerShaft.onGUIHandler = DrawTimerShaft;
        TimerShaft.RegisterCallback<WheelEvent>(OnTimerShaftWheel);
        TimerShaft.RegisterCallback<MouseDownEvent>(OnTimerShaftMouseDown);
        TimerShaft.RegisterCallback<MouseMoveEvent>(OnTimerShaftMouseMove);
        TimerShaft.RegisterCallback<MouseUpEvent>(OnTimerShaftMouseUp);
        TimerShaft.RegisterCallback<MouseOutEvent>(OnTimerShaftMouseOut);

        SelectLine = root.Q<IMGUIContainer>(nameof(SelectLine));
        SelectLine.onGUIHandler = DrawSelectLine;
    }

    private void DrawTimerShaft()
    {
        Handles.BeginGUI();
        Handles.color = Color.white;
        var rect = TimerShaft.contentRect;

        // 计算起始索引
        int index = Mathf.CeilToInt(ContentOffsetPosX / skillEditorConfig.currentFrameUnitWidth);
        // 计算绘制起点偏移
        float startDrawOffset = 0;
        if (index > 0)
            startDrawOffset = skillEditorConfig.currentFrameUnitWidth -
                              ContentOffsetPosX % skillEditorConfig.currentFrameUnitWidth;

        int tickStep = SkillEditorConfig.maxFrameWidthLv + 1 - (skillEditorConfig.currentFrameUnitWidth / SkillEditorConfig.defaultFrameUnitWidth);
        tickStep /= 2;
        if (tickStep == 0) tickStep = 1; // 避免为0
        for (float i = startDrawOffset; i < rect.width; i += skillEditorConfig.currentFrameUnitWidth)
        {
            if (index % tickStep == 0)
            {
                Handles.DrawLine(new Vector3(i, rect.height - 10), new Vector3(i, rect.height));
                string indexStr = index.ToString();
                GUI.Label(new Rect(i - indexStr.Length * 4.5f, 0, 35, 20), indexStr);
            }
            else
            {
                Handles.DrawLine(new Vector3(i, rect.height - 5), new Vector3(i, rect.height));
            }

            index++;
        }

        Handles.EndGUI();
    }
    
    private void OnTimerShaftWheel(WheelEvent evt)
    {
        int delta = (int)evt.delta.y;
        skillEditorConfig.currentFrameUnitWidth = Mathf.Clamp(
            skillEditorConfig.currentFrameUnitWidth - delta,
            SkillEditorConfig.defaultFrameUnitWidth,
            SkillEditorConfig.maxFrameWidthLv * SkillEditorConfig.defaultFrameUnitWidth);
        
        UpdateTimerShaftView();
        UpdateContentSize();
    }
    
    private void OnTimerShaftMouseDown(MouseDownEvent evt)
    {
        // 让选中线的位置停留在帧的位置上
        isTimerShaftMouseEnter = true;
        CurrentSelectFrameIndex = GetFrameIndexByMousePos(evt.localMousePosition.x);
    }
    
    private void OnTimerShaftMouseMove(MouseMoveEvent evt)
    {
        if (isTimerShaftMouseEnter)
        {
            CurrentSelectFrameIndex = GetFrameIndexByMousePos(evt.localMousePosition.x);
        }
    }
    
    private void OnTimerShaftMouseUp(MouseUpEvent evt)
    {
        isTimerShaftMouseEnter = false;
    }
    
    private void OnTimerShaftMouseOut(MouseOutEvent evt)
    {
        isTimerShaftMouseEnter = false;
    }

    private int GetFrameIndexByMousePos(float x)
    {
        return GetFrameIndexByPos(x + ContentOffsetPosX);
    }

    public int GetFrameIndexByPos(float x)
    {
        return Mathf.RoundToInt(x / skillEditorConfig.currentFrameUnitWidth);
    }
    
    private void DrawSelectLine()
    {
        // 判断当前选中帧是否在视图范围内
        if (CurrentSelectFramePosX >= ContentOffsetPosX)
        {
            Handles.BeginGUI();
            Handles.color = Color.red;
            float x = CurrentSelectFramePosX - ContentOffsetPosX;
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, contentViewport.contentRect.height + TimerShaft.contentRect.height));
            Handles.EndGUI();
        }
    }

    private void UpdateTimerShaftView()
    {
        TimerShaft.MarkDirtyLayout();
        SelectLine.MarkDirtyLayout();
    }

    #endregion
    
    #region Console

    private Button PreviousFrameBtn;
    private Button PlayBtn;
    private Button NextFrameBtn;
    private TextField CurrentFrameText;
    private TextField FrameCountText;

    private void InitConsole()
    {
        PreviousFrameBtn = root.Q<Button>(nameof(PreviousFrameBtn));
        PreviousFrameBtn.clicked += OnPreviousFrameBtnClicked;
        PlayBtn = root.Q<Button>(nameof(PlayBtn));
        PlayBtn.clicked += OnPlayBtnClicked;
        NextFrameBtn = root.Q<Button>(nameof(NextFrameBtn));
        NextFrameBtn.clicked += OnNextFrameBtnClicked;
        
        CurrentFrameText = root.Q<TextField>(nameof(CurrentFrameText));
        CurrentFrameText.RegisterValueChangedCallback(OnCurrentFrameValueChanged);
        FrameCountText = root.Q<TextField>(nameof(FrameCountText));
        FrameCountText.RegisterValueChangedCallback(OnFrameCountValueChanged);
    }

    private void OnPreviousFrameBtnClicked()
    {
        CurrentSelectFrameIndex--;
    }

    private void OnPlayBtnClicked()
    {
        // TODO: play
    }

    private void OnNextFrameBtnClicked()
    {
        CurrentSelectFrameIndex++;
    }

    private void OnCurrentFrameValueChanged(ChangeEvent<string> evt)
    {
        CurrentSelectFrameIndex = int.Parse(evt.newValue);
    }

    private void OnFrameCountValueChanged(ChangeEvent<string> evt)
    {
        CurrentFrameCount = int.Parse(evt.newValue);
    }
    
    #endregion

    #region Config

    private SkillConfig skillConfig;
    public SkillConfig SkillConfig => skillConfig;
    private SkillEditorConfig skillEditorConfig = new();

    public void SaveSkillConfig()
    {
        if (skillConfig != null)
        {
            EditorUtility.SetDirty(skillConfig);
            AssetDatabase.SaveAssetIfDirty(skillConfig);
        }
    }

    #endregion
    
    #region Track

    private VisualElement trackMenuParent;
    private VisualElement ContentListView;
    private List<SkillTrackBase> trackItems = new();
    
    private void InitContent()
    {
        trackMenuParent = root.Q<VisualElement>("TrackMenu");
        ContentListView = root.Q<VisualElement>(nameof(ContentListView));
        UpdateContentSize();
        InitTrack();
    }

    private void InitTrack()
    {
        InitAnimationTrack();
    }

    private void ResetTrack()
    {
        foreach (var trackItem in trackItems)
        {
            trackItem.ResetView(skillEditorConfig.currentFrameUnitWidth);
        }
    }

    private void UpdateContentSize()
    {
        ContentListView.style.width = skillEditorConfig.currentFrameUnitWidth * CurrentFrameCount;
    }

    private void InitAnimationTrack()
    {
        AnimationTrack animationTrack = new();
        animationTrack.Init(trackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
        trackItems.Add(animationTrack);
    }
    
    #endregion
}

public class SkillEditorConfig
{
    public const int defaultFrameUnitWidth = 10;
    public int currentFrameUnitWidth = 10;
    public const int maxFrameWidthLv = 10;
}