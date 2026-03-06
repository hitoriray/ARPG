using JKFrame;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.AI;

namespace Npc
{
    /// <summary>
    /// NPC 控制器基类，支持：
    ///   - 固定位置 NPC（无 NavMeshAgent，如商店老板）
    ///   - 可移动 NPC（挂载 NavMeshAgent）
    /// 对接 Pixel Crushers Dialogue System 插件
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class NpcController : CharacterControllerBase, IStateMachineOwner
    {
        // ─────────────────────────────────────────────
        // 移动（可选）
        // ─────────────────────────────────────────────

        [Header("移动（固定 NPC 可不挂 NavMeshAgent）")]
        public NavMeshAgent navMeshAgent;

        /// <summary>是否拥有可移动能力</summary>
        public bool CanMove => navMeshAgent != null;

        // ─────────────────────────────────────────────
        // 对话配置
        // ─────────────────────────────────────────────

        [Header("对话配置")]
        [Tooltip("Dialogue Database 中的对话标题，留空则不触发对话")]
        [SerializeField] private string conversationTitle = "";

        [Tooltip("CharacterTable 中的 CharacterId，用于映射到表中的角色名称\n不填则降级使用 conversationTitle 或 gameObject.name")]
        [SerializeField] private int characterId = -1;

        [Tooltip("交互检测范围（米）。需要 NPC 身上有 isTrigger=true 的 Collider")]
        [SerializeField] private float interactRange = 2.5f;

        [Tooltip("对话开始时自动面朝玩家")]
        [SerializeField] private bool facePlayerOnConversation = true;

        [Tooltip("检测玩家的 Tag")]
        [SerializeField] private string playerTag = "Player";

        // 运行时解析后的显示名称
        private string _displayName;

        // ─────────────────────────────────────────────
        // 状态机
        // ─────────────────────────────────────────────

        public StateMachine stateMachine;

        // ─────────────────────────────────────────────
        // 运行时状态
        // ─────────────────────────────────────────────

        /// <summary>玩家是否在交互范围内</summary>
        public bool PlayerInRange { get; private set; }

        /// <summary>当前是否正在对话</summary>
        public bool IsInConversation { get; private set; }

        /// <summary>NPC 在交互列表中显示的名称</summary>
        private string NpcDisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayName))
                    return _displayName;
                if (!string.IsNullOrEmpty(conversationTitle))
                    return conversationTitle;
                return gameObject.name;
            }
        }

        // ── 静态列表：追踪当前所有在范围内的 NPC（最近进入的排首位）
        private static readonly System.Collections.Generic.List<NpcController> _nearbyNpcs = new();

        private Transform _playerTransform;
        private bool _lastInteractive;
        private bool _isFacingPlayer;

        /// <summary>
        /// 动画相关字段
        /// </summary>
        private int _previewLayer = -1;
        private int _currentPreviewIdleIndex;
        private Animancer.ManualMixerState _previewStandMixer;

        // ─────────────────────────────────────────────
        // 生命周期
        // ─────────────────────────────────────────────

        protected override void Start()
        {
            base.Start();

            ResolveDisplayName();

            stateMachine = new StateMachine();
            stateMachine.Init(this);

            // 注册对话事件（全局广播方式，C# event 委托）
            if (DialogueManager.instance != null)
            {
                DialogueManager.instance.conversationStarted += OnConversationStarted;
                DialogueManager.instance.conversationEnded += OnConversationEnded;
            }

            PlayIdleAnimation();
        }

        /// <summary>从 CharacterTable 根据 characterId 解析显示名</summary>
        private void ResolveDisplayName()
        {
            if (characterId < 0) return;

            var table = ResSystem.LoadAsset<Config.CharacterTable>("CharacterTable");
            if (table == null) return;

            var entry = table.GetCharacterById(characterId);
            if (entry != null && !string.IsNullOrEmpty(entry.CharacterName))
                _displayName = entry.CharacterName;
        }

        private void OnDestroy()
        {
            if (DialogueManager.instance != null)
            {
                DialogueManager.instance.conversationStarted -= OnConversationStarted;
                DialogueManager.instance.conversationEnded -= OnConversationEnded;
            }

            // 确保销毁时从列表移除
            if (_nearbyNpcs.Remove(this))
                BroadcastInteractList();
        }

        protected override void Update()
        {
            base.Update();
            HandleInteractInput();
            HandleFacePlayer();
        }

        // ─────────────────────────────────────────────
        // 交互输入
        // ─────────────────────────────────────────────

        private void HandleInteractInput()
        {
            if (!PlayerInRange || IsInConversation) return;

            bool interactive = InputService.Instance != null && InputService.Instance.Interactive;
            if (interactive && !_lastInteractive)
            {
                TryStartConversation();
            }
            _lastInteractive = interactive;
        }

        // ─────────────────────────────────────────────
        // 对话启动
        // ─────────────────────────────────────────────

        /// <summary>
        /// 尝试启动对话，可在外部（如 DialogueSystemTrigger 的 OnUse 消息）直接调用
        /// </summary>
        public void TryStartConversation()
        {
            if (string.IsNullOrWhiteSpace(conversationTitle))
            {
                Debug.LogWarning($"[NpcController] {gameObject.name}：conversationTitle 未配置，无法触发对话", this);
                return;
            }

            if (DialogueManager.instance == null)
            {
                Debug.LogWarning("[NpcController] 场景中没有找到 DialogueManager，请确保已添加 Dialogue System Controller 组件", this);
                return;
            }

            if (DialogueManager.IsConversationActive)
                return;

            // 以 NPC 自身为 Conversant，玩家为 Actor
            Transform actor = _playerTransform != null ? _playerTransform : null;
            DialogueManager.StartConversation(conversationTitle, actor, transform);
        }

        /// <summary>
        /// 接收插件 Selector / Usable 发来的 OnUse 消息（备用交互路径）
        /// </summary>
        public void OnUse(Transform user)
        {
            _playerTransform = user;
            TryStartConversation();
        }

        // ─────────────────────────────────────────────
        // 对话事件回调
        // ─────────────────────────────────────────────

        private void OnConversationStarted(Transform actor)
        {
            // 只处理以本 NPC 为 Conversant 发起的对话
            if (actor == null) return;
            var conversant = DialogueManager.currentConversant;
            if (conversant == null || conversant.gameObject != gameObject) return;

            IsInConversation = true;
            _isFacingPlayer = facePlayerOnConversation;
            _playerTransform = actor;

            // 对话中从交互列表移除（不再显示提示）
            BroadcastInteractList();

            // 停止移动
            if (CanMove)
                navMeshAgent.isStopped = true;
        }

        private void OnConversationEnded(Transform actor)
        {
            if (!IsInConversation) return;
            IsInConversation = false;
            _isFacingPlayer = false;

            // 对话结束，如果玩家仍在范围内则恢复提示
            BroadcastInteractList();

            // 恢复移动
            if (CanMove)
                navMeshAgent.isStopped = false;
        }

        // ─────────────────────────────────────────────
        // NPC 面朝玩家
        // ─────────────────────────────────────────────

        private void HandleFacePlayer()
        {
            if (!_isFacingPlayer || _playerTransform == null) return;

            Vector3 dir = _playerTransform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * RotateSpeed);
        }

        // ─────────────────────────────────────────────
        // 玩家范围检测（通过 Trigger Collider）
        // ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            PlayerInRange = true;
            _playerTransform = other.transform;
            _lastInteractive = false;

            // 插入到列表最前（最近进入的 NPC 作为主焦点）
            if (!_nearbyNpcs.Contains(this))
                _nearbyNpcs.Insert(0, this);
            BroadcastInteractList();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            PlayerInRange = false;
            _lastInteractive = false;

            _nearbyNpcs.Remove(this);
            BroadcastInteractList();
        }

        // ─────────────────────────────────────────────
        // 交互列表广播
        // ─────────────────────────────────────────────

        /// <summary>
        /// 将当前所有在范围内（且未在对话中）的 NPC 名字同步到主 HUD 的交互列表
        /// </summary>
        private static void BroadcastInteractList()
        {
            var names = new System.Collections.Generic.List<string>();
            int selectedIndex = 0;

            for (int i = 0; i < _nearbyNpcs.Count; i++)
            {
                var npc = _nearbyNpcs[i];
                if (npc == null || npc.IsInConversation) continue;
                names.Add(npc.NpcDisplayName);
            }

            EventSystem.EventTrigger("UpdateInteractList", names, selectedIndex);
        }

        // ─────────────────────────────────────────────
        // 动画
        // ─────────────────────────────────────────────

        public void PlayAnimation(string animationName)
        {
            animator.CrossFadeInFixedTime(animationName, 0.25f);
        }

        /// <summary>
        /// 自动播放 Idle 动画；需要在 Inspector 配置 CharacterConfig（包含 PlayerSO）
        /// </summary>
        public void PlayIdleAnimation()
        {
            if (playerSO == null) return;
            var idleClip = playerSO.playerMovementData?.PlayerIdleData?.idle;
            if (idleClip == null) return;
            
            var state = animancer.Play(idleClip);

            // ── 第一层：根混合器 ──────────────────────────────────────
            // 对应游戏内 lockValueParameter = 0（非锁敌）
            // 若根节点是 MixerState<float>（LinearMixerState），将参数拨到 0
            if (state is Animancer.MixerState<float> rootMixer)
                rootMixer.Parameter = 0f;

            // 强制熄灭锁敌分支（Child 1），只保留非锁敌分支（Child 0）
            var lockBranch = state.GetChild(1);
            if (lockBranch != null) { lockBranch.SetWeight(0f); lockBranch.Stop(); }

            var nonLockBranch = state.GetChild(0);
            if (nonLockBranch != null)
            {
                nonLockBranch.SetWeight(1f);

                // ── 第二层：站立/蹲伏混合器 ───────────────────────────
                // 对应游戏内 standValueParameter = 1（站立）
                if (nonLockBranch is Animancer.MixerState<float> standMixer)
                    standMixer.Parameter = 1f;

                // 强制熄灭蹲伏分支（Child 0），只保留站立分支（Child 1）
                var crouchBranch = nonLockBranch.GetChild(0);
                if (crouchBranch != null) { crouchBranch.SetWeight(0f); crouchBranch.Stop(); }
            }

            // ── 第三层：站立Idle ManualMixerState ────────────────────
            // 树结构：root.Child(0).Child(1) = standIdleMixerState
            _previewStandMixer = nonLockBranch?.GetChild(1) as Animancer.ManualMixerState;
            if (_previewStandMixer != null && _previewStandMixer.ChildCount > 0)
            {
                _currentPreviewIdleIndex = -1;
                PlayNextPreviewIdle();
            }
        }
        
        /// <summary>
        /// 循环播放预览 Stand Idle 动画（首次调用播放 index 0，结束后自动切下一个）
        /// 与游戏内 PlayerReusableLogic.PlayNextState 逻辑完全对应
        /// </summary>
        private void PlayNextPreviewIdle()
        {
            if (_previewStandMixer == null || _previewStandMixer.ChildCount == 0) return;

            _currentPreviewIdleIndex = (_currentPreviewIdleIndex + 1) % _previewStandMixer.ChildCount;

            for (int i = 0; i < _previewStandMixer.ChildCount; i++)
            {
                var child = _previewStandMixer.GetChild(i);
                if (i == _currentPreviewIdleIndex)
                {
                    child.SetWeight(1f);
                    child.Play();
                    child.Events(this).OnEnd = PlayNextPreviewIdle;
                }
                else
                {
                    child.SetWeight(0f);
                    child.Stop();
                }
            }
        }

        // ─────────────────────────────────────────────
        // NavMesh 封装（固定 NPC 调用这些方法会安全跳过）
        // ─────────────────────────────────────────────

        public void StartMove()
        {
            if (!CanMove) return;
            navMeshAgent.enabled = true;
        }

        public void StopMove()
        {
            if (!CanMove) return;
            navMeshAgent.enabled = false;
        }

        public void SetDestination(Vector3 pos)
        {
            if (!CanMove) return;
            navMeshAgent.SetDestination(pos);
        }
    }
}