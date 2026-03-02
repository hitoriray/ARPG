using System.Collections.Generic;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;

namespace UI
{
    /// <summary>
    /// 固定列表式交互提示窗口
    /// </summary>
    [UIWindowData(typeof(UI_InteractListWindow), true, nameof(UI_InteractListWindow), 1)]
    public class UI_InteractListWindow : UI_WindowBase
    {
        [Header("配置")]
        [SerializeField] private ListView listView;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField, Range(0.01f, 0.5f)] private float scrollSmoothTime = 0.08f;

        // 当前正在显示的 UI 元素缓存
        private readonly List<ButtonManager> _activeItems = new();
        private static readonly List<string> EmptyNames = new();
        private float _scrollVelocity;
        private float _targetScrollY;
        private bool _hasTargetScroll;

        public override void Init()
        {
            base.Init();
            
            if (listView == null)
                listView = GetComponent<ListView>();
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);

            SetupScrollContent();
            FixMaskForTransparentBackground();
        }

        public override void OnShow()
        {
            base.OnShow();
            RefreshList(null, 0); // 刚开启时先清空一下
            
            // 通知 InteractManager 刷新并推送最新数据过来
            EventSystem.EventTrigger("RequestInteractListUpdate");
            _hasTargetScroll = false;
        }

        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            EventSystem.AddEventListener<List<string>, int>("UpdateInteractList", RefreshList);
        }

        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            EventSystem.RemoveEventListener<List<string>, int>("UpdateInteractList", RefreshList);
        }

        private void Update()
        {
            if (!_hasTargetScroll || scrollRect == null || listView == null || listView.itemParent == null)
                return;

            var content = listView.itemParent as RectTransform;
            if (content == null) return;

            float currentY = content.anchoredPosition.y;
            float nextY = Mathf.SmoothDamp(currentY, _targetScrollY, ref _scrollVelocity, scrollSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, nextY);

            if (Mathf.Abs(nextY - _targetScrollY) < 0.1f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, _targetScrollY);
                _hasTargetScroll = false;
            }
        }

        private void RefreshList(List<string> dropNames, int selectedIndex)
        {
            if (dropNames == null) dropNames = EmptyNames;

            // 1. 回收旧的 Item
            if (listView != null && listView.itemParent != null)
            {
                for (int i = listView.itemParent.childCount - 1; i >= 0; i--)
                {
                    var child = listView.itemParent.GetChild(i);
                    child.gameObject.GameObjectPushPool();
                }
            }
            _activeItems.Clear();

            // 2. 生成新的 Item
            for (int i = 0; i < dropNames.Count; i++)
            {
                var dropName = dropNames[i];
                if (string.IsNullOrEmpty(dropName)) continue;

                var go = ProjectUtility.GetOrInstantiateGameObject(listView.itemPreset, listView.itemParent);
                go.SetActive(true);
                // 配合 JKFrame 等原生 UI 组件保证布局刷新
                go.transform.SetAsLastSibling(); 

                var btn = go.GetComponent<ButtonManager>();
                var item = go.GetComponent<ListViewItem>();

                if (btn != null)
                {
                    // 设置 ButtonManager 名字兜底
                    btn.SetText(i == selectedIndex ? $"E  {dropName}" : $"   {dropName}");
                    
                    // 强制手动播放高亮效果
                    if (i == selectedIndex)
                    {
                        btn.StartCoroutine("SetHighlight");
                    }
                    else
                    {
                        btn.StartCoroutine("SetNormal");
                    }
                    
                    _activeItems.Add(btn);
                }

                if (item != null)
                {
                    // 设置 ListViewItem 名字（Modern UI Pack 列表专用逻辑）
                    item.rowCount = ListView.RowCount.One;
                    item.row0Ref = new ListView.ListRow
                    {
                        rowText = i == selectedIndex ? $"E  {dropName}" : $"●  {dropName}",
                        rowType = ListView.RowType.Text
                    };
                    item.PassReferences();
                }
            }

            // 3. 强制刷新布局，避免滚动区域不更新
            if (listView != null && listView.itemParent is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                EnsureSelectedVisible(selectedIndex);
            }
        }

        private void EnsureSelectedVisible(int selectedIndex)
        {
            if (scrollRect == null || listView == null || listView.itemParent == null)
                return;

            var content = listView.itemParent as RectTransform;
            if (content == null) return;
            if (selectedIndex < 0 || selectedIndex >= content.childCount) return;

            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            if (viewport == null) return;

            var item = content.GetChild(selectedIndex) as RectTransform;
            if (item == null) return;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            float paddingTop = layout != null ? layout.padding.top : 0f;
            float paddingBottom = layout != null ? layout.padding.bottom : 0f;

            float itemHeight = item.rect.height;
            float itemTop = paddingTop + selectedIndex * (itemHeight + spacing);
            float itemBottom = itemTop + itemHeight;

            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

            float scrollY = Mathf.Clamp(content.anchoredPosition.y, 0f, maxScroll);

            if (itemTop < scrollY)
                scrollY = itemTop;
            else if (itemBottom > scrollY + viewportHeight)
                scrollY = itemBottom - viewportHeight;

            scrollY = Mathf.Clamp(scrollY, 0f, maxScroll);
            _targetScrollY = scrollY;
            _hasTargetScroll = true;
        }

        private void SetupScrollContent()
        {
            if (listView == null || listView.itemParent == null || scrollRect == null) return;

            var content = listView.itemParent as RectTransform;
            if (content == null) return;

            // 让 Content 按顶部对齐并根据子项高度自动扩展，保证 ScrollRect 可滚动
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 确保 ScrollRect 绑定 Content
            if (scrollRect.content == null)
                scrollRect.content = content;
        }

        private void FixMaskForTransparentBackground()
        {
            if (scrollRect == null) return;
            var mask = scrollRect.GetComponent<Mask>();
            if (mask == null) return;

            // 不渲染 Mask 的图像，避免背景可见
            mask.showMaskGraphic = false;

            // 保持 Mask 图像 alpha 为 1，避免透明度导致子项被裁掉
            var graphic = mask.GetComponent<Graphic>();
            if (graphic != null)
            {
                var c = graphic.color;
                c.a = 1f;
                graphic.color = c;
            }
        }
    }
}
