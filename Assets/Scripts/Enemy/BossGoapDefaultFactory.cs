using System;
using GOAP;
using GOAP.Action;
using GOAP.Goals;
using GOAP.Plan;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Boss GOAP 状态名称常量。
    /// </summary>
    public static class BossGoapStateNames
    {
        public const string HasTarget = "Boss.HasTarget";
        public const string InAttackRange = "Boss.InAttackRange";
        public const string AttackReady = "Boss.AttackReady";
        public const string NeedRetreat = "Boss.NeedRetreat";
        public const string NeedDodge = "Boss.NeedDodge";
        public const string TooClose = "Boss.TooClose";
        public const string TooFar = "Boss.TooFar";
        public const string Attacked = "Boss.Attacked";
    }

    /// <summary>
    /// 一键构建默认 Boss GOAP（状态 + 目标 + 行为）。
    /// </summary>
    public static class BossGoapDefaultFactory
    {
        public static void Configure(GOAPAgent agent)
        {
            if (agent == null)
                return;

            ConfigureStates(agent);
            ConfigureGoals(agent);
            ConfigureActions(agent);
        }

        private static void ConfigureStates(GOAPAgent agent)
        {
            if (agent.states == null)
                agent.states = new GOAPStates();

            agent.states.StateDict.Clear();
            agent.states.TryAddState(BossGoapStateNames.HasTarget, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.InAttackRange, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.AttackReady, new BoolState { value = true });
            agent.states.TryAddState(BossGoapStateNames.NeedRetreat, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.NeedDodge, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.TooClose, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.TooFar, new BoolState { value = false });
            agent.states.TryAddState(BossGoapStateNames.Attacked, new BoolState { value = false });
        }

        private static void ConfigureGoals(GOAPAgent agent)
        {
            if (agent.goals == null)
                agent.goals = new GOAPGoals();

            agent.goals.goalItemDict.Clear();

            agent.goals.goalItemDict.Add("FindTarget", new GOAPGoals.GoalItem
            {
                targetState = BossGoapStateNames.HasTarget,
                targetValue = NewBoolComparer(BoolValue.是),
                proirityMultiply = 1f,
                runtimeProirity = 0f,
                canBreak = true,
                canBeBreak = true,
                goalChecker = new BossFindTargetGoalChecker(),
            });

            agent.goals.goalItemDict.Add("Retreat", new GOAPGoals.GoalItem
            {
                targetState = BossGoapStateNames.NeedRetreat,
                targetValue = NewBoolComparer(BoolValue.否),
                proirityMultiply = 1f,
                runtimeProirity = 0f,
                canBreak = true,
                canBeBreak = true,
                goalChecker = new BossRetreatGoalChecker(),
            });

            agent.goals.goalItemDict.Add("DodgeThreat", new GOAPGoals.GoalItem
            {
                targetState = BossGoapStateNames.NeedDodge,
                targetValue = NewBoolComparer(BoolValue.否),
                proirityMultiply = 1f,
                runtimeProirity = 0f,
                canBreak = true,
                canBeBreak = false,
                goalChecker = new BossDodgeGoalChecker(),
            });

            agent.goals.goalItemDict.Add("AttackLoop", new GOAPGoals.GoalItem
            {
                targetState = BossGoapStateNames.Attacked,
                targetValue = NewBoolComparer(BoolValue.是),
                proirityMultiply = 1f,
                runtimeProirity = 0f,
                canBreak = true,
                canBeBreak = true,
                goalChecker = new BossAttackGoalChecker(),
            });
        }

        private static void ConfigureActions(GOAPAgent agent)
        {
            if (agent.actions == null)
                agent.actions = new GOAPActions();

            agent.actions.actions.Clear();

            var acquire = new BossAcquireTargetAction
            {
                costValue = 0.2f,
                effectValue = 5f,
            };
            acquire.effects.Add(NewBoolEffect(BossGoapStateNames.HasTarget, BoolValue.是));
            agent.actions.actions.Add(acquire);

            var dodge = new BossDodgeAction
            {
                costValue = 0.4f,
                effectValue = 12f,
            };
            dodge.preconditions.Add(NewBoolCondition(BossGoapStateNames.HasTarget, BoolValue.是));
            dodge.preconditions.Add(NewBoolCondition(BossGoapStateNames.NeedDodge, BoolValue.是));
            dodge.effects.Add(NewBoolEffect(BossGoapStateNames.NeedDodge, BoolValue.否));
            dodge.effects.Add(NewBoolEffect(BossGoapStateNames.NeedRetreat, BoolValue.否));
            agent.actions.actions.Add(dodge);

            var chase = new BossChaseToAttackRangeAction
            {
                costValue = 0.5f,
                effectValue = 6f,
            };
            chase.preconditions.Add(NewBoolCondition(BossGoapStateNames.HasTarget, BoolValue.是));
            chase.effects.Add(NewBoolEffect(BossGoapStateNames.InAttackRange, BoolValue.是));
            agent.actions.actions.Add(chase);

            var waitReady = new BossWaitAttackReadyAction
            {
                costValue = 0.6f,
                effectValue = 4f,
            };
            waitReady.preconditions.Add(NewBoolCondition(BossGoapStateNames.HasTarget, BoolValue.是));
            waitReady.effects.Add(NewBoolEffect(BossGoapStateNames.AttackReady, BoolValue.是));
            agent.actions.actions.Add(waitReady);

            var attack = new BossAttackAction
            {
                costValue = 1.1f,
                effectValue = 9f,
            };
            attack.preconditions.Add(NewBoolCondition(BossGoapStateNames.HasTarget, BoolValue.是));
            attack.preconditions.Add(NewBoolCondition(BossGoapStateNames.InAttackRange, BoolValue.是));
            attack.preconditions.Add(NewBoolCondition(BossGoapStateNames.AttackReady, BoolValue.是));
            attack.preconditions.Add(NewBoolCondition(BossGoapStateNames.NeedRetreat, BoolValue.否));
            attack.preconditions.Add(NewBoolCondition(BossGoapStateNames.Attacked, BoolValue.否));
            attack.effects.Add(NewBoolEffect(BossGoapStateNames.Attacked, BoolValue.是));
            attack.effects.Add(NewBoolEffect(BossGoapStateNames.AttackReady, BoolValue.否));
            agent.actions.actions.Add(attack);

            var retreat = new BossRetreatAction
            {
                costValue = 0.8f,
                effectValue = 7f,
            };
            retreat.preconditions.Add(NewBoolCondition(BossGoapStateNames.HasTarget, BoolValue.是));
            retreat.preconditions.Add(NewBoolCondition(BossGoapStateNames.NeedRetreat, BoolValue.是));
            retreat.effects.Add(NewBoolEffect(BossGoapStateNames.NeedRetreat, BoolValue.否));
            retreat.effects.Add(NewBoolEffect(BossGoapStateNames.InAttackRange, BoolValue.否));
            retreat.effects.Add(NewBoolEffect(BossGoapStateNames.Attacked, BoolValue.否));
            agent.actions.actions.Add(retreat);
        }

        private static GOAPTypeAndComparer NewBoolCondition(string stateName, BoolValue value)
        {
            return new GOAPTypeAndComparer
            {
                stateType = stateName,
                stateComparer = NewBoolComparer(value),
            };
        }

        private static GOAPTypeAndComparer NewBoolEffect(string stateName, BoolValue value)
        {
            return NewBoolCondition(stateName, value);
        }

        private static BoolStateComparer NewBoolComparer(BoolValue value)
        {
            return new BoolStateComparer { value = value };
        }
    }

    [Serializable]
    public class BossFindTargetGoalChecker : IGOAPGoalChecker
    {
        [SerializeField] private float noTargetPriority = 11f;
        [SerializeField] private float hasTargetPriority = 0f;

        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner)
        {
            EnemyController enemy = owner as EnemyController;
            if (enemy == null || enemy.IsDead)
            {
                goalItem.runtimeProirity = 0f;
                return;
            }

            goalItem.runtimeProirity = enemy.HasTarget ? hasTargetPriority : noTargetPriority;
        }
    }

    [Serializable]
    public class BossRetreatGoalChecker : IGOAPGoalChecker
    {
        [SerializeField] private float retreatPriority = 10f;

        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner)
        {
            EnemyController enemy = owner as EnemyController;
            if (enemy == null || enemy.IsDead || !enemy.HasTarget)
            {
                goalItem.runtimeProirity = 0f;
                return;
            }

            goalItem.runtimeProirity = BossGoapUtility.ReadBool(agent, BossGoapStateNames.NeedRetreat)
                ? retreatPriority
                : 0f;
        }
    }

    [Serializable]
    public class BossDodgeGoalChecker : IGOAPGoalChecker
    {
        [SerializeField] private float dodgePriority = 12f;

        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner)
        {
            EnemyController enemy = owner as EnemyController;
            if (enemy == null || enemy.IsDead || !enemy.HasTarget)
            {
                goalItem.runtimeProirity = 0f;
                return;
            }

            goalItem.runtimeProirity = BossGoapUtility.ReadBool(agent, BossGoapStateNames.NeedDodge)
                ? dodgePriority
                : 0f;
        }
    }

    [Serializable]
    public class BossAttackGoalChecker : IGOAPGoalChecker
    {
        [SerializeField] private float attackPriority = 9f;
        [SerializeField] private float pressurePriority = 3f;

        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner)
        {
            EnemyController enemy = owner as EnemyController;
            if (enemy == null || enemy.IsDead || !enemy.HasTarget)
            {
                goalItem.runtimeProirity = 0f;
                return;
            }

            bool needRetreat = BossGoapUtility.ReadBool(agent, BossGoapStateNames.NeedRetreat);
            bool needDodge = BossGoapUtility.ReadBool(agent, BossGoapStateNames.NeedDodge);
            if (needRetreat || needDodge)
            {
                goalItem.runtimeProirity = 0f;
                return;
            }

            goalItem.runtimeProirity = enemy.IsAttackReadyForBrain() ? attackPriority : pressurePriority;
        }
    }

    [Serializable]
    public abstract class BossGoapActionBase : GOAPActionBase
    {
        protected EnemyController enemy;

        public override void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            base.Init(agent, owner);
            enemy = owner as EnemyController;
        }

        protected bool IsInvalidOwner()
        {
            return enemy == null || enemy.IsDead;
        }
    }

    [Serializable]
    public class BossAcquireTargetAction : BossGoapActionBase
    {
        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            enemy.BrainRefreshTarget();
            if (enemy.HasTarget)
            {
                enemy.BrainDebugGoap("Action AcquireTarget 成功：已获取目标");
                ApplyEffect();
                return GOAPRunState.Succeed;
            }

            return GOAPRunState.Running;
        }
    }

    [Serializable]
    public class BossDodgeAction : BossGoapActionBase
    {
        private bool startedDodge;

        public override void OnStart()
        {
            enemy?.BrainDebugGoap("Action DodgeThreat 开始");
            startedDodge = enemy != null && enemy.BrainTryStartDefensiveDodge();
        }

        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            if (!enemy.HasTarget)
                return GOAPRunState.Failed;

            if (!startedDodge)
            {
                startedDodge = enemy.BrainTryStartDefensiveDodge();
                if (!startedDodge)
                {
                    enemy.BrainDebugGoap("Action DodgeThreat 失败：无法开始闪避");
                    return GOAPRunState.Failed;
                }
            }

            if (enemy.BrainIsDodging())
                return GOAPRunState.Running;

            enemy.BrainDebugGoap("Action DodgeThreat 成功：闪避结束");
            ApplyEffect();
            return GOAPRunState.Succeed;
        }
    }

    [Serializable]
    public class BossChaseToAttackRangeAction : BossGoapActionBase
    {
        public override void OnStart()
        {
            if (enemy != null)
            {
                enemy.BrainDebugGoap("Action ChaseToAttackRange 开始");
                enemy.BrainSetMoveIntentChase();
            }
        }

        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            enemy.BrainRefreshTarget();
            if (!enemy.HasTarget)
                return GOAPRunState.Failed;

            enemy.BrainSetMoveIntentChase();
            if (enemy.BrainHasTargetInRange(enemy.AttackRange))
            {
                enemy.BrainDebugGoap("Action ChaseToAttackRange 成功：进入攻击距离");
                ApplyEffect();
                return GOAPRunState.Succeed;
            }

            return GOAPRunState.Running;
        }

        public override void OnStop()
        {
            enemy?.BrainClearMoveIntent();
        }
    }

    [Serializable]
    public class BossWaitAttackReadyAction : BossGoapActionBase
    {
        public override void OnStart()
        {
            enemy?.BrainDebugGoap("Action WaitAttackReady 开始");
        }

        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            if (!enemy.HasTarget)
                return GOAPRunState.Failed;

            if (enemy.IsAttackReadyForBrain())
            {
                enemy.BrainClearMoveIntent();
                enemy.BrainDebugGoap("Action WaitAttackReady 成功：攻击已就绪");
                ApplyEffect();
                return GOAPRunState.Succeed;
            }

            enemy.BrainAdjustSpacingWhenWaiting();

            return GOAPRunState.Running;
        }

        public override void OnStop()
        {
            enemy?.BrainClearMoveIntent();
        }
    }

    [Serializable]
    public class BossAttackAction : BossGoapActionBase
    {
        private bool startedAttack;

        public override void OnStart()
        {
            enemy?.BrainDebugGoap("Action Attack 开始");
            startedAttack = enemy != null && enemy.BrainTryStartAttack();
        }

        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            if (!enemy.HasTarget)
                return GOAPRunState.Failed;

            if (!startedAttack)
            {
                if (!enemy.BrainHasTargetInRange(enemy.AttackRange) || !enemy.IsAttackReadyForBrain())
                {
                    enemy.BrainDebugGoap("Action Attack 失败：不在攻击条件");
                    return GOAPRunState.Failed;
                }

                startedAttack = enemy.BrainTryStartAttack();
                if (!startedAttack)
                {
                    enemy.BrainDebugGoap("Action Attack 失败：无法切入攻击状态");
                    return GOAPRunState.Failed;
                }
            }

            if (enemy.BrainIsAttacking())
                return GOAPRunState.Running;

            enemy.BrainDebugGoap("Action Attack 成功：攻击流程完成");
            ApplyEffect();
            return GOAPRunState.Succeed;
        }
    }

    [Serializable]
    public class BossRetreatAction : BossGoapActionBase
    {
        [SerializeField, Min(0f)] private float minRetreatTime = 0.7f;
        [SerializeField, Min(0f)] private float maxRetreatTime = 1.2f;
        private float retreatEndTime;

        public override void OnStart()
        {
            if (enemy == null)
                return;

            float duration = UnityEngine.Random.Range(minRetreatTime, maxRetreatTime);
            retreatEndTime = Time.time + duration;
            enemy.BrainDebugGoap($"Action Retreat 开始：持续到 {retreatEndTime:F2}");
            enemy.BrainSetMoveIntentRetreat();
        }

        public override GOAPRunState OnUpdate()
        {
            if (IsInvalidOwner())
                return GOAPRunState.Failed;

            if (!enemy.HasTarget)
            {
                ApplyEffect();
                return GOAPRunState.Succeed;
            }

            enemy.BrainSetMoveIntentRetreat();
            if (!enemy.BrainNeedRetreatForSpacing() ||
                enemy.BrainDistanceToTarget() >= enemy.RetreatDistance ||
                Time.time >= retreatEndTime)
            {
                enemy.BrainDebugGoap("Action Retreat 成功：已完成拉开距离");
                ApplyEffect();
                return GOAPRunState.Succeed;
            }

            return GOAPRunState.Running;
        }

        public override void OnStop()
        {
            enemy?.BrainClearMoveIntent();
        }
    }

    internal static class BossGoapUtility
    {
        public static bool ReadBool(GOAPAgent agent, string stateName)
        {
            if (agent == null || agent.states == null)
                return false;

            if (!agent.states.TryGetState<BoolState>(stateName, out var state))
                return false;

            return state.value;
        }
    }
}
