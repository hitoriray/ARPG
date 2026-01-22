using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Skill;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SkillEditor
{
    [CustomEditor(typeof(SkillEditorWindow))]
    public class SkillEditorInspector : Editor
    {
        public static SkillEditorInspector Instance;
        private static TrackItemBase currentTrackItem;
        public static TrackItemBase CurrentTrackItem => currentTrackItem;
        private static SkillTrackBase currentTrack; 
        public static void SetTrackItem(TrackItemBase trackItem, SkillTrackBase track)
        {
            if (currentTrackItem != null)
            {
                currentTrackItem.OnUnselect();
            }

            currentTrackItem = trackItem;
            currentTrackItem.OnSelect();
            currentTrack = track;
            // 避免打开了Inspector，不刷新数据
            if (Instance != null)
            {
                Instance.Show();
            }
        }

        private void OnDestroy()
        {
            if (currentTrackItem != null)
            {
                currentTrackItem.OnUnselect();
                currentTrackItem = null;
                currentTrack = null;
            }
        }

        private VisualElement root;

        public override VisualElement CreateInspectorGUI()
        {
            Instance = this;
            root = new();
            Show();
            return root;
        }

        private void Show()
        {
            Clean();
            if (currentTrackItem == null)
                return;

            trackItemFrameIndex = currentTrackItem.FrameIndex;
            // TODO: 补充其他类型
            Type itemType = currentTrackItem.GetType();
            if (itemType == typeof(AnimationTrackItem))
            {
                DrawAnimationTrackItem((AnimationTrackItem)currentTrackItem);
            }
            else if (itemType == typeof(AudioTrackItem))
            {
                DrawAudioTrackItem((AudioTrackItem)currentTrackItem);
            }
            else if (itemType == typeof(EffectTrackItem))
            {
                DrawEffectTrackItem((EffectTrackItem)currentTrackItem);
            }
            else if (itemType == typeof(AttackDetectionTrackItem))
            {
                DrawAttackDetectionTrackItem((AttackDetectionTrackItem)currentTrackItem);
            }
            else if (itemType == typeof(EventTrackItem))
            {
                DrawEventTrackItem((EventTrackItem)currentTrackItem);
            }
        }

        private void Clean()
        {
            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    root.RemoveAt(i);
            }
        }


        private int trackItemFrameIndex;
        public void SetTrackItemFrameIndex(int index)
        {
            trackItemFrameIndex = index;
        }

        #region 动画轨道

        private IntegerField durationField;
        private FloatField transitionField;
        private Toggle rootMotionToggle;
        private Label clipFrameCountLabel;
        private Label isLoopLabel;
    
        private void DrawAnimationTrackItem(AnimationTrackItem trackItem)
        {
            // 动画资源
            ObjectField animationClipAssetField = new("动画资源");
            animationClipAssetField.objectType = typeof(AnimationClip);
            animationClipAssetField.value = trackItem.AnimationEvent.AnimationClip;
            animationClipAssetField.RegisterValueChangedCallback(OnAnimationClipAssetFieldValueChanged);
            root.Add(animationClipAssetField);
            // 根运动
            rootMotionToggle = new("应用根运动");
            rootMotionToggle.value = trackItem.AnimationEvent.ApplyRootMotion;
            rootMotionToggle.RegisterValueChangedCallback(OnRootMotionToggleValueChanged);
            root.Add(rootMotionToggle);
            // 轨道长度
            durationField = new("轨道长度");
            durationField.value = trackItem.AnimationEvent.DurationFrame;
            durationField.RegisterCallback<FocusInEvent>(OnDurationFieldFocusIn);
            durationField.RegisterCallback<FocusOutEvent>(OnDurationFieldFocusOut);
            root.Add(durationField);
            // 过渡时间
            transitionField = new("过渡时间");
            transitionField.value = trackItem.AnimationEvent.TransitionTime;
            transitionField.RegisterCallback<FocusInEvent>(OnTransitionFieldFocusIn);
            transitionField.RegisterCallback<FocusOutEvent>(OnTransitionFieldFocusOut);
            root.Add(transitionField);
            // 动画相关信息
            int clipFrameCount = (int)(trackItem.AnimationEvent.AnimationClip.length * trackItem.AnimationEvent.AnimationClip.frameRate);
            clipFrameCountLabel = new($"动画资源长度: {clipFrameCount}");
            root.Add(clipFrameCountLabel);
            isLoopLabel = new($"循环动画: {trackItem.AnimationEvent.AnimationClip.isLooping}");
            root.Add(isLoopLabel);
            // 删除
            Button deleteBtn = new(OnDeleteAnimationBtnClicked);
            deleteBtn.text = "删除";
            deleteBtn.style.backgroundColor = new Color(1, 0, 0, 0.5f);
            root.Add(deleteBtn);
            // 设置持续帧数至选中帧
            Button setFrameBtn = new(OnSetFrameBtnClicked);
            setFrameBtn.text = "设置持续帧数至选中帧";
            root.Add(setFrameBtn);
        }

        private void OnAnimationClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            AnimationClip clip = (AnimationClip)evt.newValue;
            clipFrameCountLabel.text = $"动画资源长度: {(int)(clip.length * clip.frameRate)}";
            isLoopLabel.text = $"循环动画: {clip.isLooping}";
            ((AnimationTrackItem)currentTrackItem).AnimationEvent.AnimationClip = clip;
            SkillEditorWindow.Instance.SaveSkillConfig();
            currentTrackItem.ResetView();
        }

        private void OnRootMotionToggleValueChanged(ChangeEvent<bool> evt)
        {
            ((AnimationTrackItem)currentTrackItem).AnimationEvent.ApplyRootMotion = evt.newValue;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }
        
        #region DurationField事件
        private int oldDurationValue;
        private void OnDurationFieldFocusIn(FocusInEvent evt)
        {
            oldDurationValue = durationField.value;
        }

        private void OnDurationFieldFocusOut(FocusOutEvent evt)
        {
            if (durationField.value != oldDurationValue)
            {
                // 安全校验
                if (((AnimationTrack)currentTrack).CheckFrameIndexOnDrag(trackItemFrameIndex + durationField.value, trackItemFrameIndex, false))
                {
                    // 修改数据，刷新视图
                    ((AnimationTrackItem)currentTrackItem).AnimationEvent.DurationFrame = durationField.value;
                    ((AnimationTrackItem)currentTrackItem).CheckFrameCount(); // 先刷新再保存，否则会刷新不了
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    currentTrackItem.ResetView();
                }
                else
                {
                    durationField.value = oldDurationValue;
                }
            }
        }
        
        private void OnSetFrameBtnClicked()
        {
            OnDurationFieldFocusIn(null);
            var newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - ((AnimationTrackItem)currentTrackItem).FrameIndex;
            if (newValue > 0) durationField.value = newValue;
            OnDurationFieldFocusOut(null);
        }
        #endregion

        #region TransitionField事件
        private float oldTransitionValue;
        private void OnTransitionFieldFocusIn(FocusInEvent evt)
        {
            oldTransitionValue = transitionField.value;
        }

        private void OnTransitionFieldFocusOut(FocusOutEvent evt)
        {
            if (!Mathf.Approximately(transitionField.value, oldTransitionValue))
            {
                ((AnimationTrackItem)currentTrackItem).AnimationEvent.TransitionTime = transitionField.value;
            }
        }
        #endregion
        
        private void OnDeleteAnimationBtnClicked()
        {
            currentTrack.DeleteTrackItem(trackItemFrameIndex);
            Selection.activeObject = null;
        }

        #endregion
        
        #region 音效轨道

        private FloatField volumeField;
        private void DrawAudioTrackItem(AudioTrackItem trackItem)
        {
            // 音效资源
            ObjectField audioClipAssetField = new("音效资源");
            audioClipAssetField.objectType = typeof(AudioClip);
            audioClipAssetField.value = trackItem.AudioEvent.AudioClip;
            audioClipAssetField.RegisterValueChangedCallback(OnAudioClipAssetFieldValueChanged);
            root.Add(audioClipAssetField);
            
            // 音量
            volumeField = new("播放音量");
            volumeField.value = trackItem.AudioEvent.Volume;
            volumeField.RegisterCallback<FocusInEvent>(OnVolumeFieldFocusIn);
            volumeField.RegisterCallback<FocusOutEvent>(OnVolumeFieldFocusOut);
            root.Add(volumeField);
        }
        
        private void OnAudioClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            var clip = evt.newValue as AudioClip;
            ((AudioTrackItem)currentTrackItem).AudioEvent.AudioClip = clip;
            currentTrackItem.ResetView();
        }
        
        #region VolumeField
        private float oldVolumeValue;
        private void OnVolumeFieldFocusIn(FocusInEvent evt)
        {
            oldVolumeValue = volumeField.value;
        }

        private void OnVolumeFieldFocusOut(FocusOutEvent evt)
        {
            if (!Mathf.Approximately(volumeField.value, oldVolumeValue))
            {
                ((AudioTrackItem)currentTrackItem).AudioEvent.Volume = volumeField.value;
            }
        }
        #endregion

        #endregion
        
        #region 特效轨道

        private IntegerField effectDurationField;
        
        private void DrawEffectTrackItem(EffectTrackItem trackItem)
        {
            // 预制体
            ObjectField prefabAssetField = new ObjectField("特效预制体");
            prefabAssetField.objectType = typeof(GameObject);
            prefabAssetField.value = trackItem.EffectEvent.Prefab;
            prefabAssetField.RegisterValueChangedCallback(OnEffectPrefabAssetFieldValueChanged);
            root.Add(prefabAssetField);
            // 坐标
            Vector3Field posField = new("位置");
            posField.value = trackItem.EffectEvent.Position;
            posField.RegisterValueChangedCallback(OnEffectPosFieldValueChanged);
            root.Add(posField);
            // 旋转
            Vector3Field rotField = new("旋转");
            rotField.value = trackItem.EffectEvent.Rotation;
            rotField.RegisterValueChangedCallback(OnEffectRotFieldValueChanged);
            root.Add(rotField);
            // 缩放
            Vector3Field scaleField = new("缩放");
            scaleField.value = trackItem.EffectEvent.Scale;
            scaleField.RegisterValueChangedCallback(OnEffectScaleFieldValueChanged);
            root.Add(scaleField);
            // 自动销毁
            Toggle autoDestroyToggle = new("自动销毁");
            autoDestroyToggle.value = trackItem.EffectEvent.AutoDestroy;
            autoDestroyToggle.RegisterValueChangedCallback(OnEffectAutoDestroyToggleValueChanged);
            root.Add(autoDestroyToggle);
            // 持续时间
            effectDurationField = new("持续时间");
            effectDurationField.value = trackItem.EffectEvent.Duration;
            effectDurationField.RegisterCallback<FocusInEvent>(OnEffectDurationFieldFocusIn);
            effectDurationField.RegisterCallback<FocusOutEvent>(OnEffectDurationFieldFocusOut);
            root.Add(effectDurationField);
            // 时间计算按钮
            Button calcDurationBtn = new(CalcEffectDuration);
            calcDurationBtn.text = "重新计时";
            root.Add(calcDurationBtn);
            // 应用模型Transform属性
            Button applyModelTransformBtn = new(ApplyModelTransform);
            applyModelTransformBtn.text = "应用模型Transform属性";
            root.Add(applyModelTransformBtn);
            // 设置持续帧数至选中帧
            Button setFrameBtn = new(OnSetEffectDurationFrameBtnClicked);
            setFrameBtn.text = "设置持续帧数至选中帧";
            root.Add(setFrameBtn);
        }

        private void ApplyModelTransform()
        {
            ((EffectTrackItem)currentTrackItem).ApplyModelTransform();
            Show();
        }

        private void CalcEffectDuration()
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            var particleSystems = effectTrackItem.EffectEvent.Prefab.GetComponentsInChildren<ParticleSystem>();
            float maxDuration = -1;
            foreach (var particleSystem in particleSystems)
            {
                if (particleSystem.main.duration > maxDuration)
                    maxDuration = particleSystem.main.duration;
            }
            effectTrackItem.EffectEvent.Duration = (int)(maxDuration * SkillEditorWindow.Instance.SkillClip.FrameRate);
            effectDurationField.value = effectTrackItem.EffectEvent.Duration;
            // TODO：删掉下面这一行
            effectTrackItem.ResetView();
        }

        #region Field值改变事件
        private void OnEffectPrefabAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.EffectEvent.Prefab = evt.newValue as GameObject;
            // 重新计时
            CalcEffectDuration();
            effectTrackItem.ResetView();
        }
        
        private void OnEffectPosFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.EffectEvent.Position = evt.newValue;
            effectTrackItem.ResetView();
        }
        
        private void OnEffectRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.EffectEvent.Rotation = evt.newValue;
            effectTrackItem.ResetView();
        }

        private void OnEffectScaleFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.EffectEvent.Scale = evt.newValue;
            effectTrackItem.ResetView();
        }
        
        private void OnEffectAutoDestroyToggleValueChanged(ChangeEvent<bool> evt)
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.EffectEvent.AutoDestroy = evt.newValue;
        }
        
        #region EffectDurationField
        private float oldEffectDurationValue;
        private void OnEffectDurationFieldFocusIn(FocusInEvent evt)
        {
            oldEffectDurationValue = effectDurationField.value;
        }

        private void OnEffectDurationFieldFocusOut(FocusOutEvent evt)
        {
            if (!Mathf.Approximately(effectDurationField.value, oldEffectDurationValue))
            {
                EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
                effectTrackItem.EffectEvent.Duration = effectDurationField.value;
                effectTrackItem.ResetView();
            }
        }

        private void OnSetEffectDurationFrameBtnClicked()
        {
            OnEffectDurationFieldFocusIn(null);
            int newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - ((EffectTrackItem)currentTrackItem).FrameIndex;
            if (newValue > 0) effectDurationField.value = newValue;
            OnEffectDurationFieldFocusOut(null);
        }
        #endregion
        
        #endregion
        
        #endregion
        
        #region 伤害检测轨道

        private IntegerField detectionDurationFrameField;
        private List<string> detectionTypeChoiceList;
        private void DrawAttackDetectionTrackItem(AttackDetectionTrackItem trackItem)
        {
            // 持续帧数
            detectionDurationFrameField = new IntegerField("持续帧数");
            detectionDurationFrameField.value = trackItem.AttackDetectionEvent.DurationFrame;
            detectionDurationFrameField.RegisterValueChangedCallback(OnDurationFrameFieldValueChanged);
            root.Add(detectionDurationFrameField);
            
            // 检测类型下拉列表
            detectionTypeChoiceList = new(Enum.GetNames(typeof(AttackDetectionType)));
            DropdownField detectionDropdownField = new("检测类型", detectionTypeChoiceList, (int)trackItem.AttackDetectionEvent.AttackDetectionType);
            detectionDropdownField.RegisterValueChangedCallback(OnDetectionDropdownFieldValueChanged);
            root.Add(detectionDropdownField);
            
            // 根据检测类型进行实际的绘制
            switch (trackItem.AttackDetectionEvent.AttackDetectionType)
            {
                case AttackDetectionType.Weapon:
                    var weaponDetectionData = (WeaponDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
                    DropdownField weaponDetectionDropdownField = new("武器选择");
                    if (SkillEditorWindow.Instance.CurrentPreviewCharacterObj != null)
                    {
                        var skillPlayer = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.GetComponent<SkillPlayer>();
                        weaponDetectionDropdownField.choices = skillPlayer.WeaponDict.Keys.ToList();
                    }
                    if (!string.IsNullOrEmpty(weaponDetectionData.WeaponName))
                    {
                        weaponDetectionDropdownField.value = weaponDetectionData.WeaponName;
                    }
                    weaponDetectionDropdownField.RegisterValueChangedCallback(OnWeaponDetectionDropdownFieldValueChanged);
                    root.Add(weaponDetectionDropdownField);
                    break;
                case AttackDetectionType.Box:
                    var boxDetectionData = (BoxDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
                    Vector3Field boxDetectionPosField = new("位置");
                    Vector3Field boxDetectionRotField = new("旋转");
                    Vector3Field boxDetectionScaleField = new("缩放");
                    boxDetectionPosField.value = boxDetectionData.Position;
                    boxDetectionRotField.value = boxDetectionData.Rotation;
                    boxDetectionScaleField.value = boxDetectionData.Scale;
                    boxDetectionPosField.RegisterValueChangedCallback(OnShapeDetectionPosFieldValueChanged);
                    boxDetectionRotField.RegisterValueChangedCallback(OnBoxDetectionRotFieldValueChanged);
                    boxDetectionScaleField.RegisterValueChangedCallback(OnBoxDetectionScaleFieldValueChanged);
                    root.Add(boxDetectionPosField);
                    root.Add(boxDetectionRotField);
                    root.Add(boxDetectionScaleField);
                    break;
                case AttackDetectionType.Sphere:
                    var sphereDetectionData = (SphereDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
                    Vector3Field sphereDetectionPosField = new("位置");
                    FloatField sphereDetectionRadiusField = new("半径");
                    sphereDetectionPosField.value = sphereDetectionData.Position;
                    sphereDetectionRadiusField.value = sphereDetectionData.Radius;
                    sphereDetectionPosField.RegisterValueChangedCallback(OnShapeDetectionPosFieldValueChanged);
                    sphereDetectionRadiusField.RegisterValueChangedCallback(OnSphereDetectionRadiusFieldValueChanged);
                    root.Add(sphereDetectionPosField);
                    root.Add(sphereDetectionRadiusField);
                    break;
                case AttackDetectionType.Fan:
                    var fanDetectionData = (FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
                    Vector3Field fanDetectionPosField = new("位置");
                    Vector3Field fanDetectionRotField = new("旋转");
                    FloatField fanDetectionInsideRadiusField = new("内半径");
                    FloatField fanDetectionRadiusField = new("外半径");
                    FloatField fanDetectionHeightField = new("高度");
                    FloatField fanDetectionAngleField = new("角度");
                    fanDetectionPosField.value = fanDetectionData.Position;
                    fanDetectionRotField.value = fanDetectionData.Rotation;
                    fanDetectionInsideRadiusField.value = fanDetectionData.InsideRadius;
                    fanDetectionRadiusField.value = fanDetectionData.Radius;
                    fanDetectionHeightField.value = fanDetectionData.Height;
                    fanDetectionAngleField.value = fanDetectionData.Angle;
                    fanDetectionPosField.RegisterValueChangedCallback(OnShapeDetectionPosFieldValueChanged);
                    fanDetectionRotField.RegisterValueChangedCallback(OnFanDetectionRotFieldValueChanged);
                    fanDetectionInsideRadiusField.RegisterValueChangedCallback(OnFanDetectionInsideRadiusFieldValueChanged);
                    fanDetectionRadiusField.RegisterValueChangedCallback(OnFanDetectionRadiusFieldValueChanged);
                    fanDetectionHeightField.RegisterValueChangedCallback(OnFanDetectionHeightFieldValueChanged);
                    fanDetectionAngleField.RegisterValueChangedCallback(OnFanDetectionAngleFieldValueChanged);
                    root.Add(fanDetectionPosField);
                    root.Add(fanDetectionRotField);
                    root.Add(fanDetectionInsideRadiusField);
                    root.Add(fanDetectionRadiusField);
                    root.Add(fanDetectionHeightField);
                    root.Add(fanDetectionAngleField);
                    break;
            }
            
            // 设置持续帧数至选中帧
            Button setFrameBtn = new(OnSetDetectionDurationFrameBtnClicked);
            setFrameBtn.text = "设置持续帧数至选中帧";
            root.Add(setFrameBtn);
        }

        #region Common Event
        private void OnDetectionDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            AttackDetectionTrackItem attackDetectionTrackItem = (AttackDetectionTrackItem)currentTrackItem;
            attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionType = (AttackDetectionType)detectionTypeChoiceList.IndexOf(evt.newValue);
            Show();
        }
        
        private void OnDurationFrameFieldValueChanged(ChangeEvent<int> evt)
        {
            ((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.DurationFrame = evt.newValue;
            currentTrackItem.ResetView();
        }

        private void OnSetDetectionDurationFrameBtnClicked()
        {
            var newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - ((AttackDetectionTrackItem)currentTrackItem).FrameIndex;
            if (newValue > 0)
            {
                detectionDurationFrameField.value = newValue;
            }
        }
        
        #endregion
        
        #region Weapon Events

        private void OnWeaponDetectionDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            WeaponDetectionData weaponDetectionData = (WeaponDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            weaponDetectionData.WeaponName = evt.newValue;
        }
        
        #endregion
        
        #region Shape Events
        
        private void OnShapeDetectionPosFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            ShapeDetectionDataBase shapeDetectionData = (ShapeDetectionDataBase)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            shapeDetectionData.Position = evt.newValue;
        }
        
        #endregion

        #region Box Events

        private void OnBoxDetectionRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            BoxDetectionData boxDetectionData = (BoxDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            boxDetectionData.Rotation = evt.newValue;
        }

        private void OnBoxDetectionScaleFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            BoxDetectionData boxDetectionData = (BoxDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            boxDetectionData.Scale = evt.newValue;
        }

        #endregion
        
        #region Sphere Events
        
        private void OnSphereDetectionRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            SphereDetectionData sphereDetectionData = (SphereDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            sphereDetectionData.Radius = evt.newValue;
        }
        
        #endregion

        #region Fan Events
        
        private void OnFanDetectionRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Rotation = evt.newValue;
        }
        
        private void OnFanDetectionInsideRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.InsideRadius = evt.newValue;
            if (fanDetectionData.Radius <= fanDetectionData.InsideRadius)
            {
                fanDetectionData.InsideRadius =  fanDetectionData.Radius - 0.01f;
                Show();
            }
        }

        private void OnFanDetectionRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Radius = evt.newValue;
            if (fanDetectionData.Radius <= fanDetectionData.InsideRadius)
            {
                fanDetectionData.InsideRadius =  fanDetectionData.Radius - 0.01f;
                Show();
            }
        }

        private void OnFanDetectionHeightFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Height = evt.newValue;
            if (fanDetectionData.Height <= 0)
            {
                fanDetectionData.Height = 0.01f;
                Show();
            }
        }

        private void OnFanDetectionAngleFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Angle = evt.newValue;
            if (fanDetectionData.Angle < 0)
            {
                fanDetectionData.Angle = 0.1f;
                Show();
            }
            else if (fanDetectionData.Angle > 360)
            {
                fanDetectionData.Angle = 360f;
                Show();
            }
        }
        
        #endregion
        
        #endregion
        
        #region 事件轨道

        private List<string> eventTypeChoiceList = new();
        private void DrawEventTrackItem(EventTrackItem trackItem)
        {
            // 事件类型下拉列表
            eventTypeChoiceList = new(Enum.GetNames(typeof(SkillEventType)));
            DropdownField eventTypeDropdownField = new DropdownField("事件类型", eventTypeChoiceList, (int)trackItem.CustomEvent.EventType);
            eventTypeDropdownField.RegisterValueChangedCallback(OnEventTypeDropdownFieldValueChanged);
            root.Add(eventTypeDropdownField);
            // 只有自定义事件才会显示名称
            if (trackItem.CustomEvent.EventType == SkillEventType.Custom)
            {
                // 名称
                TextField nameField = new("事件名称");
                nameField.value = trackItem.CustomEvent.CustomEventName;
                nameField.RegisterValueChangedCallback(OnNameFieldValueChanged);
                root.Add(nameField);
            }

            // 参数1
            IntegerField intArgField = new("int参数");
            intArgField.value = trackItem.CustomEvent.IntArg;
            intArgField.RegisterValueChangedCallback(OnIntArgFieldValueChanged);
            root.Add(intArgField);
            // 参数2
            FloatField floatArgField = new("float参数");
            floatArgField.value = trackItem.CustomEvent.FloatArg;
            floatArgField.RegisterValueChangedCallback(OnFloatArgFieldValueChanged);
            root.Add(floatArgField);
            // 参数3
            TextField stringArgField = new("string参数");
            stringArgField.value = trackItem.CustomEvent.StringArg;
            stringArgField.RegisterValueChangedCallback(OnStringArgFieldValueChanged);
            root.Add(stringArgField);
            // 参数4
            ObjectField objectArgField = new("object参数")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false,
            };
            objectArgField.value = trackItem.CustomEvent.ObjectArg;
            objectArgField.RegisterValueChangedCallback(OnObjectArgFieldValueChanged);
            root.Add(objectArgField);
            
            // 删除
            Button deleteBtn = new Button(OnDeleteEventBtnClicked);
            deleteBtn.text = "删除";
            deleteBtn.style.backgroundColor = new Color(1, 0, 0, 0.5f);
            root.Add(deleteBtn);
        }

        private void OnEventTypeDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            EventTrackItem eventTrackItem = (EventTrackItem)currentTrackItem;
            eventTrackItem.CustomEvent.EventType = (SkillEventType)eventTypeChoiceList.IndexOf(evt.newValue);
            if (eventTrackItem.CustomEvent.EventType != SkillEventType.Custom)
            {
                eventTrackItem.CustomEvent.CustomEventName = "";
            }
            Show();
        }

        private void OnNameFieldValueChanged(ChangeEvent<string> evt)
        {
            ((EventTrackItem)currentTrackItem).CustomEvent.CustomEventName = evt.newValue;
        }

        private void OnIntArgFieldValueChanged(ChangeEvent<int> evt)
        {
            ((EventTrackItem)currentTrackItem).CustomEvent.IntArg = evt.newValue;
        }
        
        private void OnFloatArgFieldValueChanged(ChangeEvent<float> evt)
        {
            ((EventTrackItem)currentTrackItem).CustomEvent.FloatArg = evt.newValue;
        }

        private void OnStringArgFieldValueChanged(ChangeEvent<string> evt)
        {
            ((EventTrackItem)currentTrackItem).CustomEvent.StringArg = evt.newValue;
        }

        private void OnObjectArgFieldValueChanged(ChangeEvent<Object> evt)
        {
            ((EventTrackItem)currentTrackItem).CustomEvent.ObjectArg = evt.newValue;
        }

        private void OnDeleteEventBtnClicked()
        {
            currentTrack.DeleteTrackItem(trackItemFrameIndex);
            Selection.activeObject = null;
        }

        #endregion
    }
}