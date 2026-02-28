using System;
using Animancer;
using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS;
using Battle.ECS.Core.Helper;
using Config;
using GOAP;
using Manager;
using UnityEngine;

namespace Boss
{
    public class BossController : CharacterControllerBase, ICharacter
    {
        [Header("Config")]
        
        [SerializeField] private CharacterConfig characterConfig;
        [SerializeField] private PlayerSO playerSO;

        [Header("View")]
        [SerializeField] private Transform modelTransform;

        [Header("Combat")]
        [SerializeField] private BossSkillBrainBase skillBrain;

        [SerializeField] private CharacterAttribute characterAttribute;
        [SerializeField] private WeaponSlotManager weaponSlotManager;

        [Header("Animancer Skill Layer")]
        [SerializeField] private int skillLayerIndex = 1;
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeOut = 0.1f;

        public BossAIContext AI { get; } = new BossAIContext();
        public Entity BossEntity { get; private set; }

        // 受击方向（世界空间）
        public Vector3 LastHitDirection { get; set; }

        public BossStateMachine MovementStateMachine { get; private set; }
        public PlayerReusableData ReusableData { get; private set; }

        public AnimancerComponent Animancer => animancer;
        public AnimancerLayer SkillLayer => animancer.Layers[skillLayerIndex];
        public Transform ModelTransform => modelTransform != null ? modelTransform : transform;
        public CharacterAttribute CharacterAttribute => characterAttribute;
        public CharacterConfig CharacterConfig => characterConfig;
        public PlayerSO PlayerSO => playerSO;

        public float WalkSpeed => characterConfig != null ? characterConfig.WalkSpeed : 0f;
        public float RunSpeed => characterConfig != null ? characterConfig.RunSpeed : 0f;
        public float RotateSpeed => characterConfig != null ? characterConfig.RotateSpeed : 8f;
        public bool IsPlayerControlled => false;
        
        private bool inSkill;
        private bool currentSkillUpperBody;
        private bool initialized;

        protected override void Awake()
        {
            base.Awake();
            if (modelTransform == null)
                modelTransform = transform;
        }

        private void Start()
        {
            if (!initialized && characterConfig != null)
                Init(characterConfig);
        }

        public void Init(CharacterConfig config)
        {
            initialized = true;
            characterConfig = config;
            if (playerSO == null && characterConfig != null)
                playerSO = characterConfig.PlayerSO;

            if (characterConfig != null)
            {
                ApplyControllerProfile(characterConfig.ControllerProfile);
                if (characterConfig.Avatar != null)
                    animator.avatar = characterConfig.Avatar;
            }

            if (CharacterAttribute != null && characterConfig != null)
                CharacterAttribute.Init(characterConfig, characterConfig.hpBaseValue, characterConfig.mpBaseValue);

            ReusableData = new PlayerReusableData(animancer, playerSO);
            ReusableData.speedValueParameter.TargetValue = 0f;
            ReusableData.lockValueParameter.TargetValue = 0f;

            MovementStateMachine = new BossStateMachine(this);
            MovementStateMachine.ChangeState(MovementStateMachine.idleState);

            SetupAnimancerLayers();

            if (skillBrain != null && characterConfig != null)
                skillBrain.Init(this, characterConfig.SkillConfigList);

            weaponSlotManager?.RefreshSlots();

            // 注册 ECS 实体
            if (BattleEcsRunner.Instance != null && BattleEcsRunner.Instance.Context != null)
            {
                BossEntity = BattleEcsRunner.Instance.RegisterCharacter(this);
                RayDebug.Log($"{gameObject.name} ECS实体已创建: Entity ID = {BossEntity.Id}");
            }
        }

        private void SetupAnimancerLayers()
        {
            var layer = animancer.Layers[skillLayerIndex];
            layer.SetWeight(0f);
            layer.IsAdditive = false;
        }

        protected override void Update()
        {
            base.Update();

            MovementStateMachine?.OnUpdate();
            MovementStateMachine?.TickAI();

            CleanupFinishedSkillLayer();
        }

        protected override void OnAnimatorMove()
        {
            base.OnAnimatorMove();
            MovementStateMachine?.OnAnimationUpdate();
        }

        public void AnimationEnd()
        {
            MovementStateMachine?.OnAnimationEnd();
        }

        public bool IsInSkill => MovementStateMachine?.currentState == MovementStateMachine?.skillState;

        public void SetTarget(Transform target)
        {
            AI.Target = target;
        }

        public void SetEvasionDir(Vector3 worldDir)
        {
            AI.SetEvasionDir(worldDir);
        }

        public void SetDesiredMove(Vector3 worldDir, float moveSpeedMultiplier = 1f, float moveSpeedParam = 1f)
        {
            AI.SetMove(worldDir, moveSpeedMultiplier, moveSpeedParam);
            if (moveSpeedMultiplier > 0f)
                this.moveSpeedMultiplier = moveSpeedMultiplier;
        }

        public void ClearDesiredMove()
        {
            AI.ClearMove();
        }

        public bool TryStartSkill(int skillIndex)
        {
            if (skillBrain == null)
                return false;

            if (!skillBrain.CheckReleaseSkill(skillIndex))
                return false;

            if (MovementStateMachine != null && MovementStateMachine.currentState != MovementStateMachine.skillState)
                MovementStateMachine.ChangeState(MovementStateMachine.skillState);

            skillBrain.ReleaseSkill(skillIndex);
            return true;
        }

