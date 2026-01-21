using Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AudioTrackItem : TrackItemBase<AudioTrack>
    {
        private SkillMultiLineTrackStyle.ChildTrack childTrack;
        private SkillAudioTrackItemStyle audioItemStyle;
        private SkillAudioEvent audioEvent;
        public SkillAudioEvent AudioEvent => audioEvent;
        public void Init(AudioTrack track, float frameUnitWidth, SkillAudioEvent audioEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            this.track = track;
            this.frameIndex = audioEvent.FrameIndex;
            this.childTrack = childTrack;
            this.audioEvent = audioEvent;
            normalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
            selectColor = new Color(0.388f, 0.850f, 0.905f, 1f);
            audioItemStyle = new SkillAudioTrackItemStyle();
            ItemStyle = audioItemStyle;
            
            childTrack.trackRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            childTrack.trackRoot.RegisterCallback<DragExitedEvent>(OnDragExited);
            ResetView(frameUnitWidth);
        }

        public override void ResetView(float frameUnitWidth)
        {
            base.ResetView(frameUnitWidth);
            if (audioEvent.AudioClip != null)
            {
                if (audioItemStyle.IsInit == false)
                {
                    audioItemStyle.Init(frameUnitWidth, audioEvent, childTrack);
                    BindEvents();
                }
            }
            audioItemStyle.ResetView(frameUnitWidth, audioEvent);
        }
        
        public void Destroy()
        {
            childTrack.Destroy();
        }

        public void SetTrackName(string trackName)
        {
            childTrack.SetTrackName(trackName);
        }

        #region 鼠标交互
        private bool mouseDrag = false;
        private float startDragPosX;
        private int startDragFrameIndex;

        private void BindEvents()
        {
            audioItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(OnMouseDown);
            audioItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            audioItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(OnMouseUp);
            audioItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(OnMouseOut);
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            startDragPosX = evt.mousePosition.x;
            startDragFrameIndex = frameIndex;
            mouseDrag = true;
            Select();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (mouseDrag)
            {
                float offsetPos = evt.mousePosition.x - startDragPosX;
                int offsetFrame = Mathf.RoundToInt(offsetPos / frameUnitWidth);
                int targetFrameIndex = startDragFrameIndex + offsetFrame;
                
                // 不考虑拖拽到负数的情况 和 没有偏移的情况
                if (targetFrameIndex < 0 || offsetFrame == 0) 
                    return;
                
                // 确定修改数据
                frameIndex = targetFrameIndex;
                audioEvent.FrameIndex = frameIndex;
                // 如果超出右侧边界，则拓展边界（音效感觉没必要自动拓展边界）
                // CheckFrameCount();
                // 刷新视图
                ResetView(frameUnitWidth);
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (mouseDrag)
            {
                ApplyDrag();
            }
            mouseDrag = false;
        }

        private void OnMouseOut(MouseOutEvent evt)
        {
            if (mouseDrag)
            {
                ApplyDrag();
            }
            mouseDrag = false;
        }

        private void ApplyDrag()
        {
            if (startDragFrameIndex == frameIndex)
                return;
            
            audioEvent.FrameIndex = frameIndex;
            SkillEditorInspector.Instance.SetTrackItemFrameIndex(frameIndex);
        }

        public void CheckFrameCount()
        {
            int frameCount = (int)(audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillConfig.FrameRate);
            // 如果超出右侧边界，则拓展边界
            if (frameIndex + frameCount > SkillEditorWindow.Instance.SkillConfig.FrameCount)
            {
                SkillEditorWindow.Instance.CurrentFrameCount = frameIndex + frameCount;
            }
        }
        
        #endregion
        
        #region 拖拽资源
        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            AudioClip clip = objs[0] as AudioClip;
            if (clip != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }
        
        private void OnDragExited(DragExitedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            AudioClip clip = objs[0] as AudioClip;
            if (clip != null)
            {
                int selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);
                if (selectFrameIndex >= 0)
                {
                    // 构建默认的音效数据
                    audioEvent.AudioClip = clip;
                    audioEvent.FrameIndex = selectFrameIndex;
                    audioEvent.Volume = 1;
                    this.frameIndex = selectFrameIndex;
                    ResetView();
                    SkillEditorWindow.Instance.SaveSkillConfig();
                }
            }
        }
        #endregion
    }
}