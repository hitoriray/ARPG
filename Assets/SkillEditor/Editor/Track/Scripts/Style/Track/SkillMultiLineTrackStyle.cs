using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillMultiLineTrackStyle : SkillTrackStyleBase
    {
        #region Const
        private const string MenuAssetPath = "Assets/SkillEditor/Editor/Track/Assets/MultiLineTrackStyle/MultiLineTrackMenu.uxml";
        private const string TrackAssetPath = "Assets/SkillEditor/Editor/Track/Assets/MultiLineTrackStyle/MultiLineTrackContent.uxml";
        private const float headHeight = 35f;
        private const float itemHeight = 32f;
        #endregion
        
        private Action addChildTrackAction = null;
        private Func<int, bool> deleteChildTrackFunc = null;
        private Action<int, int> swapChildTrackAction = null;
        private Action<ChildTrack, string> updateTrackNameAction = null;
        private readonly List<ChildTrack> childTracks = new();
        private VisualElement menuItemParent;
        
        public void Init(VisualElement menuParent, VisualElement contentParent, string title, 
            Action addChildTrackAction, 
            Func<int, bool> deleteChildTrackFunc,
            Action<int, int> swapChildTrackAction,
            Action<ChildTrack, string> updateTrackNameAction)
        {
            this.menuParent = menuParent;
            this.contentParent = contentParent;
            this.addChildTrackAction = addChildTrackAction;
            this.deleteChildTrackFunc = deleteChildTrackFunc;
            this.swapChildTrackAction = swapChildTrackAction;
            this.updateTrackNameAction = updateTrackNameAction;
            
            menuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuAssetPath).Instantiate().Query().ToList()[1];
            menuParent.Add(menuRoot);
            contentRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackAssetPath).Instantiate().Query().ToList()[1];
            contentParent.Add(contentRoot);
            
            titleLabel = menuRoot.Q<Label>("Title");
            titleLabel.text = title;
            
            menuItemParent = menuRoot.Q<VisualElement>("TrackMenuList");
            menuItemParent.RegisterCallback<MouseDownEvent>(OnMenuItemParentMouseDown);
            menuItemParent.RegisterCallback<MouseMoveEvent>(OnMenuItemParentMouseMove);
            menuItemParent.RegisterCallback<MouseUpEvent>(OnMenuItemParentMouseUp);
            menuItemParent.RegisterCallback<MouseOutEvent>(OnMenuItemParentMouseOut);
            
            // 添加子轨道的按钮
            Button addBtn = menuRoot.Q<Button>("AddBtn");
            addBtn.clicked += OnAddBtnClicked;
            
            UpdateSize();
        }

        #region 子轨道拖拽事件

        private bool isDragging = false;
        private int selectTrackIndex = -1;
        private void OnMenuItemParentMouseDown(MouseDownEvent evt)
        {
            // 关闭旧的选中
            if (selectTrackIndex != -1)
            {
                childTracks[selectTrackIndex].Unselect();
            }
            // 通过高度推导出当前交互的是第几个
            float mousePosY = evt.localMousePosition.y - itemHeight / 2;
            selectTrackIndex = GetChildIndexByMousePosY(mousePosY);
            childTracks[selectTrackIndex].Select();
            
            // 拖拽
            isDragging = true;
        }

        private void OnMenuItemParentMouseMove(MouseMoveEvent evt)
        {
            if (isDragging && selectTrackIndex != -1)
            {
                float mousePosY = evt.localMousePosition.y - itemHeight / 2;
                int trackIndex = GetChildIndexByMousePosY(mousePosY);
                // 确保交换有意义
                if (trackIndex != selectTrackIndex)
                {
                    SwapChildTrack(trackIndex, selectTrackIndex);
                    selectTrackIndex = trackIndex; // 把选中的轨道更新为当前鼠标所在的轨道，这样才能持续拖拽且更新
                }
            }
        }

        private void OnMenuItemParentMouseUp(MouseUpEvent evt)
        {
            isDragging = false;
        }

        private void OnMenuItemParentMouseOut(MouseOutEvent evt)
        {
            // 这个函数经常会无意义调用，因为子物体和我们本身会产生遮挡关系，就是说如果在拖拽的过程中鼠标碰到了子物体就会调用一次该函数
            if (menuItemParent.contentRect.Contains(evt.localMousePosition) == false) // 检测鼠标位置是否真的离开范围
            {
                isDragging = false;
            }
        }

        private int GetChildIndexByMousePosY(float y)
        {
            int trackIndex = Mathf.RoundToInt(y / itemHeight);
            trackIndex = Mathf.Clamp(trackIndex, 0, childTracks.Count - 1);
            return trackIndex;
        }
        #endregion

        private void SwapChildTrack(int index1, int index2)
        {
            // 不验证范围有效性，如果出错，说明本身逻辑就有问题
            if (index1 == index2)
                return;
            var childTrack1 = childTracks[index1];
            var childTrack2 = childTracks[index2];
            childTracks[index1] = childTrack2;
            childTracks[index2] = childTrack1;
            UpdateChildTracksIndex();
            // 上级轨道的实际数据变更
            swapChildTrackAction(index1, index2);
        }

        private void UpdateSize()
        {
            var height = headHeight + childTracks.Count * itemHeight;
            contentRoot.style.height = height;
            menuRoot.style.height = height;
            menuItemParent.style.height = childTracks.Count * itemHeight;
        }

        #region 子轨道相关
        /// <summary>
        /// 添加子轨道
        /// </summary>
        private void OnAddBtnClicked()
        {
            addChildTrackAction?.Invoke();
        }

        /// <summary>
        /// 添加子轨道辅助方法（供外部调用）
        /// </summary>
        public ChildTrack AddChildTrack()
        {
            ChildTrack childTrack = new ChildTrack();
            childTrack.Init(menuItemParent, contentRoot, childTracks.Count, DeleteChildTrackAndData, DeleteChildTrack, updateTrackNameAction);
            childTracks.Add(childTrack);
            UpdateSize();
            return childTrack;
        }

        /// <summary>
        /// 删除子轨道及其数据
        /// </summary>
        private void DeleteChildTrackAndData(ChildTrack childTrack)
        {
            if (deleteChildTrackFunc == null)
                return;
            
            // 由上级具体轨道类来判断能不能添加
            int index = childTrack.GetIndex();
            if (deleteChildTrackFunc(index))
            {
                childTrack.DoDestroy();
                childTracks.RemoveAt(index);
                // 所有的子轨道都需要更新一下索引
                UpdateChildTracksIndex(index);
                UpdateSize();
            }
        }
        
        /// <summary>
        /// 删除子轨道（显示层面）
        /// </summary>
        private void DeleteChildTrack(ChildTrack childTrack)
        {
            int index = childTrack.GetIndex();
            childTrack.DoDestroy();
            childTracks.RemoveAt(index);
            // 所有的子轨道都需要更新一下索引
            UpdateChildTracksIndex(index);
            UpdateSize();
        }

        /// <summary>
        /// 更新子轨道列表的索引
        /// </summary>
        /// <param name="startIndex"></param>
        private void UpdateChildTracksIndex(int startIndex = 0)
        {
            for (int i = startIndex; i < childTracks.Count; i++)
            {
                childTracks[i].SetIndex(i);
            }
        }
        
        #endregion
        
        /// <summary>
        /// 多行轨道中的子轨道
        /// </summary>
        public class ChildTrack
        {
            private const string ChildMenuItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/MultiLineTrackStyle/MultiLineTrackItem.uxml";
            private const string ChildTrackContentAssetPath = "Assets/SkillEditor/Editor/Track/Assets/MultiLineTrackStyle/MultiLineTrackContentItem.uxml";
            
            public VisualElement menuRoot;
            public VisualElement trackRoot;

            public VisualElement menuParent;
            public VisualElement trackParent;

            private TextField trackNameField;

            private Action<ChildTrack> deleteAction;
            private Action<ChildTrack> destroyAction;
            private Action<ChildTrack, string> updateTrackNameAction;

            private int index;

            private static Color normalColor = new Color(0, 0, 0, 0);
            private static Color selectColor = Color.green;
            
            private VisualElement content;
            
            public void Init(VisualElement menuParent, VisualElement trackParent, int index, 
                Action<ChildTrack> deleteAction, 
                Action<ChildTrack> destroyAction,
                Action<ChildTrack, string> updateTrackNameAction)
            {
                this.menuParent = menuParent;
                this.trackParent = trackParent;
                this.deleteAction = deleteAction;
                this.destroyAction = destroyAction;
                this.updateTrackNameAction = updateTrackNameAction;
                
                menuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChildMenuItemAssetPath).Instantiate().Query().ToList()[1];
                menuParent.Add(menuRoot);
                trackRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChildTrackContentAssetPath).Instantiate().Query().ToList()[1];
                trackParent.Add(trackRoot);
                
                trackNameField = menuRoot.Q<TextField>("NameField");
                trackNameField.RegisterCallback<FocusInEvent>(OnTrackNameFieldFocusIn);
                trackNameField.RegisterCallback<FocusOutEvent>(OnTrackNameFieldFocusOut);
                
                Button deleteBtn = menuRoot.Q<Button>("DeleteBtn");
                deleteBtn.clicked += () => this.deleteAction(this);
                
                SetIndex(index);
                Unselect();
            }

            #region TrackNameField
            private string oldTrackNameFieldValue;
            private void OnTrackNameFieldFocusIn(FocusInEvent evt)
            {
                oldTrackNameFieldValue = trackNameField.value;
            }

            private void OnTrackNameFieldFocusOut(FocusOutEvent evt)
            {
                if (oldTrackNameFieldValue != trackNameField.value)
                {
                    updateTrackNameAction?.Invoke(this, trackNameField.value);
                }
            }
            #endregion

            public void InitContent(VisualElement content)
            {
                this.content = content;
                trackRoot.Add(this.content);
            }
            
            public void SetTrackName(string trackName)
            {
                trackNameField.value = trackName;
            }
            
            public void SetIndex(int idx)
            {
                index = idx;
                
                float height = 0;
                Vector3 pos = menuRoot.transform.position;
                height = index * itemHeight;
                pos.y = height;
                menuRoot.transform.position = pos;
                    
                pos = trackRoot.transform.position;
                height = index * itemHeight + headHeight;
                pos.y = height;
                trackRoot.transform.position = pos;
            }

            public int GetIndex()
            {
                return index;
            }

            public void Select()
            {
                menuRoot.style.backgroundColor = selectColor;
            }

            public void Unselect()
            {
                menuRoot.style.backgroundColor = normalColor;
            }
            
            public void Destroy()
            {
                destroyAction(this);
            }

            public void DoDestroy()
            {
                menuParent.Remove(menuRoot);
                trackParent.Remove(trackRoot);
            }
        }
    }
}