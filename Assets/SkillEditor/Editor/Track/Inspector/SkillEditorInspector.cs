using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Player.Skill;
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
    
        private void DrawAnimationTrackItem(AnimationTrackItem animationTrackItem)
        {
            trackItemFrameIndex = animationTrackItem.FrameIndex;
            // 动画资源
            ObjectField animationClipAssetField = new ObjectField("动画资源");
            animationClipAssetField.objectType = typeof(AnimationClip);
            animationClipAssetField.value = animationTrackItem.AnimationEvent.AnimationClip;
            animationClipAssetField.RegisterValueChangedCallback(OnAnimationClipAssetFieldValueChanged);
            root.Add(animationClipAssetField);
            // 根运动
            rootMotionToggle = new Toggle("应用根运动");
            rootMotionToggle.value = animationTrackItem.AnimationEvent.ApplyRootMotion;
            rootMotionToggle.RegisterValueChangedCallback(OnRootMotionToggleValueChanged);
            root.Add(rootMotionToggle);
            // 轨道长度
            durationField = new IntegerField("轨道长度");
            durationField.value = animationTrackItem.AnimationEvent.DurationFrame;
            durationField.RegisterCallback<FocusInEvent>(OnDurationFieldFocusIn);
            durationField.RegisterCallback<FocusOutEvent>(OnDurationFieldFocusOut);
            root.Add(durationField);
            // 过渡时间
            transitionField = new FloatField("过渡时间");
            transitionField.value = animationTrackItem.AnimationEvent.TransitionTime;
            transitionField.RegisterCallback<FocusInEvent>(OnTransitionFieldFocusIn);
            transitionField.RegisterCallback<FocusOutEvent>(OnTransitionFieldFocusOut);
            root.Add(transitionField);
            // 动画相关信息
            int clipFrameCount = (int)(animationTrackItem.AnimationEvent.AnimationClip.length * animationTrackItem.AnimationEvent.AnimationClip.frameRate);
            clipFrameCountLabel = new Label($"动画资源长度: {clipFrameCount}");
            root.Add(clipFrameCountLabel);
            isLoopLabel = new Label($"循环动画: {animationTrackItem.AnimationEvent.AnimationClip.isLooping}");
            root.Add(isLoopLabel);

            Button deleteBtn = new Button(OnDeleteBtnClicked);
            deleteBtn.text = "删除";
            deleteBtn.style.backgroundColor = new Color(1, 0, 0, 0.5f);
            root.Add(deleteBtn);
        }

        private void OnAnimationClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            AnimationClip clip = evt.newValue as AnimationClip;
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
        private int oldDurationValue = 0;
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
                    SkillEditorWindow.Instance.SkillConfig.SkillAnimationData.FrameEventDict[trackItemFrameIndex].DurationFrame = durationField.value;
                    (currentTrackItem as AnimationTrackItem)?.CheckFrameCount(); // 先刷新再保存，否则会刷新不了
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    currentTrackItem.ResetView();
                }
                else
                {
                    durationField.value = oldDurationValue;
                }
            }
        }
        #endregion

        #region TransitionField事件
        private float oldTransitionValue = 0;
        private void OnTransitionFieldFocusIn(FocusInEvent evt)
        {
            oldTransitionValue = transitionField.value;
        }

        private void OnTransitionFieldFocusOut(FocusOutEvent evt)
        {
            if (transitionField.value != oldTransitionValue)
            {
                ((AnimationTrackItem)currentTrackItem).AnimationEvent.TransitionTime = transitionField.value;
            }
        }
        #endregion
        
        private void OnDeleteBtnClicked()
        {
            currentTrack.DeleteTrackItem(trackItemFrameIndex);
            Selection.activeObject = null;
        }

        #endregion
        
        #region 音效轨道

        private FloatField volumeField;
        private void DrawAudioTrackItem(AudioTrackItem audioTrackItem)
        {
            // 音效资源
            ObjectField audioClipAssetField = new ObjectField("音效资源");
            audioClipAssetField.objectType = typeof(AudioClip);
            audioClipAssetField.value = audioTrackItem.AudioEvent.AudioClip;
            audioClipAssetField.RegisterValueChangedCallback(OnAudioClipAssetFieldValueChanged);
            root.Add(audioClipAssetField);
            
            // 音量
            volumeField = new FloatField("播放音量");
            volumeField.value = audioTrackItem.AudioEvent.Volume;
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
        private float oldVolumeValue = 0;
        private void OnVolumeFieldFocusIn(FocusInEvent evt)
        {
            oldVolumeValue = volumeField.value;
        }

        private void OnVolumeFieldFocusOut(FocusOutEvent evt)
        {
            if (volumeField.value != oldVolumeValue)
            {
                ((AudioTrackItem)currentTrackItem).AudioEvent.Volume = volumeField.value;
            }
        }
        #endregion

        #endregion
        
        #region 特效轨道

        private FloatField effectDurationField;
        
        private void DrawEffectTrackItem(EffectTrackItem effectTrackItem)
        {
            // 预制体
            ObjectField prefabAssetField = new ObjectField("特效预制体");
            prefabAssetField.objectType = typeof(GameObject);
            prefabAssetField.value = effectTrackItem.EffectEvent.Prefab;
            prefabAssetField.RegisterValueChangedCallback(OnEffectPrefabAssetFieldValueChanged);
            root.Add(prefabAssetField);
            // 坐标
            Vector3Field posField = new Vector3Field("位置");
            posField.value = effectTrackItem.EffectEvent.Position;
            posField.RegisterValueChangedCallback(OnEffectPosFieldValueChanged);
            root.Add(posField);
            // 旋转
            Vector3Field rotField = new Vector3Field("旋转");
            rotField.value = effectTrackItem.EffectEvent.Rotation;
            rotField.RegisterValueChangedCallback(OnEffectRotFieldValueChanged);
            root.Add(rotField);
            // 缩放
            Vector3Field scaleField = new Vector3Field("缩放");
            scaleField.value = effectTrackItem.EffectEvent.Scale;
            scaleField.RegisterValueChangedCallback(OnEffectScaleFieldValueChanged);
            root.Add(scaleField);
            // 自动销毁
            Toggle autoDestroyToggle = new Toggle("自动销毁");
            autoDestroyToggle.value = effectTrackItem.EffectEvent.AutoDestroy;
            autoDestroyToggle.RegisterValueChangedCallback(OnEffectAutoDestroyToggleValueChanged);
            root.Add(autoDestroyToggle);
            // 持续时间
            effectDurationField = new FloatField("持续时间");
            effectDurationField.value = effectTrackItem.EffectEvent.Duration;
            effectDurationField.RegisterCallback<FocusInEvent>(OnEffectDurationFieldFocusIn);
            effectDurationField.RegisterCallback<FocusOutEvent>(OnEffectDurationFieldFocusOut);
            root.Add(effectDurationField);
            // 时间计算按钮
            Button calcDurationBtn = new Button(CalcEffectDuration);
            calcDurationBtn.text = "重新计时";
            root.Add(calcDurationBtn);
            // 应用模型Transform属性
            Button applyModelTransformBtn = new Button(ApplyModelTransform);
            applyModelTransformBtn.text = "应用模型Transform属性";
            root.Add(applyModelTransformBtn);
        }

        private void ApplyModelTransform()
        {
            EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
            effectTrackItem.ApplyModelTransform();
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
            effectTrackItem.EffectEvent.Duration = maxDuration;
            effectDurationField.value = maxDuration;
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
        private float oldEffectDurationValue = 0;
        private void OnEffectDurationFieldFocusIn(FocusInEvent evt)
        {
            oldEffectDurationValue = effectDurationField.value;
        }

        private void OnEffectDurationFieldFocusOut(FocusOutEvent evt)
        {
            if (effectDurationField.value != oldEffectDurationValue)
            {
                EffectTrackItem effectTrackItem = (EffectTrackItem)currentTrackItem;
                effectTrackItem.EffectEvent.Duration = effectDurationField.value;
                effectTrackItem.ResetView();
            }
        }
        #endregion
        
        #endregion
        
        #endregion
        
        #region 伤害检测轨道

        private IntegerField durationFrameField;
        private List<string> detectionTypeList;
        private void DrawAttackDetectionTrackItem(AttackDetectionTrackItem attackDetectionTrackItem)
        {
            // 持续帧数
            durationFrameField = new IntegerField("持续帧数");
            durationFrameField.value = attackDetectionTrackItem.AttackDetectionEvent.DurationFrame;
            durationFrameField.RegisterCallback<FocusInEvent>(OnDurationFrameFieldFocusIn);
            durationFrameField.RegisterCallback<FocusOutEvent>(OnDurationFrameFieldFocusOut);
            root.Add(durationFrameField);
            
            // 检测类型下拉列表
            detectionTypeList = new(Enum.GetNames(typeof(AttackDetectionType)));
            DropdownField detectionDropdownField = new DropdownField("检测类型", detectionTypeList, (int)attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionType);
            detectionDropdownField.RegisterValueChangedCallback(OnDetectionDropdownFieldValueChanged);
            root.Add(detectionDropdownField);
            
            // 根据检测类型进行实际的绘制
            switch (attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionType)
            {
                case AttackDetectionType.Weapon:
                    WeaponDetectionData weaponDetectionData = (WeaponDetectionData)attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionData;
                    DropdownField weaponDetectionDropdownField = new DropdownField("武器选择");
                    SkillPlayer skillPlayer = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.GetComponent<SkillPlayer>();
                    weaponDetectionDropdownField.choices = skillPlayer.WeaponDict.Keys.ToList();
                    if (!string.IsNullOrEmpty(weaponDetectionData.WeaponName) &&
                        skillPlayer.WeaponDict.ContainsKey(weaponDetectionData.WeaponName))
                    {
                        weaponDetectionDropdownField.value = weaponDetectionData.WeaponName;
                    }
                    weaponDetectionDropdownField.RegisterValueChangedCallback(OnWeaponDetectionDropdownFieldValueChanged);
                    root.Add(weaponDetectionDropdownField);
                    break;
                case AttackDetectionType.Box:
                    BoxDetectionData boxDetectionData = (BoxDetectionData)attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionData;
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
                    SphereDetectionData sphereDetectionData = (SphereDetectionData)attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionData;
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
                    FanDetectionData fanDetectionData = (FanDetectionData)attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionData;
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
        }


        #region Common Event
        private void OnDetectionDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            AttackDetectionTrackItem attackDetectionTrackItem = (AttackDetectionTrackItem)currentTrackItem;
            attackDetectionTrackItem.AttackDetectionEvent.AttackDetectionType = (AttackDetectionType)detectionTypeList.IndexOf(evt.newValue);
            Show();
        }
        
        #region DurationFrameField
        private float oldDurationFrameValue = 0;
        private void OnDurationFrameFieldFocusIn(FocusInEvent evt)
        {
            oldDurationFrameValue = durationFrameField.value;
        }

        private void OnDurationFrameFieldFocusOut(FocusOutEvent evt)
        {
            if (durationFrameField.value != oldDurationFrameValue)
            {
                ((AttackDetectionTrackItem)currentTrackItem).AttackDetectionEvent.DurationFrame = durationFrameField.value;
                currentTrackItem.ResetView();
            }
        }
        #endregion
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
    }
}