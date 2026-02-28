using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Animancer;
using UnityEngine;

namespace BT.Actions
{
    /// <summary>
    /// 巡逻节点（升级版）
    /// 
    /// 功能：
    /// 1. 双模式：有 Waypoints → 按路径巡逻（支持 PingPong）；
    ///            无 Waypoints  → 在 RandomRange 半径内随机选点巡逻。
    /// 2. 到达巡逻点后进入休息状态：播放可选的 IdleVariant 动画，休息 [RestMinTime, RestMaxTime] 秒。
    /// 3. 巡逻过程中检测视野（SeenTarget）：一旦发现玩家，立即设置 Target 并返回 Success，
    ///    外部行为树的 Selector/Sequence 结构负责切换到追击分支。
    /// </summary>
    [TaskCategory("Enemy/Boss")]
    [TaskDescription("巡逻：支持 Waypoint 路径或随机区域巡逻，到达后可休息，发现玩家立即中止。")]
    public class Patrol : BossActionBase
    {
        [Header("路径模式")]
        [BehaviorDesigner.Runtime.Tasks.Tooltip("为空则使用随机巡逻")]
        public SharedTransformList Waypoints;
        public SharedBool PingPong;

        [Header("随机巡逻")]
        [BehaviorDesigner.Runtime.Tasks.Tooltip("以 Boss 出生点为圆心的随机巡逻半径（Waypoints 为空时生效）")]
        public SharedFloat RandomRange;

        [Header("移动参数")]
        public SharedFloat StopDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        [Header("到达后休息")]
        [BehaviorDesigner.Runtime.Tasks.Tooltip("到达巡逻点后休息的动画（可选，为空则只站立等待）")]
        public SharedObject IdleVariantClip;   // 填 ClipTransition / AnimationClip
        public SharedFloat RestMinTime;
        public SharedFloat RestMaxTime;

        [Header("视野中止")]
        [BehaviorDesigner.Runtime.Tasks.Tooltip("发现玩家时写入 Target，行为树外层负责切换追击分支")]
        public SharedTransform SeenTarget;
        public SharedTransform Target;

        // ── 私有状态 ──────────────────────────────────────────────
        private enum PatrolPhase { Moving, Resting }
        private PatrolPhase _phase = PatrolPhase.Moving;

        private int _waypointIndex;
        private int _waypointDir = 1;
        private Vector3 _currentTarget;
        private bool _targetSet;
        private float _restTimer;
        private Vector3 _originPos; // 随机模式的圆心（第一次激活时记录）
        private bool _originCaptured;

        // ── 生命周期 ──────────────────────────────────────────────
        public override void OnStart()
        {
            base.OnStart();
            _phase = PatrolPhase.Moving;
            _restTimer = 0f;

            // 只在最初一次记录出生点作为随机巡逻圆心
            if (!_originCaptured && boss != null)
            {
                _originPos = boss.transform.position;
                _originCaptured = true;
            }

            // 第一次进入时选好目标点
            if (!_targetSet)
                PickNextTarget();
        }

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            // ① 每帧检测视野：发现玩家立即中止巡逻
            if (SeenTarget.Value != null)
            {
                Target.Value = SeenTarget.Value;
                boss.SetTarget(SeenTarget.Value);
                boss.ClearDesiredMove();
                astarMover?.ClearDestination();
                _targetSet = false;   // 下次进入重新选点
                return TaskStatus.Success;
            }

            // ② 休息阶段
            if (_phase == PatrolPhase.Resting)
            {
                _restTimer -= Time.deltaTime;
                if (_restTimer <= 0f)
                {
                    // 休息结束，选下一个巡逻点，切回移动
                    PickNextTarget();
                    _phase = PatrolPhase.Moving;
                }
                boss.ClearDesiredMove();
                return TaskStatus.Running;
            }

            // ③ 移动阶段
            Vector3 toTarget = _currentTarget - boss.transform.position;
            Vector3 toFlat   = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toFlat.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 1f;
            if (dist <= stop)
            {
                // 到达巡逻点：进入休息阶段
                boss.ClearDesiredMove();
                astarMover?.ClearDestination();
                EnterRest();
                return TaskStatus.Running;
            }

            // 移动
            Vector3 moveDir;
            if (astarMover != null)
            {
                astarMover.SetDestination(_currentTarget);
                moveDir = astarMover.DesiredDirection;
                if (moveDir.sqrMagnitude < 0.0001f)
                {
                    // A* 路径堵死，直接跳到下个点防止卡死
                    PickNextTarget();
                    return TaskStatus.Running;
                }
            }
            else
            {
                moveDir = toFlat.normalized;
            }

            float speedMult  = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(moveDir, speedMult, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
            astarMover?.ClearDestination();
        }

        // ── 内部逻辑 ──────────────────────────────────────────────

        private void PickNextTarget()
        {
            _targetSet = true;
            bool hasWaypoints = Waypoints.Value != null && Waypoints.Value.Count > 0;

            if (hasWaypoints)
            {
                // Waypoint 模式
                if (_waypointIndex >= Waypoints.Value.Count)
                    _waypointIndex = 0;

                Transform wp = Waypoints.Value[_waypointIndex];
                _currentTarget = wp != null ? wp.position : boss.transform.position;
                AdvanceWaypointIndex();
            }
            else
            {
                // 随机模式：在 _originPos 为圆心的圆内随机选点
                float range = RandomRange.Value > 0f ? RandomRange.Value : 8f;
                Vector2 rand = Random.insideUnitCircle * range;
                _currentTarget = _originPos + new Vector3(rand.x, 0f, rand.y);
            }
        }

        private void AdvanceWaypointIndex()
        {
            if (Waypoints.Value == null || Waypoints.Value.Count == 0) return;

            if (PingPong.Value && Waypoints.Value.Count > 1)
            {
                if (_waypointIndex == Waypoints.Value.Count - 1) _waypointDir = -1;
                else if (_waypointIndex == 0)                    _waypointDir = 1;
                _waypointIndex += _waypointDir;
            }
            else
            {
                _waypointIndex = (_waypointIndex + 1) % Waypoints.Value.Count;
            }
        }

        private void EnterRest()
        {
            _phase = PatrolPhase.Resting;

            float minT = RestMinTime.Value > 0f ? RestMinTime.Value : 1f;
            float maxT = RestMaxTime.Value > minT ? RestMaxTime.Value : minT + 2f;
            _restTimer = Random.Range(minT, maxT);

            // 播放 IdleVariant 动画（可选）
            if (IdleVariantClip.Value != null && boss != null)
            {
                var animancer = boss.GetComponentInChildren<AnimancerComponent>();
                animancer.Play((AnimationClip)IdleVariantClip.Value);
            }
        }
    }
}
