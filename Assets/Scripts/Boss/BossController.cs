using System;
using Animancer;
using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS;
using Battle.ECS.Core.Helper;
using Config;
using Manager;
using UnityEngine;

namespace Boss
{
    public class BossController : CharacterControllerBase, ICharacter
    {
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
        // CharacterConfig / PlayerSO / WalkSpeed / RunSpeed / RotateSpeed 由基类提供
        public bool IsDead => isDead;
        
        
        private bool inSkill;
        private bool currentSkillUpperBody;
        private bool isDead;

        // 击退位移
        private Vector3 _repelVelocity;   // 当前击退速度（世界坐标）
        private float _repelRemainTime;  // 击退剩余时间

        protected override void Awake()
        {
            base.Awake();
            if (modelTransform == null)
                modelTransform = transform;
        }
        
        public override void Init(CharacterConfig config)
        {
            base.Init(config);
            
            isDead = false;

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
            SetupAnimancerLayers(skillLayerIndex);
        }

        protected override void Update()
        {
            base.Update();

            MovementStateMachine?.OnUpdate();
            MovementStateMachine?.TickAI();

            ApplyRepelMotion();
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
            if (isDead)
                return;

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
            if (isDead)
                return false;

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
            if (isDead)
                return false;

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
            if (isDead)
                return;

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
            base.EnterSkillMode(upperBody, skillLayerIndex, upperBodyMask);
        }

        public void ExitSkillMode()
        {
            if (!inSkill)
                return;

            inSkill = false;
            currentSkillUpperBody = false;
            base.ExitSkillMode(skillLayerIndex, skillLayerFadeOut);
        }

        private void CleanupFinishedSkillLayer()
        {
            if (inSkill) return;
            base.CleanupFinishedSkillLayer(skillLayerIndex);
        }
        
        public bool IsPlayerControlled => false;

        // OnSkillMove / OnSkillRotate(Quaternion) 由基类提供

        public void TryReleaseSkillBySkillIndex(int skillIndex)
        {
            throw new NotImplementedException();
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
            if (isDead)
                return;
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

            // 3. 击退位移
            var hitConfig = attackData.detectionEvent?.AttackHitConfig;
            if (hitConfig != null && hitConfig.RepelTime > 0f && hitConfig.RepelStrength.sqrMagnitude > 0f)
            {
                _repelVelocity = CalcKnockbackWorldVelocity(hitConfig, attackData);
                _repelRemainTime = hitConfig.RepelTime;
            }

            // 4. 切换到受伤状态
            if (MovementStateMachine != null)
            {
                MovementStateMachine.ChangeState(MovementStateMachine.hitState);
            }

            RayDebug.Log($"{gameObject.name}被命中！伤害值: {attackData.attackValue}");
        }

        private Vector3 CalcKnockbackWorldVelocity(AttackHitConfig hitConfig, AttackData attackData)
        {
            Vector3 strength = hitConfig.RepelStrength;
            switch (hitConfig.KnockbackDirection)
            {
                case KnockbackDirection.PlayerOpposite:
                {
                    // 攻击者指向被击者的方向
                    Vector3 knockDir = (transform.position - attackData.hitPoint);
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude < 0.0001f)
                        knockDir = -transform.forward;
                    knockDir.Normalize();
                    // strength.z 为居4展开的透风强度，strength.y 为上相分量
                    return knockDir * strength.z + Vector3.up * strength.y;
                }
                case KnockbackDirection.WorldSpace:
                    return strength;
                case KnockbackDirection.SkillForward:
                {
                    // 施法者前向
                    if (attackData.source == null) goto default;
                    var src = attackData.source as MonoBehaviour;
                    if (src == null) goto default;
                    Quaternion fwdRot = Quaternion.LookRotation(src.transform.forward, Vector3.up);
                    return fwdRot * strength;
                }
                default:
                    return strength;
            }
        }

        private void ApplyRepelMotion()
        {
            if (_repelRemainTime <= 0f || controller == null) return;

            float dt = Time.deltaTime;
            controller.Move(_repelVelocity * dt);
            _repelRemainTime -= dt;
            if (_repelRemainTime <= 0f)
            {
                _repelRemainTime = 0f;
                _repelVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// 由 DeathSystem 通过 IDeathCallback 接口调用
        /// </summary>
        public void OnDeath()
        {
            if (isDead)
                return;

            isDead = true;
            RayDebug.Info($"{gameObject.name} 死亡！");
            
            // 死亡瞬间关闭所有碰撞体，防止在死亡动画期间发生发生攻击判定
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

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

        public void TryReleaseBasicAttack()
        {
        }
    }
}
