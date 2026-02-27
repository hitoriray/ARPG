using Animancer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boss
{
    /// <summary>
    /// Boss 攀爬/翻越状态。
    /// 逻辑对标 PlayerClimbState，去掉输入依赖，改为由 AI 自动触发。
    /// 进入此状态前必须先调用 BossClimbDetector.TryDetectClimbable() 并将结果写入 ReusableData。
    /// </summary>
    public class BossClimbState : BossStateBase
    {
        private AnimancerState climbAnimState;
        private PlayerClimbData climbData;

        private ClimbTargetMatchInfo targetMatchInfo_Start;
        private ClimbTargetMatchInfo targetMatchInfo_Y;
        private PlayerClimbAnimationSettings animationSettings;

        private readonly List<AnimancerEvent> eventList = new();
        private Action onClimbEnd;

        public BossClimbState(BossController boss) : base(boss)
        {
            climbData = playerSO?.playerMovementData?.PlayerClimbData;
        }

        public override void OnEnter()
        {
            if (climbData == null)
            {
                RayDebug.Warn("[BossClimbState] PlayerClimbData 为空，无法攀爬，回退到 Idle");
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.idleState);
                return;
            }

            var clip = GetClimbAnimation();
            if (clip == null)
            {
                RayDebug.Warn("[BossClimbState] 找不到对应高度的攀爬动画，回退到 Idle");
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.idleState);
                return;
            }

            // 禁用物理控制，交由根运动驱动
            boss.disableGravity = true;
            boss.controller.enabled = false;
            boss.applyFullRootMotion = true;

            climbAnimState = animancer.Play(clip);
            climbAnimState.ApplyFootIK = true;

            animationSettings = GetClimbAnimationSettings();

            // 运动匹配目标点（与 PlayerClimbState 完全一致）
            targetMatchInfo_Y = new ClimbTargetMatchInfo(
                reusableData.vaultPos + Vector3.up * animationSettings.targetHeightOffSet);
            targetMatchInfo_Start = new ClimbTargetMatchInfo(
                new Vector3(reusableData.hit.point.x, boss.transform.position.y, reusableData.hit.point.z)
                + reusableData.hit.normal * (0.35f + animationSettings.startMatchDistanceOffset));

            // 动画结束时回到 Idle
            climbAnimState.Events(boss).OnEnd = OnClimbAnimationEnd;
        }

        public override void OnExit()
        {
            climbAnimState?.Events(boss).RemoveAll(eventList);
            climbAnimState = null;
            onClimbEnd = null;

            // 恢复物理控制
            boss.disableGravity = false;
            boss.controller.enabled = true;
            boss.applyFullRootMotion = false;
        }

        public override void OnAnimationUpdate()
        {
            if (climbAnimState == null)
                return;

            // 运动匹配：先匹配 XZ（靠近墙），再匹配 Y（爬上高台）
            ClimbTargetMatch(climbAnimState, ref targetMatchInfo_Start,
                animationSettings.startMatchTime.x, animationSettings.startMatchTime.y);

            ClimbTargetMatch_Y(climbAnimState, ref targetMatchInfo_Y,
                animationSettings.targetMatchTime.x, animationSettings.targetMatchTime.y);
        }

        public override void OnAnimationEnd()
        {
            // 由 BossController.AnimationEnd() 驱动（如需要）
        }

        // ── 运动匹配算法（移植自 PlayerReusableLogic）────────────
        private void ClimbTargetMatch(AnimancerState state, ref ClimbTargetMatchInfo info,
            float startTime, float endTime)
        {
            float t = state.NormalizedTime;
            if (!info.setTargetMatchInitPos && t > startTime)
            {
                info.setTargetMatchInitPos = true;
                info.InitPos = boss.transform.position;
            }

            if (t > startTime && t < endTime)
            {
                float alpha = (t - startTime) / (endTime - startTime);
                boss.transform.position = Vector3.Lerp(info.InitPos, info.TargetPos, alpha);
            }
        }

        private void ClimbTargetMatch_Y(AnimancerState state, ref ClimbTargetMatchInfo info,
            float startTime, float endTime)
        {
            float t = state.NormalizedTime;
            if (!info.setTargetMatchInitPos && t > startTime)
            {
                info.setTargetMatchInitPos = true;
                info.InitPos = boss.transform.position;
            }

            if (t > startTime && t < endTime)
            {
                float alpha = (t - startTime) / (endTime - startTime);
                Vector3 targetPos = new Vector3(
                    boss.transform.position.x,
                    Mathf.Lerp(info.InitPos.y, info.TargetPos.y, alpha),
                    boss.transform.position.z);
                boss.transform.position = targetPos;
            }
        }

        // ── 动画结束 ──────────────────────────────────────────
        private void OnClimbAnimationEnd()
        {
            // 攀爬完毕，回到 Idle，由行为树继续决策
            if (boss.MovementStateMachine != null)
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.idleState);
        }

        // ── 动画选取（直接复用 PlayerClimbState 中的逻辑）────────
        private ClipTransition GetClimbAnimation()
        {
            int index = (int)reusableData.ObstructHeightLevel;

            if (reusableData.ClimbType == ClimbType.Climb)
            {
                if (index < 0) index = 0;
                if (index >= climbData.climbs.Length) return null;
                return climbData.climbs[index];
            }
            else if (reusableData.ClimbType == ClimbType.Vault)
            {
                index--; // 最低障碍物不翻越
                if (index < 0) index = 0;
                if (index >= climbData.vaults.Length) return null;
                return climbData.vaults[index];
            }

            return null;
        }

        private PlayerClimbAnimationSettings GetClimbAnimationSettings()
        {
            int index = (int)reusableData.ObstructHeightLevel;

            if (reusableData.ClimbType == ClimbType.Climb)
            {
                if (index >= climbData.climbSettings.Length || climbData.climbSettings[index] == null)
                    return climbData.climbSettings[0];
                return climbData.climbSettings[index];
            }
            else if (reusableData.ClimbType == ClimbType.Vault)
            {
                if (index >= climbData.vaultSettings.Length || climbData.vaultSettings[index] == null)
                    return climbData.vaultSettings[0];
                return climbData.vaultSettings[index];
            }

            return climbData.climbSettings[0];
        }
    }
}
