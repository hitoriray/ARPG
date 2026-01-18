using Config.Skill;
using SkillEditor.Editor.EditorWindow;
using SkillEditor.Editor.Track.Inspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track.AnimationTrack
{
    public class AnimationTrackItem : TrackItemBase<AnimationTrack>
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/AnimationTrack/AnimationTrackItem.uxml";
        private SkillAnimationEvent animationEvent;
        public SkillAnimationEvent AnimationEvent => animationEvent;
        private VisualElement mainDragArea;
        private VisualElement animationOverLine;

        public void Init(AnimationTrack animationTrack, VisualElement parent, int startFrameIndex, float frameUnitWidth,
            SkillAnimationEvent animationEvent)
        {
            track = animationTrack;
            this.frameIndex = startFrameIndex;
            this.frameUnitWidth = frameUnitWidth;
            this.animationEvent = animationEvent;

            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            mainDragArea = Root.Q<VisualElement>("Main");
            animationOverLine = Root.Q<VisualElement>("OverLine");
            parent.Add(Root);

            normalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
            selectColor = new Color(0.388f, 0.850f, 0.905f, 1f);
            OnUnselect();

            BindEvents();
            ResetView(frameUnitWidth);
        }

        public override void ResetView(float frameUnitWidth)
        {
            base.ResetView(frameUnitWidth);

            Root.text = animationEvent.AnimationClip.name;

            // 位置计算
            Vector3 mainPos = Root.transform.position;
            mainPos.x = frameIndex * frameUnitWidth;
            Root.transform.position = mainPos;
            Root.style.width = animationEvent.DurationFrame * frameUnitWidth;

            // 计算动画结束线的位置
            int animationClipFrameCount = (int)(animationEvent.AnimationClip.length * animationEvent.AnimationClip.frameRate);
            if (animationClipFrameCount > animationEvent.DurationFrame)
            {
                animationOverLine.style.display = DisplayStyle.None;
            }
            else
            {
                animationOverLine.style.display = DisplayStyle.Flex;
                Vector3 overLinePos = animationOverLine.transform.position;
                overLinePos.x = animationClipFrameCount * frameUnitWidth - 1;
                animationOverLine.transform.position = overLinePos;
            }
        }

        #region 鼠标拖拽事件
        private bool mouseDrag = false;
        private float startDragPosX;
        private int startDragFrameIndex;
        
        private void BindEvents()
        {
            mainDragArea.RegisterCallback<MouseDownEvent>(OnMouseDown);
            mainDragArea.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            mainDragArea.RegisterCallback<MouseUpEvent>(OnMouseUp);
            mainDragArea.RegisterCallback<MouseOutEvent>(OnMouseOut);
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
                bool checkDrag = false;
                if (targetFrameIndex < 0) // 不考虑拖拽到负数的情况
                    return;
                if (offsetFrame < 0)
                {
                    checkDrag = track.CheckFrameIndexOnDrag(targetFrameIndex, startDragFrameIndex, true);
                }
                else if (offsetFrame > 0)
                {
                    checkDrag = track.CheckFrameIndexOnDrag(targetFrameIndex + animationEvent.DurationFrame, startDragFrameIndex, false);
                }
                else
                {
                    return;
                }

                if (checkDrag)
                {
                    // 确定修改数据
                    frameIndex = targetFrameIndex;
                    // 如果超出右侧边界，则拓展边界
                    CheckFrameCount();
                    // 刷新视图
                    ResetView(frameUnitWidth);
                }
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
            
            track.SetFrameIndex(startDragFrameIndex, frameIndex);
            SkillEditorInspector.Instance.SetTrackItemFrameIndex(frameIndex);
        }

        public void CheckFrameCount()
        {
            // 如果超出右侧边界，则拓展边界
            if (frameIndex + animationEvent.DurationFrame > SkillEditorWindow.Instance.SkillConfig.FrameCount)
            {
                SkillEditorWindow.Instance.CurrentFrameCount = frameIndex + animationEvent.DurationFrame;
            }
        }

        #endregion

        public override void OnConfigChanged()
        {
            animationEvent = track.AnimationData.FrameEventDict[frameIndex];
        }
    }
}