        public bool TryStartEvasion(BossEvasionType type)
        {
            if (MovementStateMachine == null || playerSO == null || ReusableData == null)
                return false;

            float cd = type switch
            {
                BossEvasionType.Avoid => playerSO.playerMovementData.PlayerAvoidData.cooldown,
                BossEvasionType.Slide => playerSO.playerMovementData.PlayerSlideData.cooldown,
                BossEvasionType.Roll => playerSO.playerMovementData.PlayerRollData.cooldown,
                _ => 0.3f
            };

            if (Time.time - ReusableData.lastEvasiveActionTime < cd)
                return false;

            switch (type)
            {
                case BossEvasionType.Avoid:
                    MovementStateMachine.ChangeState(MovementStateMachine.avoidState);
                    break;
                case BossEvasionType.Slide:
                    MovementStateMachine.ChangeState(MovementStateMachine.slideState);
                    break;
                case BossEvasionType.Roll:
                    MovementStateMachine.ChangeState(MovementStateMachine.rollState);
                    break;
            }

            return true;
        }

        public void Change2IdleState()
        {
            if (MovementStateMachine == null)
                return;

            if (MovementStateMachine.currentState == MovementStateMachine.skillState)
            {
                MovementStateMachine.skillState.NotifySkillEnd();
                return;
            }

            ExitSkillMode();
        }

        public void EnterSkillMode(bool upperBody)
        {
            if (inSkill && currentSkillUpperBody == upperBody)
                return;

            inSkill = true;
            currentSkillUpperBody = upperBody;

            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            skillLayer.IsAdditive = false;
            skillLayer.Mask = upperBody ? upperBodyMask : null;

            baseLayer.Stop();
            skillLayer.SetWeight(1f);
            baseLayer.SetWeight(upperBody ? 1f : 0f);
        }

        public void ExitSkillMode()
        {
            if (!inSkill)
                return;

            inSkill = false;
            currentSkillUpperBody = false;
            ClearSkillRootMotion();

            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            var skillState = skillLayer.CurrentState;
            if (skillState != null)
                skillState.IsPlaying = false;

            skillLayer.StartFade(0f, skillLayerFadeOut);
            baseLayer.StartFade(1f, skillLayerFadeOut);
        }

        private void CleanupFinishedSkillLayer()
        {
            if (inSkill)
                return;

            var skillLayer = animancer.Layers[skillLayerIndex];
            if (skillLayer.Weight > 0f || skillLayer.CurrentState == null)
                return;

            skillLayer.Stop();
        }

        public void SetSkillRootMotion(Action<Vector3, Quaternion> handler, bool applyRootMotion)
        {
            if (applyRootMotion && handler != null)
                SetRootMotionMode(RootMotionMode.Custom, handler);
            else
                SetRootMotionMode(RootMotionMode.Suppressed, null);
        }

        public void ClearSkillRootMotion()
        {
            SetRootMotionMode(RootMotionMode.Default, null);
        }

        public void OnSkillMove(Vector3 deltaPos)
        {
            controller.Move(deltaPos);
        }

        public void OnSkillRotate(Quaternion deltaRot)
        {
            transform.rotation = deltaRot * transform.rotation;
        }

        public void OnSkillRotate()
        {
            if (AI.Target == null)
                return;

            Vector3 dir = AI.Target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;

            float speed = RotateSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }

        public void AddBuff(BuffConfig buffConfig, int stack)
        {
            // TODO: boss buff系统未接入
        }

        public void CreateWeapon(int slotIndex, GameObject weaponPrefab)
        {
            weaponSlotManager?.CreateWeapon(slotIndex, weaponPrefab);
        }

        public void DestroyWeapon(int slotIndex)
        {
            weaponSlotManager?.DestroyWeapon(slotIndex);
        }

        public void OnHit(AttackData attackData)
        {
            // 1. 发射 ECS 伤害请求
            if (BossEntity.IsAlive())
            {
                DamageHelper.EmitDamage(BossEntity, attackData, transform.position);
            }

            // 2. 记录受击方向
            Vector3 hitDir = attackData.hitPoint - transform.position;
            hitDir.y = 0f;
            if (hitDir.sqrMagnitude < 0.0001f)
                hitDir = -transform.forward;
            LastHitDirection = hitDir.normalized;

            // 3. 切换到受伤状态
            if (MovementStateMachine != null)
            {
                MovementStateMachine.ChangeState(MovementStateMachine.hitState);
            }

            RayDebug.Log($"{gameObject.name}被命中！伤害值: {attackData.attackValue}");
        }

        /// <summary>
        /// 由 DeathSystem 通过 IDeathCallback 接口调用
        /// </summary>
        public void OnDeath()
        {
            RayDebug.Info($"{gameObject.name} 死亡！");
            if (MovementStateMachine != null)
            {
                MovementStateMachine.ChangeState(MovementStateMachine.deadState);
            }
        }

        public float GetAttackValue(SkillAttackDetectionEvent detectionEvent)
        {
            if (CharacterAttribute == null || detectionEvent?.AttackHitConfig == null)
                return 0f;

            return CharacterAttribute.attack.Total * detectionEvent.AttackHitConfig.AttackMultiply;
        }
    }
}
