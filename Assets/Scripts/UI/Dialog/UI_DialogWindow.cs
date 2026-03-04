using System;
using System.Collections;
using System.Collections.Generic;
using JKFrame;
using Manager;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_DialogWindow), true, nameof(UI_DialogWindow), 1)]
    public class UI_DialogWindow : UI_WindowBase
    {
        [Serializable]
        private sealed class SubWindowContext
        {
            public int WindowIndex;
            public string NpcId;
            public string DisplayName;
            public GameObject WindowObject;
            public GameObject ButtonObject;

            public TMP_InputField InputField;
            public ButtonManager SendButton;
            public ButtonManager CloseButton;
            public TextMeshProUGUI AiResponseText;
            public ListView ChatList;
            public TextMeshProUGUI TitleText;

            [NonSerialized] public UnityAction SendAction;
            [NonSerialized] public UnityAction CloseAction;
            [NonSerialized] public UnityAction<string> EndEditAction;
            [NonSerialized] public Coroutine TypingCoroutine;
            [NonSerialized] public TextMeshProUGUI TypingText;
            [NonSerialized] public string TypingFullText;
            [NonSerialized] public bool IsTyping;
        }

        [Header("Window")]
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private string defaultNpcId = "tavern_default";

        [Header("Legacy Fallback (Optional)")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ButtonManager sendButton;
        [SerializeField] private ButtonManager closeButton;
        [SerializeField] private TextMeshProUGUI aiResponseText;
        [SerializeField] private ListView chatList;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Chat Bubble")]
        [SerializeField, Min(120f)] private float bubbleMaxWidth = 840f;
        [SerializeField, Min(40f)] private float bubbleMinWidth = 120f;
        [SerializeField, Min(0f)] private float bubbleHorizontalPadding = 22f;
        [SerializeField, Min(0f)] private float bubbleVerticalPadding = 14f;
        [SerializeField, Min(0f)] private float messageOuterPadding = 10f;
        [SerializeField, Min(0f)] private float sideIndent = 160f;
        [SerializeField] private Sprite bubbleSpriteOverride;
        [SerializeField, Min(0.1f)] private float bubblePixelsPerUnitMultiplier = 0.65f;
        [SerializeField] private Color userBubbleColor = new(0.23f, 0.54f, 0.96f, 0.95f);
        [SerializeField] private Color assistantBubbleColor = new(0.20f, 0.20f, 0.24f, 0.95f);
        [SerializeField] private Color userTextColor = Color.white;
        [SerializeField] private Color assistantTextColor = Color.white;
        [SerializeField] private bool showSpeakerName = false;

        [Header("Typewriter")]
        [SerializeField, Min(1f)] private float typingCharsPerSecond = 36f;
        [SerializeField, Min(0f)] private float typingSkipInputDelay = 0.12f;
        [SerializeField] private bool clickToSkipTyping = true;

        private readonly List<SubWindowContext> contexts = new();
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
            if (isInited)
            {
                SelectNpcTab(currentNpcId, true);
            }

            return this;
        }

        public override void OnClose()
        {
            base.OnClose();
            StopAllTyping(true);
            PlayerService.Instance?.SetCharacterControl(true);
            InputService.Instance?.inputMap?.UI.Enable();
            PlayerService.Instance?.PopUICursor();
            UIModalStack.Remove(OnCloseBtnClick);
        }

        private void OnDestroy()
        {
            if (windowManager != null)
            {
                windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            }

            StopAllTyping(false);
            UnbindSubWindowListeners();
        }

        private void ResolveReferencesIfNeeded()
        {
            if (windowManager == null)
            {
                windowManager = GetComponentInChildren<WindowManager>(true);
            }
        }

        private void RebuildTabs()
        {
            ResolveReferencesIfNeeded();
            StopAllTyping(true);
            UnbindSubWindowListeners();
            contexts.Clear();

            AIDialogManager dialogManager = AIDialogManager.Instance;
            if (dialogManager == null)
            {
                BuildFallbackSingleTab();
                return;
            }

            dialogManager.GetAvailableNpcInfos(npcInfos);
            if (npcInfos.Count == 0)
            {
                string fallbackNpcId = NormalizeNpcId(defaultNpcId);
                npcInfos.Add(new AIDialogManager.NpcInfo(fallbackNpcId, dialogManager.GetNpcDisplayName(fallbackNpcId), null));
            }

            if (windowManager == null || !TryGetTemplateRoots(out Transform buttonsRoot, out Transform windowsRoot))
            {
                BuildFallbackSingleTab();
                return;
            }

            EnsureChildCount(buttonsRoot, npcInfos.Count);
            EnsureChildCount(windowsRoot, npcInfos.Count);

            for (int i = 0; i < buttonsRoot.childCount; i++)
            {
                buttonsRoot.GetChild(i).gameObject.SetActive(i < npcInfos.Count);
            }

            for (int i = 0; i < windowsRoot.childCount; i++)
            {
                windowsRoot.GetChild(i).gameObject.SetActive(i < npcInfos.Count);
            }

            windowManager.windows.Clear();
            for (int i = 0; i < npcInfos.Count; i++)
            {
                AIDialogManager.NpcInfo info = npcInfos[i];
                GameObject windowObject = windowsRoot.GetChild(i).gameObject;
                GameObject buttonObject = buttonsRoot.GetChild(i).gameObject;

                SetupTabButton(buttonObject, info);

                SubWindowContext context = BuildContext(i, info, windowObject, buttonObject);
                contexts.Add(context);

                windowManager.windows.Add(new WindowManager.WindowItem
                {
                    windowName = BuildWindowName(info, i),
                    windowObject = windowObject,
                    buttonObject = buttonObject,
                    firstSelected = context.InputField != null ? context.InputField.gameObject : null
                });
            }

            int initialIndex = FindContextIndexByNpcId(currentNpcId);
            if (initialIndex < 0) initialIndex = 0;
            windowManager.currentWindowIndex = initialIndex;

            windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            windowManager.onWindowChange.AddListener(OnWindowChanged);
            windowManager.InitializeWindows();

            currentWindowIndex = Mathf.Clamp(windowManager.currentWindowIndex, 0, contexts.Count - 1);
            currentNpcId = contexts[currentWindowIndex].NpcId;
            dialogManager.SetCurrentNpc(currentNpcId);
            RefreshSubWindow(contexts[currentWindowIndex]);
        }

        private void BuildFallbackSingleTab()
        {
            if (windowManager != null)
            {
                windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            }

            string fallbackNpcId = NormalizeNpcId(currentNpcId);
            var context = new SubWindowContext
            {
                WindowIndex = 0,
                NpcId = fallbackNpcId,
                DisplayName = AIDialogManager.Instance != null
                    ? AIDialogManager.Instance.GetNpcDisplayName(fallbackNpcId)
                    : "酒馆对话",
                WindowObject = gameObject,
                ButtonObject = null,
                InputField = inputField,
                SendButton = sendButton,
                CloseButton = closeButton,
                AiResponseText = aiResponseText,
                ChatList = chatList,
                TitleText = titleText
            };

            BindContextListeners(context);
            contexts.Add(context);

            currentWindowIndex = 0;
            currentNpcId = fallbackNpcId;
            AIDialogManager.Instance?.SetCurrentNpc(currentNpcId);
            RefreshSubWindow(context);
        }

        private bool TryGetTemplateRoots(out Transform buttonsRoot, out Transform windowsRoot)
        {
            buttonsRoot = null;
            windowsRoot = null;
            if (windowManager == null) return false;

            Transform root = windowManager.transform;
            buttonsRoot = root.Find("Buttons");
            windowsRoot = root.Find("Windows");
            if (buttonsRoot == null || windowsRoot == null) return false;
            if (buttonsRoot.childCount == 0 || windowsRoot.childCount == 0) return false;

            return true;
        }

        private static void EnsureChildCount(Transform root, int requiredCount)
        {
            if (root == null || requiredCount <= 0) return;
            GameObject template = root.GetChild(0).gameObject;

            while (root.childCount < requiredCount)
            {
                GameObject clone = Instantiate(template, root);
                clone.name = template.name;
            }
        }

        private SubWindowContext BuildContext(
            int windowIndex,
            AIDialogManager.NpcInfo info,
            GameObject windowObject,
            GameObject buttonObject)
        {
            var context = new SubWindowContext
            {
                WindowIndex = windowIndex,
                NpcId = NormalizeNpcId(info.NpcId),
                DisplayName = string.IsNullOrWhiteSpace(info.DisplayName)
                    ? NormalizeNpcId(info.NpcId)
                    : info.DisplayName.Trim(),
                WindowObject = windowObject,
                ButtonObject = buttonObject,
                InputField = FindComponentByName<TMP_InputField>(windowObject.transform, "InputField"),
                SendButton = FindComponentByName<ButtonManager>(windowObject.transform, "BtnSend"),
                CloseButton = FindComponentByName<ButtonManager>(windowObject.transform, "BtnClose"),
                AiResponseText = FindComponentByName<TextMeshProUGUI>(windowObject.transform, "AIResp"),
                ChatList = FindComponentByName<ListView>(windowObject.transform, "ChatList"),
                TitleText = FindComponentByName<TextMeshProUGUI>(windowObject.transform, "Title")
            };

            if (windowIndex == 0)
            {
                inputField = context.InputField;
                sendButton = context.SendButton;
                closeButton = context.CloseButton;
                aiResponseText = context.AiResponseText;
                chatList = context.ChatList;
                titleText = context.TitleText;
            }

            BindContextListeners(context);
            return context;
        }

        private void BindContextListeners(SubWindowContext context)
        {
            if (context.SendButton != null)
            {
                context.SendAction = () => SendUserMessage(context.WindowIndex);
                context.SendButton.onClick.AddListener(context.SendAction);
            }

            if (context.CloseButton != null)
            {
                context.CloseAction = OnCloseBtnClick;
                context.CloseButton.onClick.AddListener(context.CloseAction);
            }

            if (context.InputField != null)
            {
                context.EndEditAction = _ =>
                {
                    if (currentWindowIndex != context.WindowIndex) return;
                    SendUserMessage(context.WindowIndex);
                };
                context.InputField.onEndEdit.AddListener(context.EndEditAction);
            }
        }

        private void UnbindSubWindowListeners()
        {
            for (int i = 0; i < contexts.Count; i++)
            {
                SubWindowContext context = contexts[i];
                if (context == null) continue;

                StopTyping(context, false);

                if (context.SendButton != null && context.SendAction != null)
                {
                    context.SendButton.onClick.RemoveListener(context.SendAction);
                }

                if (context.CloseButton != null && context.CloseAction != null)
                {
                    context.CloseButton.onClick.RemoveListener(context.CloseAction);
                }

                if (context.InputField != null && context.EndEditAction != null)
                {
                    context.InputField.onEndEdit.RemoveListener(context.EndEditAction);
                }
            }
        }

        private void SetupTabButton(GameObject buttonObject, AIDialogManager.NpcInfo info)
        {
            if (buttonObject == null) return;
            ButtonManager button = buttonObject.GetComponent<ButtonManager>();
            if (button == null)
            {
                button = buttonObject.GetComponentInChildren<ButtonManager>(true);
            }

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

        private static T FindComponentByName<T>(Transform root, string objectName) where T : Component
        {
            if (root == null || string.IsNullOrEmpty(objectName)) return null;

            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null) continue;
                if (component.gameObject.name == objectName)
                    return component;
            }

            return null;
        }

        private void SelectNpcTab(string npcId, bool refreshIfSame)
        {
            if (contexts.Count == 0) return;

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

            currentWindowIndex = targetIndex;
            currentNpcId = contexts[targetIndex].NpcId;
            AIDialogManager.Instance?.SetCurrentNpc(currentNpcId);

            if (refreshIfSame)
            {
                RefreshSubWindow(contexts[targetIndex]);
            }
        }

        private int FindContextIndexByNpcId(string npcId)
        {
            string normalizedNpcId = NormalizeNpcId(npcId);
            for (int i = 0; i < contexts.Count; i++)
            {
                if (string.Equals(contexts[i].NpcId, normalizedNpcId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string BuildWindowName(AIDialogManager.NpcInfo info, int index)
        {
            string npcId = string.IsNullOrWhiteSpace(info?.NpcId) ? "npc" : info.NpcId.Trim();
            return $"{index}_{npcId}";
        }

        private void OnWindowChanged(int index)
        {
            if (contexts.Count == 0) return;

            currentWindowIndex = Mathf.Clamp(index, 0, contexts.Count - 1);
            SubWindowContext context = contexts[currentWindowIndex];
            currentNpcId = context.NpcId;

            AIDialogManager.Instance?.SetCurrentNpc(currentNpcId);
            RefreshSubWindow(context);
        }

        private void OnCloseBtnClick()
        {
            UISystem.Close<UI_DialogWindow>();
        }

        private void SendUserMessage(int windowIndex)
        {
            if (!TryGetContext(windowIndex, out SubWindowContext context)) return;
            if (AIDialogManager.Instance == null)
            {
                RayDebug.Error("场景中没有找到 AIDialogManager 实例！请将其挂载到场景中。");
                return;
            }

            string userMessage = context.InputField != null ? context.InputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(userMessage)) return;

            StopTyping(context, true);

            if (context.AiResponseText != null) context.AiResponseText.text = "思考中...";

            currentWindowIndex = context.WindowIndex;
            currentNpcId = context.NpcId;
            AIDialogManager.Instance.SetCurrentNpc(currentNpcId);

            StartCoroutine(AIDialogManager.Instance.SendMessageToNpc(context.NpcId, userMessage,
                response =>
                {
                    if (context.AiResponseText != null) context.AiResponseText.text = "";
                    RefreshSubWindow(context, true, response);
                },
                error =>
                {
                    if (context.AiResponseText != null) context.AiResponseText.text = $"错误：{error}";
                    RefreshSubWindow(context);
                }));

            if (context.InputField != null) context.InputField.text = "";
            RefreshSubWindow(context);
        }

        private bool TryGetContext(int windowIndex, out SubWindowContext context)
        {
            for (int i = 0; i < contexts.Count; i++)
            {
                if (contexts[i].WindowIndex == windowIndex)
                {
                    context = contexts[i];
                    return true;
                }
            }

            context = null;
            return false;
        }

        private void RefreshSubWindow(SubWindowContext context, bool playTypingForLatestAi = false, string latestAiMessage = null)
        {
            if (context == null) return;
            if (AIDialogManager.Instance == null) return;

            if (context.TitleText != null)
            {
                context.TitleText.text = AIDialogManager.Instance.GetNpcDisplayName(context.NpcId);
            }

            if (context.ChatList == null || context.ChatList.itemParent == null) return;

            StopTyping(context, true);

            var list = AIDialogManager.Instance.GetMessageHistory(context.NpcId);
            string npcName = AIDialogManager.Instance.GetNpcDisplayName(context.NpcId);

            for (int i = context.ChatList.itemParent.childCount - 1; i >= 0; i--)
            {
                var child = context.ChatList.itemParent.GetChild(i);
                child.gameObject.GameObjectPushPool();
            }

            int typingIndex = FindLatestAssistantIndex(list, latestAiMessage, playTypingForLatestAi);
            TextMeshProUGUI typingTargetText = null;
            string typingTargetContent = null;

            for (int i = 0; i < list.Count; i++)
            {
                var msg = list[i];
                if (msg == null) continue;
                if (msg.role == "system") continue;

                var go = ProjectUtility.GetOrInstantiateGameObject(context.ChatList.itemPreset, context.ChatList.itemParent);
                go.SetActive(true);
                go.transform.SetAsLastSibling();

                var item = go.GetComponent<ListViewItem>();
                if (item == null) continue;

                bool isUser = string.Equals(msg.role, "user", StringComparison.OrdinalIgnoreCase);
                string content = BuildMessageContent(msg, npcName, isUser);

                ConfigureBubbleItem(item, isUser, content, out var textComp);

                if (playTypingForLatestAi && i == typingIndex && !isUser && textComp != null)
                {
                    typingTargetText = textComp;
                    typingTargetContent = content;
                }
            }

            if (context.ChatList.itemParent is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            if (typingTargetText != null && !string.IsNullOrEmpty(typingTargetContent))
            {
                StartTyping(context, typingTargetText, typingTargetContent);
            }
        }

        private string BuildMessageContent(AIDialogManager.ChatMessage msg, string npcName, bool isUser)
        {
            string body = msg?.content ?? string.Empty;
            if (!showSpeakerName) return body;

            string speaker = isUser ? "玩家" : npcName;
            return $"<b>{speaker}</b>\n{body}";
        }

        private void ConfigureBubbleItem(ListViewItem item, bool isUser, string content, out TextMeshProUGUI textComponent)
        {
            textComponent = null;
            if (item == null || item.row0 == null) return;

            item.rowCount = ListView.RowCount.One;
            item.row0Ref = new ListView.ListRow
            {
                rowType = ListView.RowType.Text,
                rowText = content
            };
            item.PassReferences();

            ListViewRow row = item.row0;
            if (row == null || row.textObject == null || row.layoutElement == null) return;
            row.gameObject.SetActive(true);

            textComponent = row.textObject;
            textComponent.gameObject.SetActive(true);
            textComponent.text = content;
            textComponent.enableAutoSizing = false;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.richText = true;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.maxVisibleCharacters = int.MaxValue;
            textComponent.color = isUser ? userTextColor : assistantTextColor;
            textComponent.alignment = isUser ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            if (row.iconImage != null)
            {
                row.iconImage.gameObject.SetActive(false);
            }

            RectTransform textRt = textComponent.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(bubbleHorizontalPadding, bubbleVerticalPadding);
            textRt.offsetMax = new Vector2(-bubbleHorizontalPadding, -bubbleVerticalPadding);

            Image bubbleImage = row.GetComponent<Image>();
            if (bubbleImage == null)
            {
                bubbleImage = row.gameObject.AddComponent<Image>();
            }
            bubbleImage.raycastTarget = false;

            Sprite bubbleSprite = bubbleSpriteOverride;
            Image rootImage = item.GetComponent<Image>();
            if (bubbleSprite == null && rootImage != null)
            {
                bubbleSprite = rootImage.sprite;
            }
            if (rootImage != null)
            {
                rootImage.enabled = false;
                rootImage.raycastTarget = false;
            }
            bubbleImage.enabled = true;
            bubbleImage.sprite = bubbleSprite;
            bubbleImage.type = bubbleImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bubbleImage.pixelsPerUnitMultiplier = bubblePixelsPerUnitMultiplier;
            bubbleImage.color = isUser ? userBubbleColor : assistantBubbleColor;

            RectTransform bubbleRt = row.GetComponent<RectTransform>();
            if (bubbleRt != null)
            {
                bubbleRt.pivot = isUser ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            }

            float maxTextWidth = Mathf.Max(20f, bubbleMaxWidth - bubbleHorizontalPadding * 2f);
            Vector2 preferred = textComponent.GetPreferredValues(content, maxTextWidth, 0f);

            float bubbleWidth = Mathf.Clamp(preferred.x + bubbleHorizontalPadding * 2f, bubbleMinWidth, bubbleMaxWidth);
            float bubbleHeight = Mathf.Max(preferred.y + bubbleVerticalPadding * 2f, textComponent.fontSize + bubbleVerticalPadding * 2f);

            row.layoutElement.minWidth = -1f;
            row.layoutElement.flexibleWidth = -1f;
            row.layoutElement.preferredWidth = bubbleWidth;
            row.layoutElement.minHeight = -1f;
            row.layoutElement.flexibleHeight = -1f;
            row.layoutElement.preferredHeight = bubbleHeight;

            HorizontalLayoutGroup rootLayout = item.GetComponent<HorizontalLayoutGroup>();
            if (rootLayout != null)
            {
                rootLayout.childAlignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                int leftPad = Mathf.RoundToInt(isUser ? sideIndent : messageOuterPadding);
                int rightPad = Mathf.RoundToInt(isUser ? messageOuterPadding : sideIndent);
                int verticalPad = Mathf.RoundToInt(messageOuterPadding * 0.5f);
                rootLayout.padding = new RectOffset(leftPad, rightPad, verticalPad, verticalPad);
                rootLayout.spacing = 0;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = false;
                rootLayout.childForceExpandHeight = false;
            }

            RectTransform itemRt = item.GetComponent<RectTransform>();
            if (itemRt != null)
            {
                Vector2 size = itemRt.sizeDelta;
                size.y = bubbleHeight + messageOuterPadding;
                itemRt.sizeDelta = size;
            }

            LayoutElement itemLayout = item.GetComponent<LayoutElement>();
            if (itemLayout == null)
            {
                itemLayout = item.gameObject.AddComponent<LayoutElement>();
            }
            itemLayout.minHeight = bubbleHeight + messageOuterPadding;
            itemLayout.preferredHeight = bubbleHeight + messageOuterPadding;
            itemLayout.flexibleHeight = 0f;
        }

        private int FindLatestAssistantIndex(List<AIDialogManager.ChatMessage> list, string latestAiMessage, bool enableTyping)
        {
            if (!enableTyping || list == null || list.Count == 0) return -1;

            string target = latestAiMessage?.Trim();
            if (!string.IsNullOrEmpty(target))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var msg = list[i];
                    if (msg == null) continue;
                    if (!string.Equals(msg.role, "assistant", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals((msg.content ?? string.Empty).Trim(), target, StringComparison.Ordinal)) return i;
                }
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var msg = list[i];
                if (msg == null) continue;
                if (string.Equals(msg.role, "assistant", StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }

        private void StartTyping(SubWindowContext context, TextMeshProUGUI textComponent, string fullText)
        {
            StopTyping(context, true);
            if (context == null || textComponent == null || string.IsNullOrEmpty(fullText)) return;

            context.TypingCoroutine = StartCoroutine(TypewriterRoutine(context, textComponent, fullText));
        }

        private IEnumerator TypewriterRoutine(SubWindowContext context, TextMeshProUGUI textComponent, string fullText)
        {
            context.IsTyping = true;
            context.TypingText = textComponent;
            context.TypingFullText = fullText;

            textComponent.text = fullText;
            textComponent.maxVisibleCharacters = 0;
            textComponent.ForceMeshUpdate();

            int totalChars = textComponent.textInfo.characterCount;
            if (totalChars <= 0)
            {
                CompleteTyping(context);
                yield break;
            }

            float interval = 1f / Mathf.Max(1f, typingCharsPerSecond);
            float elapsed = 0f;
            int visibleChars = 0;
            float startTime = Time.unscaledTime;

            while (visibleChars < totalChars)
            {
                if (clickToSkipTyping && Time.unscaledTime - startTime >= typingSkipInputDelay && IsSkipInputTriggered())
                {
                    visibleChars = totalChars;
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                while (elapsed >= interval && visibleChars < totalChars)
                {
                    elapsed -= interval;
                    visibleChars++;
                }

                textComponent.maxVisibleCharacters = visibleChars;
                yield return null;
            }

            textComponent.maxVisibleCharacters = int.MaxValue;
            CompleteTyping(context);
        }

        private void CompleteTyping(SubWindowContext context)
        {
            if (context == null) return;
            context.TypingCoroutine = null;
            context.IsTyping = false;
            context.TypingText = null;
            context.TypingFullText = null;
        }

        private void StopTyping(SubWindowContext context, bool revealFullText)
        {
            if (context == null) return;

            if (context.TypingCoroutine != null)
            {
                StopCoroutine(context.TypingCoroutine);
                context.TypingCoroutine = null;
            }

            if (revealFullText && context.TypingText != null)
            {
                if (!string.IsNullOrEmpty(context.TypingFullText))
                {
                    context.TypingText.text = context.TypingFullText;
                }
                context.TypingText.maxVisibleCharacters = int.MaxValue;
            }

            context.IsTyping = false;
            context.TypingText = null;
            context.TypingFullText = null;
        }

        private void StopAllTyping(bool revealFullText)
        {
            for (int i = 0; i < contexts.Count; i++)
            {
                StopTyping(contexts[i], revealFullText);
            }
        }

        private static bool IsSkipInputTriggered()
        {
            if (Input.GetMouseButtonDown(0)) return true;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Began)
                        return true;
                }
            }

            return false;
        }

        private string NormalizeNpcId(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return string.IsNullOrWhiteSpace(defaultNpcId) ? "tavern_default" : defaultNpcId.Trim();
            return npcId.Trim();
        }
    }
}
