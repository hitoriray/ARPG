using System;
using System.Collections.Generic;
using Config;
using Skill;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SkillEditor
{
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
            SkillClip.SetSkillClipValidateAction(ResetView);
            
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

            if (skillClip != null)
            {
                SkillConfigObjectField.value = skillClip;
                CurrentFrameCount = skillClip.FrameCount;
            }
            else
            {
                CurrentFrameCount = 100;
            }

            // 防止因为编辑器刷新导致演示预制体消失
            if (currentPreviewCharacterPrefab != null)
            {
                PreviewCharacterPrefabObjectField.value = currentPreviewCharacterPrefab;
            }
            if (currentPreviewCharacterObj != null)
            {
                PreviewCharacterObjectField.value = currentPreviewCharacterObj;
            }

            CurrentSelectFrameIndex = 0;
        }

        private void ResetView()
        {
            // ResetTrackData();
            // UpdateContentSize();
            // ResetTrack();
            var tmpConfig = skillClip;
            SkillConfigObjectField.value = null;
            SkillConfigObjectField.value = tmpConfig;
        }

        private void OnEnable()
        {
            SceneView.beforeSceneGui += OnSceneGUI;
        }

        // OnDestroy有一个问题：窗口销毁会调用，但是直接关闭Unity不会调用
        // 因此改为OnDisable
        private void OnDisable()
        {
            if (skillClip != null)
            {
                SaveSkillConfig();
            }
            SceneView.beforeSceneGui -= OnSceneGUI;
        }

        #region TopMenu

        private const string skillEditorScenePath = "Assets/SkillEditor/SkillEditorScene.unity";
        private const string previewCharacterParentPath = "PreviewCharacterRoot";
        private string oldScenePath;
        private Button LoadEditorSceneBtn;
        private Button LoadOldSceneBtn;
        private Button SkillBasicBtn;
        private ObjectField PreviewCharacterPrefabObjectField;
        private ObjectField PreviewCharacterObjectField;
        private ObjectField SkillConfigObjectField;

        private GameObject currentPreviewCharacterPrefab;
        
        private GameObject currentPreviewCharacterObj;
        public GameObject CurrentPreviewCharacterObj => currentPreviewCharacterObj;

        private void InitTopMenu()
        {
            InitTopMenuObjectFields();

            BindTopMenuEvents();
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
                allowSceneObjects = true,
            };
            PreviewCharacterPrefabObjectField.AddToClassList("compact-object-field");
            PreviewCharacterPrefabObjectField.style.flexGrow = 1;
            PreviewCharacterPrefabObjectField.style.flexShrink = 1;
            PreviewCharacterPrefabObjectField.style.minWidth = 0;
            PreviewCharacterPrefabObjectField.style.alignItems = new StyleEnum<Align>(Align.Center);
            topMenu.Add(PreviewCharacterPrefabObjectField);

            PreviewCharacterObjectField = new ObjectField("演示角色物体")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
            };
            PreviewCharacterObjectField.AddToClassList("compact-object-field");
            PreviewCharacterObjectField.style.flexGrow = 1;
            PreviewCharacterObjectField.style.flexShrink = 1;
            PreviewCharacterObjectField.style.minWidth = 0;
            PreviewCharacterObjectField.style.alignItems = new StyleEnum<Align>(Align.Center);
            topMenu.Add(PreviewCharacterObjectField);
            
            SkillConfigObjectField = new ObjectField("技能配置文件")
            {
                objectType = typeof(SkillClip),
                allowSceneObjects = true,
            };
            SkillConfigObjectField.AddToClassList("compact-object-field");
            SkillConfigObjectField.style.flexGrow = 1;
            SkillConfigObjectField.style.flexShrink = 1;
            SkillConfigObjectField.style.minWidth = 0;
            SkillConfigObjectField.style.alignItems = new StyleEnum<Align>(Align.Center);
            topMenu.Add(SkillConfigObjectField);
        }
        
        private void BindTopMenuEvents()
        {
            LoadEditorSceneBtn = root.Q<Button>(nameof(LoadEditorSceneBtn));
            LoadEditorSceneBtn.clicked += OnLoadEditorSceneBtnClicked;
            LoadOldSceneBtn = root.Q<Button>(nameof(LoadOldSceneBtn));
            LoadOldSceneBtn.clicked += OnLoadOldSceneBtnClicked;
            SkillBasicBtn = root.Q<Button>(nameof(SkillBasicBtn));
            SkillBasicBtn.clicked += OnSkillBasicBtnClicked;

            PreviewCharacterPrefabObjectField.RegisterValueChangedCallback(OnPreviewCharacterPrefabObjectValueChanged);
            PreviewCharacterObjectField.RegisterValueChangedCallback(OnPreviewCharacterObjectValueChanged);
            SkillConfigObjectField.RegisterValueChangedCallback(OnSkillConfigValueChanged);
        }

        public bool IsInEditorScene
        {
            get
            {
                string currentScenePath = EditorSceneManager.GetActiveScene().path;
                return currentScenePath == skillEditorScenePath;
            }
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
            if (skillClip != null)
            {
                Selection.activeObject = skillClip;
            }
        }

        private void OnPreviewCharacterPrefabObjectValueChanged(ChangeEvent<Object> evt)
        {
            // 避免在其他场景实例化
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            if (currentScenePath != skillEditorScenePath)
            {
                PreviewCharacterPrefabObjectField.value = null;
                return;
            }

            // 如果值相等，return掉
            if (evt.newValue == currentPreviewCharacterPrefab)
                return;

            currentPreviewCharacterPrefab = evt.newValue as GameObject; 
            
            // 销毁旧的
            if (currentPreviewCharacterObj != null)
                DestroyImmediate(currentPreviewCharacterObj);
            Transform parent = GameObject.Find(previewCharacterParentPath).transform;
            if (parent != null && parent.childCount > 0)
            {
                DestroyImmediate(parent.GetChild(0).gameObject);
            }

            // 实例化新的
            if (evt.newValue != null)
            {
                currentPreviewCharacterObj = Instantiate(evt.newValue as GameObject, parent);
                currentPreviewCharacterObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
                PreviewCharacterObjectField.value = currentPreviewCharacterObj;
                if (currentPreviewCharacterObj.GetComponent<SkillPlayer>() == null)
                {
                    currentPreviewCharacterObj.AddComponent<SkillPlayer>();
                }
            }
        }

        private void OnPreviewCharacterObjectValueChanged(ChangeEvent<Object> evt)
        {
            currentPreviewCharacterObj = evt.newValue as GameObject;
        }

        /// <summary>
        /// 技能配置修改事件
        /// </summary>
        private void OnSkillConfigValueChanged(ChangeEvent<Object> evt)
        {
            SaveSkillConfig();
            
            skillClip = evt.newValue as SkillClip;
            // 重新绘制
            CurrentSelectFrameIndex = 0;
            if (skillClip == null)
            {
                CurrentFrameCount = 100;
            }
            else
            {
                CurrentFrameCount = skillClip.FrameCount;
            }
            
            ResetTrack();
        }

        #endregion

        #region TimerShaft

        private IMGUIContainer TimerShaft;
        private IMGUIContainer SelectLine;
        private VisualElement contentContainer;
        private VisualElement contentViewport;
        private int currentSelectFrameIndex = -1;
        public int CurrentSelectFrameIndex
        {
            get => currentSelectFrameIndex;
            private set
            {
                int oldValue = currentSelectFrameIndex;
                // 如果超出范围，更新最大帧
                if (value > CurrentFrameCount)
                    CurrentFrameCount = value;
                currentSelectFrameIndex = Mathf.Clamp(value, 0, CurrentFrameCount);
                CurrentFrameField.value = currentSelectFrameIndex;
                // 避免重复调用
                if (oldValue != currentSelectFrameIndex)
                {
                    UpdateTimerShaftView();
                    TickSkill();
                }
            }
        }

        private int currentFrameCount;

        public int CurrentFrameCount
        {
            get => currentFrameCount;
            set
            {
                currentFrameCount = value;
                FrameCountField.value = currentFrameCount;
                // 同步给SkillConfig
                if (skillClip != null)
                {
                    skillClip.FrameCount = currentFrameCount;
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
            ResetTrack();
        }
    
        private void OnTimerShaftMouseDown(MouseDownEvent evt)
        {
            // 让选中线的位置停留在帧的位置上
            isTimerShaftMouseEnter = true;
            IsPlaying = false;
            int newValue = GetFrameIndexByMousePos(evt.localMousePosition.x);
            if (newValue != CurrentSelectFrameIndex)
                CurrentSelectFrameIndex = newValue;
        }
    
        private void OnTimerShaftMouseMove(MouseMoveEvent evt)
        {
            if (isTimerShaftMouseEnter)
            {
                int newValue = GetFrameIndexByMousePos(evt.localMousePosition.x);
                if (newValue != CurrentSelectFrameIndex)
                    CurrentSelectFrameIndex = newValue;
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

        public int GetFrameIndexByMousePos(float x)
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
        private VisualElement FramerController;
        private IntegerField CurrentFrameField;
        private Label SeparatorLabel;
        private IntegerField FrameCountField;

        private void InitConsole()
        {
            PreviousFrameBtn = root.Q<Button>(nameof(PreviousFrameBtn));
            PreviousFrameBtn.clicked += OnPreviousFrameBtnClicked;
            PlayBtn = root.Q<Button>(nameof(PlayBtn));
            PlayBtn.clicked += OnPlayBtnClicked;
            NextFrameBtn = root.Q<Button>(nameof(NextFrameBtn));
            NextFrameBtn.clicked += OnNextFrameBtnClicked;
        
            FramerController = root.Q<VisualElement>(nameof(FramerController));
            
            CurrentFrameField = new IntegerField();
            CurrentFrameField.style.width = 40;
            CurrentFrameField.style.height = 20;
            CurrentFrameField.RegisterValueChangedCallback(OnCurrentFrameValueChanged);
            FramerController.Add(CurrentFrameField);

            SeparatorLabel = new Label("/");
            SeparatorLabel.style.fontSize = 15;
            FramerController.Add(SeparatorLabel);
            
            FrameCountField = new IntegerField();
            FrameCountField.style.width = 40;
            FrameCountField.style.height = 20;
            FrameCountField.RegisterValueChangedCallback(OnFrameCountValueChanged);
            FramerController.Add(FrameCountField);
        }

        private void OnPreviousFrameBtnClicked()
        {
            IsPlaying = false;
            CurrentSelectFrameIndex--;
        }

        private void OnPlayBtnClicked()
        {
            IsPlaying = !IsPlaying;
        }

        private void OnNextFrameBtnClicked()
        {
            IsPlaying = false;
            CurrentSelectFrameIndex++;
        }

        private void OnCurrentFrameValueChanged(ChangeEvent<int> evt)
        {
            int newValue = evt.newValue;
            if (CurrentSelectFrameIndex != newValue)
                CurrentSelectFrameIndex = newValue;
        }

        private void OnFrameCountValueChanged(ChangeEvent<int> evt)
        {
            int newValue = evt.newValue;
            if (CurrentFrameCount != newValue)
                CurrentFrameCount = newValue;
        }
    
        #endregion

        #region Config

        private SkillClip skillClip;
        public SkillClip SkillClip => skillClip;
        private SkillEditorConfig skillEditorConfig = new();

        public void SaveSkillConfig()
        {
            if (skillClip != null)
            {
                EditorUtility.SetDirty(skillClip);
                AssetDatabase.SaveAssetIfDirty(skillClip);
                ResetTrackData();
            }
        }

        private void ResetTrackData()
        {
            // 重新引用一下数据
            for (int i = 0; i < trackItemList.Count; i++)
            {
                trackItemList[i].OnConfigChanged();
            }
        }

        #endregion
    
        #region Track

        private VisualElement TrackMenuParent;
        private VisualElement ContentListView;
        private readonly List<SkillTrackBase> trackItemList = new();
    
        private void InitContent()
        {
            ContentListView = root.Q<VisualElement>(nameof(ContentListView));
            TrackMenuParent = root.Q<VisualElement>("TrackMenu");
            ScrollView trackMenuScrollView = root.Q<ScrollView>("TrackMenuScrollView");
            ScrollView mainContentScrollView = root.Q<ScrollView>("MainContentView");
            trackMenuScrollView.verticalScroller.valueChanged += (value) =>
            {
                mainContentScrollView.verticalScroller.value = value;
            };
            mainContentScrollView.verticalScroller.valueChanged += (value) =>
            {
                trackMenuScrollView.verticalScroller.value = value;
            };
            
            UpdateContentSize();
            InitTrack();
        }

        private void InitTrack()
        {
            // 如果没有配置，不需要初始化轨道
            if (skillClip == null)
                return;
            InitAnimationTrack();
            InitEventTrack();
            InitAudioTrack();
            InitEffectTrack();
            InitAttackDetectionTrack();
        }
        
        private void InitAnimationTrack()
        {
            AnimationTrack animationTrack = new();
            animationTrack.Init(TrackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
            trackItemList.Add(animationTrack);
            getPositionForRootMotionAction = animationTrack.GetPositionForRootMotion;
        }

        private void InitAudioTrack()
        {
            AudioTrack audioTrack = new();
            audioTrack.Init(TrackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
            trackItemList.Add(audioTrack);
        }
        
        private void InitEffectTrack()
        {
            EffectTrack effectTrack = new();
            effectTrack.Init(TrackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
            trackItemList.Add(effectTrack);
        }

        private void InitAttackDetectionTrack()
        {
            AttackDetectionTrack attackDetectionTrack = new();
            attackDetectionTrack.Init(TrackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
            trackItemList.Add(attackDetectionTrack);
        }

        private void InitEventTrack()
        {
            EventTrack eventTrack = new();
            eventTrack.Init(TrackMenuParent, ContentListView, skillEditorConfig.currentFrameUnitWidth);
            trackItemList.Add(eventTrack);
        }

        private void ResetTrack()
        {
            // 如果配置文件为空，则清空所有轨道
            if (skillClip == null)
            {
                DestroyTracks();
                return;
            }
            // 如果轨道列表里没有数据，说明没有轨道，但是当前用户是有配置的，所以需要初始化轨道
            if (trackItemList.Count == 0)
                InitTrack();
            // 更新视图
            foreach (var trackItem in trackItemList)
            {
                trackItem.ResetView(skillEditorConfig.currentFrameUnitWidth);
            }
        }

        private void DestroyTracks()
        {
            foreach (var trackItem in trackItemList)
            {
                trackItem.Destroy();
            }
            trackItemList.Clear();
        }

        private void UpdateContentSize()
        {
            ContentListView.style.width = skillEditorConfig.currentFrameUnitWidth * CurrentFrameCount;
        }

        public void ShowTrackItemOnInspector(TrackItemBase trackItem, SkillTrackBase track)
        {
            SkillEditorInspector.SetTrackItem(trackItem, track);
            Selection.activeObject = this;
        }
    
        #endregion
    
        #region Preview

        private bool isPlaying;

        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                isPlaying = value;
                if (isPlaying)
                {
                    startTime = DateTime.Now;
                    startFrameIndex = currentSelectFrameIndex;
                    
                    // OnPlay
                    foreach (var trackItem in trackItemList)
                    {
                        trackItem.OnPlay(currentSelectFrameIndex);
                    }
                }
                else
                {
                    // OnStop
                    foreach (var trackItem in trackItemList)
                    {
                        trackItem.OnStop();
                    }
                }
            }
        }

        private DateTime startTime;
        private int startFrameIndex;

        private void Update()
        {
            if (IsPlaying)
            {
                // 得到时间差
                float dt = (float)DateTime.Now.Subtract(startTime).TotalSeconds;
                // 确定时间轴的帧率
                float frameRate;
                if (skillClip != null) frameRate = skillClip.FrameRate;
                else frameRate = skillEditorConfig.defaultFrameRate;
                // 根据时间差计算当前的选中帧
                CurrentSelectFrameIndex = (int)(dt * frameRate + startFrameIndex);
                // 到达最后一帧自动暂停
                if (CurrentSelectFrameIndex == CurrentFrameCount)
                {
                    IsPlaying = false;
                }
            }
        }

        public void TickSkill()
        {
            // 驱动技能表现
            if (skillClip != null && currentPreviewCharacterObj != null)
            {
                foreach (var trackItem in trackItemList)
                {
                    trackItem.TickView(currentSelectFrameIndex);
                }
            }
        }

        private Func<int, bool, Vector3> getPositionForRootMotionAction;
        public Vector3 GetPositionForRootMotion(int frameIndex, bool recover = false) => getPositionForRootMotionAction(frameIndex, recover);

        #endregion
        
        #region Gizmo & SceneGUI

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawGizmos(SkillPlayer skillPlayer, GizmoType gizmoType)
        {
            if (Instance == null || Instance.currentPreviewCharacterObj == null || Instance.currentPreviewCharacterObj.GetComponent<SkillPlayer>() != skillPlayer)
                return;

            foreach (var item in Instance.trackItemList)
            {
                item.DrawGizmos();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (currentPreviewCharacterObj == null)
                return;
            
            foreach (var item in Instance.trackItemList)
            {
                item.OnSceneGUI();
            }
        }
        
        #endregion
    }

    public class SkillEditorConfig
    {
        public const int defaultFrameUnitWidth = 10;
        public const int maxFrameWidthLv = 10;
        public int currentFrameUnitWidth = 10;
        public float defaultFrameRate = 10;
    }
}