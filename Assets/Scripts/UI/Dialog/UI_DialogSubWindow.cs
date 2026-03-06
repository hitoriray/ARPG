using System;
using System.Collections;
using System.Collections.Generic;
using JKFrame;
using Manager;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class UI_DialogSubWindow : MonoBehaviour
    {
        private const string WindowManagerContentName = "Content";

        [Header("References")]
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

        public string NpcId { get; private set; }
        public int WindowIndex { get; private set; }

        private Coroutine typingCoroutine;
        private Coroutine scrollRoutine;
        private TextMeshProUGUI typingText;
        private string typingFullText;
        private bool isTyping;
        private bool skipTypingRequested;
        private ScrollRect scrollRect;

        private bool needRefreshOnEnable;
        private bool pendingPlayTyping;
        private string pendingAiMessage;

        public Action<int> OnSendRequested;
        public Action OnCloseRequested;

        public GameObject FirstSelected => inputField != null ? inputField.gameObject : null;

        public void Init(int index, string npcId)
        {
            EnsureWindowManagerAnimationTargets();

            WindowIndex = index;
            NpcId = npcId;

            if (chatList != null)
            {
                scrollRect = chatList.GetComponentInParent<ScrollRect>(true);
                if (scrollRect == null)
                    scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (sendButton != null) sendButton.onClick.AddListener(OnSendBtnClick);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseBtnClick);
            if (inputField != null) inputField.onEndEdit.AddListener(OnInputEndEdit);

            EventTrigger trigger = GetComponent<EventTrigger>();
            if (trigger == null) trigger = gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener((data) => { SkipCurrentTyping(); });
            trigger.triggers.Add(entry);
            
            if (titleText != null)
            {
                titleText.text = AIDialogManager.Instance?.GetNpcDisplayName(NpcId);
            }
        }

        private void EnsureWindowManagerAnimationTargets()
        {
            if (!TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.ignoreParentGroups = false;
            }

            if (transform.Find(WindowManagerContentName) != null)
                return;

            var existingChildren = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                existingChildren.Add(transform.GetChild(i));
            }

            var contentGo = new GameObject(WindowManagerContentName, typeof(RectTransform));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(transform, false);
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            contentRt.pivot = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < existingChildren.Count; i++)
            {
                existingChildren[i].SetParent(contentRt, false);
            }
        }

        private void OnDestroy()
        {
            StopTyping(false);
            if (sendButton != null) sendButton.onClick.RemoveListener(OnSendBtnClick);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseBtnClick);
            if (inputField != null) inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        private void OnEnable()
        {
            if (needRefreshOnEnable)
            {
                Refresh(pendingPlayTyping, pendingAiMessage);
            }
        }

        public void SkipCurrentTyping()
        {
            skipTypingRequested = true;
        }

        private void OnSendBtnClick()
        {
            OnSendRequested?.Invoke(WindowIndex);
        }

        private void OnCloseBtnClick()
        {
            OnCloseRequested?.Invoke();
        }

        private void OnInputEndEdit(string text)
        {
            if (gameObject.activeInHierarchy)
            {
                OnSendRequested?.Invoke(WindowIndex);
            }
        }

        public string GetInputTextAndClear()
        {
            if (inputField == null) return string.Empty;
            string text = inputField.text.Trim();
            inputField.text = "";
            return text;
        }

        public void SetAiResponseStatus(string statusText)
        {
            if (aiResponseText != null)
            {
                aiResponseText.text = statusText;
            }
        }

        public void Refresh(bool playTypingForLatestAi = false, string latestAiMessage = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                needRefreshOnEnable = true;
                pendingPlayTyping = playTypingForLatestAi;
                pendingAiMessage = latestAiMessage;
                return;
            }
            needRefreshOnEnable = false;

            if (AIDialogManager.Instance == null) return;
            if (chatList == null || chatList.itemParent == null) return;

            StopTyping(true);

            var list = AIDialogManager.Instance.GetMessageHistory(NpcId);
            string npcName = AIDialogManager.Instance.GetNpcDisplayName(NpcId);

            for (int i = chatList.itemParent.childCount - 1; i >= 0; i--)
            {
                var child = chatList.itemParent.GetChild(i);
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

                var go = ProjectUtility.GetOrInstantiateGameObject(chatList.itemPreset, chatList.itemParent);
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

            if (chatList.itemParent is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            if (typingTargetText != null && !string.IsNullOrEmpty(typingTargetContent))
            {
                StartTyping(typingTargetText, typingTargetContent);
            }

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
                scrollRoutine = null;
            }

            if (scrollRect != null)
            {
                scrollRoutine = StartCoroutine(ScrollToBottomRoutine(scrollRect));
            }
        }

        private IEnumerator ScrollToBottomRoutine(ScrollRect sr)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForEndOfFrame();
                if (sr == null) break;

                if (sr.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
                }
                Canvas.ForceUpdateCanvases();
                
                sr.velocity = Vector2.zero;
                sr.verticalNormalizedPosition = 0f;
            }

            while (isTyping && sr != null)
            {
                yield return new WaitForEndOfFrame();
                if (sr.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
                }
                sr.velocity = Vector2.zero;
                sr.verticalNormalizedPosition = 0f;
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

        private void StartTyping(TextMeshProUGUI textComponent, string fullText)
        {
            StopTyping(true);
            if (textComponent == null || string.IsNullOrEmpty(fullText)) return;

            typingCoroutine = StartCoroutine(TypewriterRoutine(textComponent, fullText));
        }

        private IEnumerator TypewriterRoutine(TextMeshProUGUI textComponent, string fullText)
        {
            isTyping = true;
            skipTypingRequested = false;
            typingText = textComponent;
            typingFullText = fullText;

            textComponent.text = fullText;
            textComponent.maxVisibleCharacters = 0;
            textComponent.ForceMeshUpdate();

            int totalChars = textComponent.textInfo.characterCount;
            if (totalChars <= 0)
            {
                CompleteTyping();
                yield break;
            }

            float interval = 1f / Mathf.Max(1f, typingCharsPerSecond);
            float elapsed = 0f;
            int visibleChars = 0;
            float startTime = Time.unscaledTime;

            while (visibleChars < totalChars)
            {
                if (clickToSkipTyping && Time.unscaledTime - startTime >= typingSkipInputDelay && skipTypingRequested)
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
            CompleteTyping();
        }

        private void CompleteTyping()
        {
            typingCoroutine = null;
            isTyping = false;
            typingText = null;
            typingFullText = null;
        }

        private void StopTyping(bool revealFullText)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (revealFullText && typingText != null)
            {
                if (!string.IsNullOrEmpty(typingFullText))
                {
                    typingText.text = typingFullText;
                }
                typingText.maxVisibleCharacters = int.MaxValue;
            }

            isTyping = false;
            typingText = null;
            typingFullText = null;
        }
    }
}
