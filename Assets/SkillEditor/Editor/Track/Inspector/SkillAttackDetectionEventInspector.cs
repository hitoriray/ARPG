using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Skill;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAttackDetectionEventInspector : SkillEventDataInspectorBase<AttackDetectionTrackItem, AttackDetectionTrack>
    {
        private IntegerField detectionDurationFrameField;
        private List<string> detectionTypeChoiceList;

        protected override void OnDraw()
        {
            // 持续帧数
            detectionDurationFrameField = new IntegerField("持续帧数")
            {
                value = trackItem.AttackDetectionEvent.DurationFrame
            };
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
            Button setFrameBtn = new(OnSetDetectionDurationFrameBtnClicked)
            {
                text = "设置持续帧数至选中帧"
            };
            root.Add(setFrameBtn);
        }
        
        #region Common Event
        private void OnDetectionDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            trackItem.AttackDetectionEvent.AttackDetectionType = (AttackDetectionType)detectionTypeChoiceList.IndexOf(evt.newValue);
            SkillEditorInspector.Instance.Show();
        }
        
        private void OnDurationFrameFieldValueChanged(ChangeEvent<int> evt)
        {
            trackItem.AttackDetectionEvent.DurationFrame = evt.newValue;
            trackItem.ResetView();
        }

        private void OnSetDetectionDurationFrameBtnClicked()
        {
            var newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - trackItem.FrameIndex;
            if (newValue > 0)
            {
                detectionDurationFrameField.value = newValue;
            }
        }
        
        #endregion
        
        #region Weapon Events

        private void OnWeaponDetectionDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            ((WeaponDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData).WeaponName = evt.newValue;
        }
        
        #endregion
        
        #region Shape Events
        
        private void OnShapeDetectionPosFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            ((ShapeDetectionDataBase)trackItem.AttackDetectionEvent.AttackDetectionData).Position = evt.newValue;
        }
        
        #endregion

        #region Box Events

        private void OnBoxDetectionRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            ((BoxDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData).Rotation = evt.newValue;
        }

        private void OnBoxDetectionScaleFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            ((BoxDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData).Scale = evt.newValue;
        }

        #endregion
        
        #region Sphere Events
        
        private void OnSphereDetectionRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            ((SphereDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData).Radius = evt.newValue;
        }
        
        #endregion

        #region Fan Events
        
        private void OnFanDetectionRotFieldValueChanged(ChangeEvent<Vector3> evt)
        {
            ((FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData).Rotation = evt.newValue;
        }
        
        private void OnFanDetectionInsideRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.InsideRadius = evt.newValue;
            if (fanDetectionData.Radius <= fanDetectionData.InsideRadius)
            {
                fanDetectionData.InsideRadius =  fanDetectionData.Radius - 0.01f;
                SkillEditorInspector.Instance.Show();
            }
        }

        private void OnFanDetectionRadiusFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Radius = evt.newValue;
            if (fanDetectionData.Radius <= fanDetectionData.InsideRadius)
            {
                fanDetectionData.InsideRadius =  fanDetectionData.Radius - 0.01f;
                SkillEditorInspector.Instance.Show();
            }
        }

        private void OnFanDetectionHeightFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Height = evt.newValue;
            if (fanDetectionData.Height <= 0)
            {
                fanDetectionData.Height = 0.01f;
                SkillEditorInspector.Instance.Show();
            }
        }

        private void OnFanDetectionAngleFieldValueChanged(ChangeEvent<float> evt)
        {
            FanDetectionData fanDetectionData = (FanDetectionData)trackItem.AttackDetectionEvent.AttackDetectionData;
            fanDetectionData.Angle = evt.newValue;
            if (fanDetectionData.Angle < 0)
            {
                fanDetectionData.Angle = 0.1f;
                SkillEditorInspector.Instance.Show();
            }
            else if (fanDetectionData.Angle > 360)
            {
                fanDetectionData.Angle = 360f;
                SkillEditorInspector.Instance.Show();
            }
        }
        
        #endregion
    }
}