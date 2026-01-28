using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillEffectEventInspector : SkillEventDataInspectorBase<EffectTrackItem, EffectTrack>
    {
        private IntegerField effectDurationField;

        protected override void OnDraw()
        {
            // 预制体
            ObjectField prefabAssetField = new ObjectField("特效预制体")
            {
                objectType = typeof(GameObject),
                value = trackItem.EffectEvent.Prefab
            };
            prefabAssetField.RegisterValueChangedCallback(OnEffectPrefabAssetFieldValueChanged);
            root.Add(prefabAssetField);
            // 坐标
            Vector3Field posField = new("位置")
            {
                value = trackItem.EffectEvent.Position
            };
            posField.RegisterValueChangedCallback(OnEffectPosFieldValueChanged);
            root.Add(posField);
            // 旋转
            Vector3Field rotField = new("旋转")
            {
                value = trackItem.EffectEvent.Rotation
            };
            rotField.RegisterValueChangedCallback(OnEffectRotFieldValueChanged);
            root.Add(rotField);
            // 缩放
            Vector3Field scaleField = new("缩放")
            {
                value = trackItem.EffectEvent.Scale
            };
            scaleField.RegisterValueChangedCallback(OnEffectScaleFieldValueChanged);
            root.Add(scaleField);
            // 自动销毁
            Toggle autoDestroyToggle = new("自动销毁")
            {
                value = trackItem.EffectEvent.AutoDestroy
            };
            autoDestroyToggle.RegisterValueChangedCallback(OnEffectAutoDestroyToggleValueChanged);
            root.Add(autoDestroyToggle);
            // 持续时间
            effectDurationField = new IntegerField("持续时间")
            {
                value = trackItem.EffectEvent.Duration
            };
            effectDurationField.RegisterCallback<FocusInEvent>(OnEffectDurationFieldFocusIn);
            effectDurationField.RegisterCallback<FocusOutEvent>(OnEffectDurationFieldFocusOut);
            root.Add(effectDurationField);
            // 时间计算按钮
            Button calcDurationBtn = new(CalcEffectDuration)
            {
                text = "重新计时"
            };
            root.Add(calcDurationBtn);
            // 应用模型Transform属性
            Button applyModelTransformBtn = new(ApplyModelTransform)
            {
                text = "应用模型Transform属性"
            };
            root.Add(applyModelTransformBtn);
            // 设置持续帧数至选中帧
            Button setFrameBtn = new(OnSetEffectDurationFrameBtnClicked)
            {
                text = "设置持续帧数至选中帧"
            };
            root.Add(setFrameBtn);
        }
        
        private void ApplyModelTransform()
        {
            trackItem.ApplyModelTransform();
            SkillEditorInspector.Instance.Show();
        }

        private void CalcEffectDuration()
        {
            var particleSystems = trackItem.EffectEvent.Prefab.GetComponentsInChildren<ParticleSystem>();
            float maxDuration = -1;
            foreach (var particleSystem in particleSystems)
            {
                if (particleSystem.main.duration > maxDuration)
                    maxDuration = particleSystem.main.duration;
            }
            trackItem.EffectEvent.Duration = (int)(maxDuration * SkillEditorWindow.Instance.SkillClip.FrameRate);
            effectDurationField.value = trackItem.EffectEvent.Duration;
            // TODO：删掉下面这一行
            trackItem.ResetView();
        }

        #region Field值改变事件
        private void OnEffectPrefabAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            trackItem.EffectEvent.Prefab = (GameObject)evt.newValue;
            // 重新计时
            CalcEffectDuration();
            trackItem.ResetView();
            SkillEditorWindow.Instance.TickSkill();
        }
        
        private void OnEffectPosFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            trackItem.EffectEvent.Position = evt.newValue;
            trackItem.ResetView();
            SkillEditorWindow.Instance.TickSkill();
        }
        
        private void OnEffectRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            trackItem.EffectEvent.Rotation = evt.newValue;
            trackItem.ResetView();
            SkillEditorWindow.Instance.TickSkill();
        }

        private void OnEffectScaleFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            trackItem.EffectEvent.Scale = evt.newValue;
            trackItem.ResetView();
            SkillEditorWindow.Instance.TickSkill();
        }
        
        private void OnEffectAutoDestroyToggleValueChanged(ChangeEvent<bool> evt)
        {
            trackItem.EffectEvent.AutoDestroy = evt.newValue;
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
                trackItem.EffectEvent.Duration = effectDurationField.value;
                trackItem.ResetView();
                SkillEditorWindow.Instance.TickSkill();
            }
        }

        private void OnSetEffectDurationFrameBtnClicked()
        {
            OnEffectDurationFieldFocusIn(null);
            int newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - trackItem.FrameIndex;
            if (newValue > 0) effectDurationField.value = newValue;
            OnEffectDurationFieldFocusOut(null);
        }
        #endregion
        
        #endregion
    }
}