using System;
using System.Collections;
using System.Collections.Generic;
using JKFrame;
using Manager;
using Michsky.MUIP;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CharacterType = PixelCrushers.DialogueSystem.CharacterType;

namespace UI
{
    /// <summary>
    /// NPC 剧情对话窗口，由 NpcDialogueUI 桥接脚本驱动。
    /// 支持：
    ///   - NPC / PC 台词显示（含角色名、头像、内容）
    ///   - 继续按钮（单条台词推进）
    ///   - 玩家分支选项按钮列表
    /// </summary>
    [UIWindowData(typeof(UI_ConversationWindow), true, nameof(UI_ConversationWindow), 1)]
    public class UI_ConversationWindow : UI_WindowBase
    {
        // ─── Inspector 引用 ──────────────────────────────────────────

        [Header("说话者信息")]
        [SerializeField] private Image    speakerPortrait;   // 说话者头像
        [SerializeField] private TMP_Text speakerName;       // 说话者名字

        [Header("台词区")]
        [SerializeField] private TMP_Text subtitleText;      // 台词文字
        [SerializeField] private Button   continueButton;    // 继续按钮
        [SerializeField] private GameObject subtitlePanel;   // 台词面板（含说话者信息 + 文字 + 继续按钮）

        [Header("选项区")]
        [SerializeField] private Transform responsesRoot;    // 选项按钮的父节点
        [SerializeField] private ButtonManager responseButtonPrefab; // 选项按钮预制体（Button + TMP_Text）
        [SerializeField] private GameObject responsesPanel;  // 选项面板（可与 subtitlePanel 切换显示）

        [Header("打字机")]
        [Tooltip("每秒显示字符数，0 = 不延迟直接显示")]
        [SerializeField] private float typewriterSpeed = 40f;

        // ─── 运行时 ─────────────────────────────────────────────────

        private NpcDialogueUI             _bridge;
        private Action<Response>          _onResponseSelected;
        private readonly List<ButtonManager> _responseButtons = new();

        private Coroutine _typewriterCoroutine;
        private string    _fullText = string.Empty;
        private bool      _typewriterRunning;

        // ─── 生命周期 ────────────────────────────────────────────────

        public override void Init()
        {
            base.Init();
            continueButton?.onClick.AddListener(OnContinueClicked);
        }

        public override void OnShow()
        {
            base.OnShow();
            UIModalStack.Push(ForceClose);

            // 打开时默认显示台词面板，隐藏选项面板
            subtitlePanel?.SetActive(true);
            responsesPanel?.SetActive(false);
            continueButton?.gameObject.SetActive(false);
        }

        public override void OnClose()
        {
            base.OnClose();
            UIModalStack.Remove(ForceClose);
            ClearResponseButtons();
        }

        // ─── 由 NpcDialogueUI 调用 ──────────────────────────────────

        /// <summary>存储桥接引用（Open 时由 NpcDialogueUI 传入）</summary>
        public void SetBridge(NpcDialogueUI bridge)
        {
            _bridge = bridge;
        }

        /// <summary>显示 NPC 或 PC 台词，启动打字机效果</summary>
        public void ShowSubtitle(Subtitle subtitle)
        {
            // 说话者名字
            if (speakerName != null)
                speakerName.text = subtitle.speakerInfo.Name;

            // 头像
            if (speakerPortrait != null)
            {
                Sprite portrait = subtitle.speakerInfo.portrait;
                speakerPortrait.sprite  = portrait;
                speakerPortrait.enabled = portrait != null;
            }

            // 切到台词面板，隐藏选项面板
            subtitlePanel?.SetActive(true);
            responsesPanel?.SetActive(false);

            // 启动打字机
            StartTypewriter(subtitle.formattedText.text);
        }

        /// <summary>显示继续按钮</summary>
        public void ShowContinueButton()
        {
            // 打字机运行中先不显示，打完后协程自己显示
            if (_typewriterRunning) return;
            continueButton?.gameObject.SetActive(true);
        }

