using System.Collections.Generic;
using Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class EffectTrack : SkillTrackBase
    {
        private SkillMultiLineTrackStyle trackStyle;
        public SkillEffectData EffectData => SkillEditorWindow.Instance.SkillClip.SkillEffectData;
        private readonly List<EffectTrackItem> trackItemList = new();

        public static Transform EffectParent { get; private set; }
        
        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillMultiLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "特效配置", AddChildTrack, CheckDeleteChildTrack, SwapChildTrack, UpdateChildTrackName);
            
            // 只在SkillEditor场景生效
            if (SkillEditorWindow.Instance.IsInEditorScene)
            {
                // 通过GameObject管理特效的生命周期，防止特效因为重新编译而不销毁
                EffectParent = GameObject.Find("Effects").transform;
                EffectParent.position = Vector3.zero;
                EffectParent.rotation = Quaternion.identity;
                for (int i = EffectParent.childCount - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(EffectParent.GetChild(i).gameObject);
                }
            }

            ResetView();
        }

        public override void ResetView(float frameWidth)
        {
            base.ResetView(frameWidth);
            // 销毁当前已有的
            foreach (var item in trackItemList)
            {
                item.Destroy();
            }
            trackItemList.Clear();
            
            // 根据数据绘制TrackItem
            foreach (var audioEvent in EffectData.FrameData)
            {
                CreateEffectTrackItem(audioEvent);
            }
        }

        private void CreateEffectTrackItem(SkillEffectEvent effectEvent)
        {
            EffectTrackItem item = new();
            item.Init(this, frameWidth, effectEvent, trackStyle.AddChildTrack());
            item.SetTrackName(effectEvent.TrackName);
            trackItemList.Add(item);
        }

        /// <summary>
        /// 更新子轨道名称
        /// </summary>
        private void UpdateChildTrackName(SkillMultiLineTrackStyle.ChildTrack childTrack, string newName)
        {
            // 同步给配置
            EffectData.FrameData[childTrack.GetIndex()].TrackName = newName;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 新增子轨道
        /// </summary>
        private void AddChildTrack()
        {
            SkillEditorWindow.Instance.RecordUndoSnapshot();
            SkillEffectEvent effectEvent = new();
            EffectData.FrameData.Add(effectEvent);
            CreateEffectTrackItem(effectEvent);
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 检查子轨道能否删除
        /// </summary>
        /// <param name="index">子轨道的索引</param>
        /// <returns></returns>
        private bool CheckDeleteChildTrack(int index)
        {
            if (index < 0 || index >= EffectData.FrameData.Count)
                return false;
            if (EffectData.FrameData[index] == null)
                return false;
            SkillEditorWindow.Instance.RecordUndoSnapshot();
            EffectData.FrameData.RemoveAt(index);
            SkillEditorWindow.Instance.SaveSkillConfig();
            trackItemList[index].CleanupEffectPrefabObject();
            trackItemList.RemoveAt(index);
            return true;
        }

        private void SwapChildTrack(int index1, int index2)
        {
            var audioData1 = EffectData.FrameData[index1];
            var audioData2 = EffectData.FrameData[index2];
            EffectData.FrameData[index1] = audioData2;
            EffectData.FrameData[index2] = audioData1;
            // 保存交给窗口的退出机制
        }

        #region 重载方法
        
        public override void DeleteTrackItem(int frameIndex)
        {
            for (int i = 0; i < trackItemList.Count; i++)
            {
                if (trackItemList[i].EffectEvent.FrameIndex == frameIndex)
                {
                    SkillEditorWindow.Instance.RecordUndoSnapshot();
                    EffectData.FrameData.RemoveAt(i);
                    trackItemList[i].CleanupEffectPrefabObject();
                    trackItemList[i].Destroy();
                    trackItemList.RemoveAt(i);
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    return;
                }
            }
        }
        
        public override void Destroy()
        {
            trackStyle.Destroy();
            foreach (var item in trackItemList)
            {
                item.CleanupEffectPrefabObject();
            }
        }
        
        public override void TickView(int frameIndex)
        {
            foreach (var item in trackItemList)
            {
                item.TickView(frameIndex);
            }
        }

        public override void DrawGizmos()
        {
            int currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
            foreach (var item in trackItemList)
            {
                SkillEffectEvent effectEvent = item.EffectEvent;
                if (effectEvent == null)
                    continue;
                if (currentFrameIndex < effectEvent.FrameIndex ||
                    currentFrameIndex > effectEvent.FrameIndex + effectEvent.Duration)
                    continue;
                if (SkillEditorInspector.CurrentTrackItem != item)
                    continue;
                item.DrawGizmos();
            }
        }

        public override void OnSceneGUI()
        {
            int currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
            foreach (var item in trackItemList)
            {
                SkillEffectEvent effectEvent = item.EffectEvent;
                if (effectEvent == null)
                    continue;
                if (currentFrameIndex < effectEvent.FrameIndex ||
                    currentFrameIndex > effectEvent.FrameIndex + effectEvent.Duration)
                    continue;
                // 必须选中才绘制
                if (SkillEditorInspector.CurrentTrackItem != item)
                    continue;
                item.OnSceneGUI();
            }
        }

        #endregion
    }
}
