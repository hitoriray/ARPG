using Animancer;
using UnityEngine;

namespace Boss
{
    public abstract class BossEvasionState : BossMovementState
    {
        protected enum EvasionDirection { Forward, Backward, Left, Right }

        private int invincibleTimerId = -1;

        protected abstract ClipTransition GetClip(EvasionDirection dir);
        protected abstract float InvincibleDuration { get; }
        protected abstract float CooldownTime { get; }
        protected abstract float ForwardThreshold { get; }
        protected abstract float SideThreshold { get; }
        protected virtual bool PreferSideStep => false;
        protected virtual bool PreferBackward => true;

        protected BossEvasionState(BossController boss) : base(boss) { }

        public override void OnEnter()
        {
            base.OnEnter();

            if (reusableData == null)
            {
                ReturnToDefault();
                return;
            }

            if (Time.time - reusableData.lastEvasiveActionTime < CooldownTime)
            {
                ReturnToDefault();
                return;
            }

            reusableData.lastEvasiveActionTime = Time.time;

            EvasionDirection dir = DetermineDirection();
            PlayEvasionAnimation(dir);

            if (InvincibleDuration > 0f)
            {
                reusableData.isInvincible = true;
                invincibleTimerId = TimerService.Instance.AddTimer((int)(InvincibleDuration * 1000), OnInvincibleEnd);
            }
        }

        public override void OnUpdate()
        {
            // 闪避期间硬直
        }

        public override void OnExit()
        {
            base.OnExit();

            reusableData.isInvincible = false;
            if (invincibleTimerId != -1)
            {
                TimerService.Instance.RemoveTimer(invincibleTimerId);
                invincibleTimerId = -1;
            }
        }

        public override void OnAnimationEnd() { }
        public override void OnAnimationUpdate() { }

        private EvasionDirection DetermineDirection()
        {
            if (boss.AI.TryConsumeEvasionDir(out var overrideDir))
                return ToEvasionDirection(overrideDir);

            if (boss.AI.HasMove)
                return ToEvasionDirection(boss.AI.MoveDir);

            if (boss.AI.Target != null)
            {
                Vector3 toTarget = boss.AI.Target.position - boss.transform.position;
                toTarget.y = 0f;

                if (PreferSideStep)
                {
                    return Random.value < 0.5f ? EvasionDirection.Left : EvasionDirection.Right;
                }

                if (PreferBackward)
                    return EvasionDirection.Backward;

                if (toTarget.sqrMagnitude > 0.0001f)
                    return ToEvasionDirection(-toTarget);
            }

            return EvasionDirection.Backward;
        }

        private EvasionDirection ToEvasionDirection(Vector3 worldDir)
        {
            if (worldDir.sqrMagnitude <= 0.0001f)
                return EvasionDirection.Backward;

            Vector3 local = boss.transform.InverseTransformDirection(worldDir.normalized);
            if (Mathf.Abs(local.x) > SideThreshold)
                return local.x > 0 ? EvasionDirection.Right : EvasionDirection.Left;

            if (local.z > ForwardThreshold)
                return EvasionDirection.Forward;

            return EvasionDirection.Backward;
        }

        private void PlayEvasionAnimation(EvasionDirection dir)
        {
            ClipTransition clip = GetClip(dir);
            if (clip != null && clip.Clip != null)
            {
                animancer.Play(clip).Events(boss).OnEnd = OnEvasionComplete;
            }
            else
            {
                RayDebug.Error($"{GetType().Name} 动画未配置：{dir}");
                ReturnToDefault();
            }
        }

        private void OnInvincibleEnd()
        {
            reusableData.isInvincible = false;
            invincibleTimerId = -1;
        }

        private void OnEvasionComplete()
        {
            ReturnToDefault();
        }

        private void ReturnToDefault()
        {
            if (boss.AI.HasMove)
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.moveState);
            else
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.idleState);
        }
    }
}