        /// <summary>隐藏继续按钮</summary>
        public void HideContinueButton()
        {
            continueButton?.gameObject.SetActive(false);
        }

        /// <summary>显示玩家分支选项</summary>
        public void ShowResponses(Response[] responses, Action<Response> onSelected)
        {
            _onResponseSelected = onSelected;
            ClearResponseButtons();

            // subtitlePanel?.SetActive(false);
            responsesPanel?.SetActive(true);

            if (responses == null || responsesRoot == null || responseButtonPrefab == null)
                return;

            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                if (!response.enabled) continue;

                var btn = Instantiate(responseButtonPrefab, responsesRoot);
                btn.gameObject.SetActive(true);

                // 设置选项文字（子物体里找 TMP_Text）
                btn.SetText(response.formattedText.text);
                
                // 捕获 response，避免闭包引用最后一个 i
                var capturedResponse = response;
                btn.onClick.AddListener(() => OnResponseButtonClicked(capturedResponse));

                _responseButtons.Add(btn);
            }
        }

        /// <summary>隐藏选项列表</summary>
        public void HideResponses()
        {
            responsesPanel?.SetActive(false);
            ClearResponseButtons();
        }

        /// <summary>更新 PC（玩家）头像</summary>
        public void SetPCPortrait(Sprite sprite, string name)
        {
            // 如果你有单独的 PC 头像区，在这里更新；否则复用 speakerPortrait
        }

        /// <summary>更新任意角色的头像（仅在名字匹配时更新）</summary>
        public void SetActorPortrait(string actorName, Sprite sprite)
        {
            // 当 SetPortrait() sequencer 命令触发时插件会调用此方法
            // 如果当前说话者名字匹配则更新头像
            if (speakerName != null &&
                string.Equals(speakerName.text, actorName, StringComparison.OrdinalIgnoreCase) &&
                speakerPortrait != null)
            {
                speakerPortrait.sprite  = sprite;
                speakerPortrait.enabled = sprite != null;
            }
        }

        // ─── 内部按钮响应 ────────────────────────────────────────────

        private void OnContinueClicked()
        {
            if (_typewriterRunning)
            {
                SkipTypewriter();
                return;
            }

            continueButton?.gameObject.SetActive(false);
            _bridge?.OnContinue();
        }

        private void OnResponseButtonClicked(Response response)
        {
            ClearResponseButtons();
            responsesPanel?.SetActive(false);
            _onResponseSelected?.Invoke(response);
        }

        private void ForceClose()
        {
            // ESC 关闭对话（结束对话系统的当前对话）
            if (DialogueManager.IsConversationActive)
                DialogueManager.StopConversation();
            else
                UISystem.Close<UI_ConversationWindow>();
        }

        // ─── 打字机 ────────────────────────────────────────────────────

        private void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            if (subtitleText == null) return;
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _fullText          = text ?? string.Empty;
            _typewriterRunning = true;
            subtitleText.text  = string.Empty;
            continueButton?.gameObject.SetActive(true);  // 打字中就可点击（点了跳到全文）

            if (typewriterSpeed <= 0f)
            {
                subtitleText.text = _fullText;
            }
            else
            {
                float interval = 1f / typewriterSpeed;
                for (int i = 1; i <= _fullText.Length; i++)
                {
                    subtitleText.text = _fullText.Substring(0, i);
                    yield return new WaitForSeconds(interval);
                }
            }

            _typewriterRunning   = false;
            _typewriterCoroutine = null;
            // 按钮本来就是显示的，不需要再 SetActive(true)
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            _typewriterRunning = false;
            if (subtitleText != null) subtitleText.text = _fullText;
            continueButton?.gameObject.SetActive(true);
        }

        // ─── 工具 ────────────────────────────────────────────────────

        private void ClearResponseButtons()
        {
            foreach (var btn in _responseButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _responseButtons.Clear();
        }
    }
}
