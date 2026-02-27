using Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class EffectTrackItem : TrackItemBase<EffectTrack>
    {
        private const float HandleSizeMultiplier = 2.5f;
        private const float GizmoSizeMultiplier = 0.25f;
        private SkillMultiLineTrackStyle.ChildTrack childTrack;
        private SkillEffectTrackItemStyle effectItemStyle;
        private SkillEffectEvent effectEvent;
        public SkillEffectEvent EffectEvent => effectEvent;
        public void Init(EffectTrack track, float frameUnitWidth, SkillEffectEvent effectEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            this.track = track;
            this.frameIndex = effectEvent.FrameIndex;
            this.childTrack = childTrack;
            this.effectEvent = effectEvent;
            normalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
            selectColor = new Color(0.388f, 0.850f, 0.905f, 1f);
            effectItemStyle = new SkillEffectTrackItemStyle();
            ItemStyle = effectItemStyle;
            
            childTrack.trackRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            childTrack.trackRoot.RegisterCallback<DragExitedEvent>(OnDragExited);
            ResetView(frameUnitWidth);
        }

        public override void ResetView(float frameUnitWidth)
        {
            base.ResetView(frameUnitWidth);
            if (effectEvent.Prefab != null)
            {
                if (effectItemStyle.IsInit == false)
                {
                    effectItemStyle.Init(frameUnitWidth, effectEvent, childTrack);
                    BindEvents();
                }
            }
            effectItemStyle.ResetView(frameUnitWidth, effectEvent);
            
            // 强制重新生成预览
            CleanupEffectPrefabObject();
            TickView(SkillEditorWindow.Instance.CurrentSelectFrameIndex);
        }
        
        public void Destroy()
        {
            CleanupEffectPrefabObject();
            childTrack.Destroy();
        }

        public void CleanupEffectPrefabObject()
        {
            if (effectPrefabObj != null)
            {
                GameObject.DestroyImmediate(effectPrefabObj);
                effectPrefabObj = null;
            }
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
            effectItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(OnMouseDown);
            effectItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            effectItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(OnMouseUp);
            effectItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(OnMouseOut);
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
                effectEvent.FrameIndex = frameIndex;
                // 如果超出右侧边界，则拓展边界
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
            
            effectEvent.FrameIndex = frameIndex;
            SkillEditorInspector.Instance.SetTrackItemFrameIndex(frameIndex);
        }
        
        #endregion
        
        #region 拖拽资源
        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            GameObject prefab = objs[0] as GameObject;
            if (prefab != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }
        
        private void OnDragExited(DragExitedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            GameObject prefab = objs[0] as GameObject;
            if (prefab != null)
            {
                int selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);
                if (selectFrameIndex >= 0)
                {
                    // 构建默认的特效数据
                    effectEvent.Prefab = prefab;
                    effectEvent.Position = Vector3.zero;
                    effectEvent.Rotation = Vector3.zero;
                    effectEvent.Scale = Vector3.one;
                    effectEvent.AutoDestroy = true;
                    var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>();
                    float maxDuration = -1;
                    foreach (var particleSystem in particleSystems)
                    {
                        if (particleSystem.main.duration > maxDuration)
                            maxDuration = particleSystem.main.duration;
                    }
                    effectEvent.Duration = (int)(maxDuration * SkillEditorWindow.Instance.SkillClip.FrameRate);
                    effectEvent.FrameIndex = selectFrameIndex;
                    
                    this.frameIndex = selectFrameIndex;
                    ResetView();
                    SkillEditorWindow.Instance.SaveSkillConfig();
                }
            }
        }
        #endregion
        
        #region 预览

        private GameObject effectPrefabObj;
        public void TickView(int frameIndex)
        {
            if (effectEvent.Prefab == null || SkillEditorWindow.Instance.CurrentPreviewCharacterObj == null)
                return;
            // 是否在播放范围内
            int durationFrame = effectEvent.Duration;
            if (effectEvent.FrameIndex <= frameIndex && frameIndex < effectEvent.FrameIndex + durationFrame)
            {
                GameObject.DestroyImmediate(effectPrefabObj);
                
                Transform characterRoot = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform;
                // 把角色临时设置到播放坐标
                Vector3 pos = characterRoot.TransformPoint(effectEvent.Position);
                Vector3 rot = characterRoot.eulerAngles + effectEvent.Rotation;
                // 实例化
                effectPrefabObj = GameObject.Instantiate(effectEvent.Prefab, pos, Quaternion.Euler(rot), EffectTrack.EffectParent);
                effectPrefabObj.name = effectEvent.Prefab.name;
                effectPrefabObj.transform.localScale = effectEvent.Scale;
                // 粒子模拟
                ParticleSystem[] particleSystems = effectPrefabObj.GetComponentsInChildren<ParticleSystem>();
                foreach (var particleSystem in particleSystems)
                {
                    int simulateFrame = frameIndex - effectEvent.FrameIndex;
                    particleSystem.Simulate((float)simulateFrame / SkillEditorWindow.Instance.SkillClip.FrameRate);
                }
            }
            else
            {
                CleanupEffectPrefabObject();
            }
        }

        public void ApplyModelTransform()
        {
            if (effectPrefabObj != null)
            {
                Transform characterRoot = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform;
                // 获取模拟坐标
                Vector3 rootPosition = SkillEditorWindow.Instance.GetPositionForRootMotion(effectEvent.FrameIndex, true);
                Vector3 oldPos = characterRoot.position;
                // 把角色临时设置到播放坐标
                characterRoot.position = rootPosition;
                effectEvent.Position = characterRoot.InverseTransformPoint(effectPrefabObj.transform.position);
                effectEvent.Rotation = effectPrefabObj.transform.eulerAngles - characterRoot.eulerAngles;
                effectEvent.Scale = effectPrefabObj.transform.localScale;
                // 还原坐标
                characterRoot.position = oldPos;
            }
        }
        #endregion

        #region Gizmos & SceneGUI

        public void DrawGizmos()
        {
            if (effectEvent.Prefab == null || SkillEditorWindow.Instance.CurrentPreviewCharacterObj == null)
                return;

            Transform previewObjTransform = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform;
            Vector3 pos = previewObjTransform.TransformPoint(effectEvent.Position);
            Quaternion rot = previewObjTransform.rotation * Quaternion.Euler(effectEvent.Rotation);

            float size = HandleUtility.GetHandleSize(pos) * GizmoSizeMultiplier;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(pos, size);
            Gizmos.DrawLine(pos, pos + rot * Vector3.forward * size * 2f);
        }

        public void OnSceneGUI()
        {
            if (effectEvent.Prefab == null || SkillEditorWindow.Instance.CurrentPreviewCharacterObj == null)
                return;

            Transform previewObjTransform = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform;
            Vector3 pos;
            Quaternion rot;
            Vector3 scale;

            if (effectPrefabObj != null)
            {
                pos = effectPrefabObj.transform.position;
                rot = effectPrefabObj.transform.rotation;
                scale = effectPrefabObj.transform.localScale;
            }
            else
            {
                pos = previewObjTransform.TransformPoint(effectEvent.Position);
                rot = previewObjTransform.rotation * Quaternion.Euler(effectEvent.Rotation);
                scale = effectEvent.Scale;
            }

            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref pos, ref rot, ref scale);
            if (EditorGUI.EndChangeCheck())
            {
                effectEvent.Position = previewObjTransform.InverseTransformPoint(pos);
                effectEvent.Rotation = (Quaternion.Inverse(previewObjTransform.rotation) * rot).eulerAngles;
                effectEvent.Scale = scale;

                if (effectPrefabObj != null)
                {
                    effectPrefabObj.transform.position = pos;
                    effectPrefabObj.transform.rotation = rot;
                    effectPrefabObj.transform.localScale = scale;
                }

                SkillEditorInspector.SetTrackItem(this, track);
            }
        }
        
        #endregion
    }
}
