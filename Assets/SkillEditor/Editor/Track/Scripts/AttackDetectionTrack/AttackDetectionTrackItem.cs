using System;
using System.Numerics;
using Config;
using Player.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace SkillEditor
{
    public class AttackDetectionTrackItem : TrackItemBase<AttackDetectionTrack>
    {
        private SkillMultiLineTrackStyle.ChildTrack childTrack;
        private SkillAttackDetectionTrackItemStyle audioItemStyle;
        private SkillAttackDetectionEvent attackDetectionEvent;
        public SkillAttackDetectionEvent AttackDetectionEvent => attackDetectionEvent;
        
        public void Init(AttackDetectionTrack track, float frameUnitWidth, SkillAttackDetectionEvent attackDetectionEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            this.track = track;
            this.frameIndex = attackDetectionEvent.FrameIndex;
            this.childTrack = childTrack;
            this.attackDetectionEvent = attackDetectionEvent;
            normalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
            selectColor = new Color(0.388f, 0.850f, 0.905f, 1f);
            audioItemStyle = new SkillAttackDetectionTrackItemStyle();
            ItemStyle = audioItemStyle;

            ResetView(frameUnitWidth);
        }

        public override void ResetView(float frameUnitWidth)
        {
            base.ResetView(frameUnitWidth);
            if (audioItemStyle.IsInit == false)
            {
                audioItemStyle.Init(frameUnitWidth, attackDetectionEvent, childTrack);
                BindEvents();
            }
            audioItemStyle.ResetView(frameUnitWidth, attackDetectionEvent);
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
                attackDetectionEvent.FrameIndex = frameIndex;
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
            
            attackDetectionEvent.FrameIndex = frameIndex;
            SkillEditorInspector.Instance.SetTrackItemFrameIndex(frameIndex);
        }

        public void CheckFrameCount()
        {
            int frameCount = (int)(attackDetectionEvent.DurationFrame * SkillEditorWindow.Instance.SkillConfig.FrameRate);
            // 如果超出右侧边界，则拓展边界
            if (frameIndex + frameCount > SkillEditorWindow.Instance.SkillConfig.FrameCount)
            {
                SkillEditorWindow.Instance.CurrentFrameCount = frameIndex + frameCount;
            }
        }
        
        #endregion
        
        #region Gizmos & SceneGUI

        public void DrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Matrix4x4 rotationMat = new();
            switch (attackDetectionEvent.AttackDetectionType)
            {
                case AttackDetectionType.Weapon:
                    WeaponDetectionData weaponDetectionData = (WeaponDetectionData)attackDetectionEvent.AttackDetectionData;
                    SkillPlayer skillPlayer = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.GetComponent<SkillPlayer>();
                    if (!string.IsNullOrEmpty(weaponDetectionData.WeaponName) &&
                        skillPlayer.WeaponDict.TryGetValue(weaponDetectionData.WeaponName, out var skillWeapon))
                    {
                        Collider collider = skillWeapon.GetComponent<Collider>();
                        rotationMat.SetTRS(collider.transform.position, collider.transform.rotation, collider.transform.localScale);
                        Gizmos.matrix = rotationMat;
                        if (collider is BoxCollider boxCollider)
                        {
                            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                        }
                        else if (collider is SphereCollider sphereCollider)
                        {
                            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                        }
                    }
                    
                    break;
                case AttackDetectionType.Box:
                    BoxDetectionData boxDetectionData = (BoxDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform.TransformPoint(boxDetectionData.Position);
                    Quaternion rot = Quaternion.Euler(boxDetectionData.Rotation) * SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform.rotation;
                    rotationMat.SetTRS(pos, rot, Vector3.one);
                    Gizmos.matrix = rotationMat;
                    Gizmos.DrawCube(Vector3.zero, boxDetectionData.Scale);
                    break;
                case AttackDetectionType.Sphere:
                    SphereDetectionData sphereDetectionData = (SphereDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos2 = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform.TransformPoint(sphereDetectionData.Position);
                    Gizmos.DrawSphere(pos2, sphereDetectionData.Radius);
                    break;
                case AttackDetectionType.Fan:
                    FanDetectionData fanDetectionData = (FanDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos3 = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform.TransformPoint(fanDetectionData.Position);
                    Quaternion rot3 = Quaternion.Euler(fanDetectionData.Rotation) * SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform.rotation;
                    Mesh mesh = MeshGenerator.GenerateFanMesh(fanDetectionData.InsideRadius, fanDetectionData.Radius,
                        fanDetectionData.Height, fanDetectionData.Angle);
                    Gizmos.DrawMesh(mesh, pos3, rot3);
                    break;
            }
            
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.identity;
        }

        public void OnSceneGUI()
        {
            Transform previewObjTransform = SkillEditorWindow.Instance.CurrentPreviewCharacterObj.transform; 
            switch (attackDetectionEvent.AttackDetectionType)
            {
                case AttackDetectionType.Weapon:
                    break;
                case AttackDetectionType.Box:
                    BoxDetectionData boxDetectionData = (BoxDetectionData)attackDetectionEvent.AttackDetectionData;
                    Quaternion rot = previewObjTransform.rotation * Quaternion.Euler(boxDetectionData.Rotation);
                    Vector3 pos = previewObjTransform.TransformPoint(boxDetectionData.Position);
                    EditorGUI.BeginChangeCheck();
                    Handles.TransformHandle(ref pos, ref rot, ref boxDetectionData.Scale);
                    // 如果发生了修改
                    if (EditorGUI.EndChangeCheck())
                    {
                        boxDetectionData.Position = previewObjTransform.InverseTransformPoint(pos);
                        boxDetectionData.Rotation = (Quaternion.Inverse(previewObjTransform.rotation) * rot).eulerAngles;
                        SkillEditorInspector.SetTrackItem(this, track);
                    }
                    break;
                case AttackDetectionType.Sphere:
                    SphereDetectionData sphereDetectionData = (SphereDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 oldPos = previewObjTransform.TransformPoint(sphereDetectionData.Position);
                    Vector3 newPos = Handles.PositionHandle(oldPos, Quaternion.identity);
                    float newRadius = Handles.ScaleSlider(sphereDetectionData.Radius, newPos, 
                        Vector3.up, Quaternion.identity, sphereDetectionData.Radius + 0.5f, 0.1f);
                    if (oldPos != newPos || !Mathf.Approximately(sphereDetectionData.Radius, newRadius))
                    {
                        sphereDetectionData.Position = previewObjTransform.InverseTransformPoint(newPos);
                        sphereDetectionData.Radius = newRadius;
                        SkillEditorInspector.SetTrackItem(this, track);
                    }
                    break;
                case AttackDetectionType.Fan:
                    FanDetectionData fanDetectionData = (FanDetectionData)attackDetectionEvent.AttackDetectionData;
                    Quaternion fanRot = previewObjTransform.rotation * Quaternion.Euler(fanDetectionData.Rotation);
                    Vector3 fanPos = previewObjTransform.TransformPoint(fanDetectionData.Position);
                    // fanScale  x:角度  y:高度  z:外半径
                    Vector3 fanScale = new Vector3(fanDetectionData.Angle, fanDetectionData.Height, fanDetectionData.Radius);
                    EditorGUI.BeginChangeCheck();
                    Handles.TransformHandle(ref fanPos, ref fanRot, ref fanScale);
                    float insideRadius = Handles.ScaleSlider(fanDetectionData.InsideRadius, fanPos, 
                        -previewObjTransform.forward, Quaternion.identity, 1.5f, 0.1f);
                    // 如果发生了修改
                    if (EditorGUI.EndChangeCheck())
                    {
                        fanDetectionData.Position = previewObjTransform.InverseTransformPoint(fanPos);
                        fanDetectionData.Rotation = (Quaternion.Inverse(previewObjTransform.rotation) * fanRot).eulerAngles;
                        fanDetectionData.Angle = fanScale.x;
                        fanDetectionData.Height = fanScale.y;
                        fanDetectionData.Radius = fanScale.z;
                        fanDetectionData.InsideRadius = insideRadius;
                        SkillEditorInspector.SetTrackItem(this, track);
                    }
                    break;
            }
        }
        
        #endregion
    }
}