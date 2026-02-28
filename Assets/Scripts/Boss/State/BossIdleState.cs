using Animancer;
using UnityEngine;

namespace Boss
{
    public class BossIdleState : BossMovementState
    {
        public BossIdleState(BossController boss) : base(boss) { }

        public override void OnEnter()
        {
            if (playerSO == null || reusableData == null)
                return;

            reusableData.currentCrouchIdleIndex = -1;
            reusableData.currentStandIdleIndex = -1;

            InitIdleState();
            PlayNextState();

            UpdateSpeedParam(0f);
        }

        public override void OnUpdate()
        {
            if (!boss.AI.FaceTarget || boss.AI.Target == null)
                return;

            Vector3 dir = boss.AI.Target.position - boss.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;

            UpdateRotation(dir);
        }

        private void InitIdleState()
        {
            var state = animancer.Play(playerSO.playerMovementData.PlayerIdleData.idle);

            bool isUpdateIdleState = (reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 0) ||
                                     (!reusableData.isLockIdle && reusableData.lockValueParameter.TargetValue == 1);

            if (reusableData.standIdleMixerState == null || isUpdateIdleState)
            {
                if (reusableData.lockValueParameter.TargetValue == 1)
                {
                    reusableData.standIdleMixerState = state.GetChild(1).GetChild(1) as ManualMixerState;
                    reusableData.isLockIdle = true;
                }
                else
                {
                    reusableData.standIdleMixerState = state.GetChild(0).GetChild(1) as ManualMixerState;
                    reusableData.isLockIdle = false;
                }
            }

            if (reusableData.crouchIdleMixerState == null || isUpdateIdleState)
            {
                if (reusableData.lockValueParameter.TargetValue == 1)
                {
                    reusableData.crouchIdleMixerState = state.GetChild(1).GetChild(0) as ManualMixerState;
                    reusableData.isLockIdle = true;
                }
                else
                {
                    reusableData.crouchIdleMixerState = state.GetChild(0).GetChild(0) as ManualMixerState;
                    reusableData.isLockIdle = false;
                }
            }

            if (reusableData.standIdleMixerState != null &&
                (reusableData.standIdleList.Count != reusableData.standIdleMixerState.ChildCount || isUpdateIdleState))
            {
                reusableData.standIdleList.Clear();
                for (int i = 0; i < reusableData.standIdleMixerState.ChildCount; i++)
                {
                    var animancerState = reusableData.standIdleMixerState.GetChild(i);
                    animancerState.Events(boss).OnEnd = PlayNextState;
                    reusableData.standIdleList.Add(animancerState);
                }

                reusableData.standIdleList[0].Weight = 1;
            }

            if (reusableData.crouchIdleMixerState != null &&
                (reusableData.crouchIdleList.Count != reusableData.crouchIdleMixerState.ChildCount || isUpdateIdleState))
            {
                reusableData.crouchIdleList.Clear();
                for (int i = 0; i < reusableData.crouchIdleMixerState.ChildCount; i++)
                {
                    var animancerState = reusableData.crouchIdleMixerState.GetChild(i);
                    if (reusableData.crouchIdleMixerState.ChildCount != 1)
                        animancerState.Events(boss).OnEnd = PlayNextState;
                    reusableData.crouchIdleList.Add(animancerState);
                }

                reusableData.crouchIdleList[0].Weight = 1;
            }
        }

        private void PlayNextState()
        {
            if (reusableData.standValueParameter.TargetValue == 1)
            {
                if (reusableData.standIdleList.Count == 0) return;

                reusableData.currentStandIdleIndex =
                    (reusableData.currentStandIdleIndex + 1) % reusableData.standIdleList.Count;

                for (int i = 0; i < reusableData.standIdleList.Count; i++)
                {
                    if (i == reusableData.currentStandIdleIndex)
                    {
                        reusableData.standIdleList[i].SetWeight(1);
                        reusableData.standIdleList[i].Play();
                    }
                    else
                    {
                        reusableData.standIdleList[i].SetWeight(0);
                        reusableData.standIdleList[i].Stop();
                    }
                }
            }
            else if (reusableData.standValueParameter.TargetValue == 0)
            {
                if (reusableData.crouchIdleList.Count == 0) return;

                reusableData.currentCrouchIdleIndex =
                    (reusableData.currentCrouchIdleIndex + 1) % reusableData.crouchIdleList.Count;

                for (int i = 0; i < reusableData.crouchIdleList.Count; i++)
                {
                    if (i == reusableData.currentCrouchIdleIndex)
                    {
                        reusableData.crouchIdleList[i].SetWeight(1);
                        reusableData.crouchIdleList[i].Play();
                    }
                    else
                    {
                        reusableData.crouchIdleList[i].SetWeight(0);
                        reusableData.crouchIdleList[i].Stop();
                    }
                }
            }
        }
    }
}
