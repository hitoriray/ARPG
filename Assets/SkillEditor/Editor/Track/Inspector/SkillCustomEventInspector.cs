using System;
using System.Collections.Generic;
using Config;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillCustomEventInspector : SkillEventDataInspectorBase<EventTrackItem, EventTrack>
    {
        private List<string> eventTypeChoiceList = new();

        protected override void OnDraw()
        {
            // 事件类型下拉列表
            eventTypeChoiceList = new List<string>(Enum.GetNames(typeof(SkillEventType)));
            DropdownField eventTypeDropdownField = new DropdownField("事件类型", eventTypeChoiceList, (int)trackItem.CustomEvent.EventType);
            eventTypeDropdownField.RegisterValueChangedCallback(OnEventTypeDropdownFieldValueChanged);
            root.Add(eventTypeDropdownField);
            // 只有自定义事件才会显示名称
            if (trackItem.CustomEvent.EventType == SkillEventType.Custom)
            {
                // 名称
                TextField nameField = new("事件名称")
                {
                    value = trackItem.CustomEvent.CustomEventName
                };
                nameField.RegisterValueChangedCallback(OnNameFieldValueChanged);
                root.Add(nameField);
            }

            // 参数1
            IntegerField intArgField = new("int参数")
            {
                value = trackItem.CustomEvent.IntArg
            };
            intArgField.RegisterValueChangedCallback(OnIntArgFieldValueChanged);
            root.Add(intArgField);
            // 参数2
            FloatField floatArgField = new("float参数")
            {
                value = trackItem.CustomEvent.FloatArg
            };
            floatArgField.RegisterValueChangedCallback(OnFloatArgFieldValueChanged);
            root.Add(floatArgField);
            // 参数3
            TextField stringArgField = new("string参数")
            {
                value = trackItem.CustomEvent.StringArg
            };
            stringArgField.RegisterValueChangedCallback(OnStringArgFieldValueChanged);
            root.Add(stringArgField);
            // 参数4
            ObjectField objectArgField = new("object参数")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false,
                value = trackItem.CustomEvent.ObjectArg
            };
            objectArgField.RegisterValueChangedCallback(OnObjectArgFieldValueChanged);
            root.Add(objectArgField);
        }
        
        private void OnEventTypeDropdownFieldValueChanged(ChangeEvent<string> evt)
        {
            trackItem.CustomEvent.EventType = (SkillEventType)eventTypeChoiceList.IndexOf(evt.newValue);
            if (trackItem.CustomEvent.EventType != SkillEventType.Custom)
            {
                trackItem.CustomEvent.CustomEventName = "";
            }
            SkillEditorInspector.Instance.Show();
        }
        
        private void OnNameFieldValueChanged(ChangeEvent<string> evt)
        {
            trackItem.CustomEvent.CustomEventName = evt.newValue;
        }

        private void OnIntArgFieldValueChanged(ChangeEvent<int> evt)
        {
            trackItem.CustomEvent.IntArg = evt.newValue;
        }
        
        private void OnFloatArgFieldValueChanged(ChangeEvent<float> evt)
        {
            trackItem.CustomEvent.FloatArg = evt.newValue;
        }

        private void OnStringArgFieldValueChanged(ChangeEvent<string> evt)
        {
            trackItem.CustomEvent.StringArg = evt.newValue;
        }

        private void OnObjectArgFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            trackItem.CustomEvent.ObjectArg = evt.newValue;
        }
    }
}