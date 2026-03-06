using System.Collections.Generic;
using JKFrame;
using Manager;
using Michsky.MUIP;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_DialogWindow), true, nameof(UI_DialogWindow), 1)]
    public class UI_DialogWindow : UI_WindowBase
    {
        [Header("Window")]
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private string defaultNpcId = "tavern_default";

        [Header("Prefabs")]
        [SerializeField] private GameObject tabButtonPrefab;
        [SerializeField] private UI_DialogSubWindow subWindowPrefab;
        [SerializeField] private Transform buttonsRoot;
        [SerializeField] private Transform windowsRoot;

        private readonly List<UI_DialogSubWindow> subWindows = new();
        private readonly List<AIDialogManager.NpcInfo> npcInfos = new();

        private string currentNpcId;
        private int currentWindowIndex;
        private bool isInited;

        public override void Init()
        {
            base.Init();
            ResolveReferencesIfNeeded();
            RebuildTabs();
            isInited = true;
        }

        public override void OnShow()
        {
            base.OnShow();

            currentNpcId = NormalizeNpcId(currentNpcId);
            RebuildTabs();
            SelectNpcTab(currentNpcId, true);

            PlayerService.Instance?.SetCharacterControl(false);
            InputService.Instance?.inputMap?.UI.Disable();
            PlayerService.Instance?.PushUICursor();
            UIModalStack.Push(OnCloseBtnClick);
        }

        public UI_DialogWindow Show(string npcId)
        {
            currentNpcId = NormalizeNpcId(npcId);
            if (isInited) SelectNpcTab(currentNpcId, true);
            return this;
        }

        public override void OnClose()
        {
            base.OnClose();
            PlayerService.Instance?.SetCharacterControl(true);
            InputService.Instance?.inputMap?.UI.Enable();
            PlayerService.Instance?.PopUICursor();
            UIModalStack.Remove(OnCloseBtnClick);
        }

        private void OnDestroy()
        {
            if (windowManager != null)
                windowManager.onWindowChange.RemoveListener(OnWindowChanged);

            ClearSubWindows();
        }

        private void ResolveReferencesIfNeeded()
        {
            if (windowManager == null)
                windowManager = GetComponentInChildren<WindowManager>(true);
        }

        private void RebuildTabs()
        {
            ResolveReferencesIfNeeded();
            ClearSubWindows();

            AIDialogManager dialogManager = AIDialogManager.Instance;
            if (dialogManager == null)
            {
                return;
            }

            dialogManager.GetAvailableNpcInfos(npcInfos);
            if (npcInfos.Count == 0)
            {
                string fallbackNpcId = NormalizeNpcId(defaultNpcId);
                npcInfos.Add(new AIDialogManager.NpcInfo(fallbackNpcId, dialogManager.GetNpcDisplayName(fallbackNpcId), null));
            }

            if (windowManager == null || buttonsRoot == null || windowsRoot == null || tabButtonPrefab == null || subWindowPrefab == null)
            {
                RayDebug.Error("UI_DialogWindow 缺少必要的预制体或容器引用配置！(buttonsRoot, windowsRoot, tabButtonPrefab, subWindowPrefab 等)");
                return;
            }

            windowManager.windows.Clear();
            for (int i = 0; i < npcInfos.Count; i++)
            {
                AIDialogManager.NpcInfo info = npcInfos[i];
                
                // 实例化
                GameObject buttonObject = Instantiate(tabButtonPrefab, buttonsRoot);
                buttonObject.name = $"Tab_{info.NpcId}";
                buttonObject.SetActive(true);

                UI_DialogSubWindow subWindow = Instantiate(subWindowPrefab, windowsRoot);
                subWindow.gameObject.SetActive(false);
                subWindow.gameObject.name = $"SubWindow_{info.NpcId}";

                SetupTabButton(buttonObject, info);
                
                subWindow.Init(i, info.NpcId);
                subWindow.OnSendRequested = SendUserMessage;
                subWindow.OnCloseRequested = OnCloseBtnClick;

                subWindows.Add(subWindow);

                windowManager.windows.Add(new WindowManager.WindowItem
                {
                    windowName = $"{i}_{info.NpcId}",
                    windowObject = subWindow.gameObject,
                    buttonObject = buttonObject,
                    firstSelected = subWindow.FirstSelected
                });
            }

            int initialIndex = FindContextIndexByNpcId(currentNpcId);
            if (initialIndex < 0) initialIndex = 0;
            windowManager.currentWindowIndex = initialIndex;

            if (windowManager.windows.Count > initialIndex)
            {
                var targetItem = windowManager.windows[initialIndex];
                if (targetItem.windowObject != null) targetItem.windowObject.SetActive(true);
                if (targetItem.buttonObject != null) targetItem.buttonObject.SetActive(true);
            }

            windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            windowManager.onWindowChange.AddListener(OnWindowChanged);
            windowManager.InitializeWindows();

            ApplyWindowIndex(windowManager.currentWindowIndex, true);
        }

        private void ClearSubWindows()
        {
            if (buttonsRoot != null)
            {
                foreach (Transform child in buttonsRoot) Destroy(child.gameObject);
            }
            if (windowsRoot != null)
            {
                foreach (Transform child in windowsRoot) Destroy(child.gameObject);
            }
            subWindows.Clear();
        }

        private void SetupTabButton(GameObject buttonObject, AIDialogManager.NpcInfo info)
        {
            if (buttonObject == null) return;
            ButtonManager button = buttonObject.GetComponent<ButtonManager>();
            if (button == null) button = buttonObject.GetComponentInChildren<ButtonManager>(true);
            if (button == null) return;

            string displayName = string.IsNullOrWhiteSpace(info?.DisplayName) ? "角色" : info.DisplayName.Trim();
            bool hasIcon = info != null && info.Icon != null;
            bool hasIconSlots = button.normalImage != null || button.highlightImage != null || button.disabledImage != null;
            bool useIcon = hasIcon && hasIconSlots;

            button.enableIcon = useIcon;
            button.enableText = !useIcon;
            button.SetText(displayName);
            button.SetIcon(useIcon ? info.Icon : null);
            button.UpdateUI();
        }

        private void SelectNpcTab(string npcId, bool refreshIfSame)
        {
            if (subWindows.Count == 0) return;

            int targetIndex = FindContextIndexByNpcId(npcId);
            if (targetIndex < 0) targetIndex = 0;

            if (windowManager != null && windowManager.windows != null && targetIndex < windowManager.windows.Count)
            {
                if (windowManager.currentWindowIndex != targetIndex)
                {
                    windowManager.OpenWindowByIndex(targetIndex);
                    return;
                }
            }

            ApplyWindowIndex(targetIndex, refreshIfSame);
        }

        private int FindContextIndexByNpcId(string npcId)
        {
            string normalizedNpcId = NormalizeNpcId(npcId);
            for (int i = 0; i < subWindows.Count; i++)
            {
                if (string.Equals(subWindows[i].NpcId, normalizedNpcId, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void OnWindowChanged(int index)
        {
            ApplyWindowIndex(index, true);
        }

        private void ApplyWindowIndex(int index, bool refresh)
        {
            if (subWindows.Count == 0) return;

            currentWindowIndex = Mathf.Clamp(index, 0, subWindows.Count - 1);
            UI_DialogSubWindow subWindow = subWindows[currentWindowIndex];
            currentNpcId = subWindow.NpcId;

            AIDialogManager.Instance?.SetCurrentNpc(currentNpcId);
            if (refresh)
            {
                subWindow.Refresh();
            }
        }

        private void OnCloseBtnClick()
        {
            UISystem.Close<UI_DialogWindow>();
        }

        private void SendUserMessage(int windowIndex)
        {
            if (windowIndex < 0 || windowIndex >= subWindows.Count) return;
            UI_DialogSubWindow subWindow = subWindows[windowIndex];

            if (AIDialogManager.Instance == null) return;

            string userMessage = subWindow.GetInputTextAndClear();
            if (string.IsNullOrEmpty(userMessage)) return;

            subWindow.SetAiResponseStatus("emmmm...");

            currentWindowIndex = windowIndex;
            currentNpcId = subWindow.NpcId;
            AIDialogManager.Instance.SetCurrentNpc(currentNpcId);

            StartCoroutine(AIDialogManager.Instance.SendMessageToNpc(currentNpcId, userMessage,
                response =>
                {
                    subWindow.SetAiResponseStatus("");
                    subWindow.Refresh(true, response);
                },
                error =>
                {
                    subWindow.SetAiResponseStatus($"错误：{error}");
                    subWindow.Refresh();
                }));

            subWindow.Refresh();
        }

        private string NormalizeNpcId(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return string.IsNullOrWhiteSpace(defaultNpcId) ? "tavern_default" : defaultNpcId.Trim();
            return npcId.Trim();
        }
    }
}
