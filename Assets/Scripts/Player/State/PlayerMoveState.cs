// using System;
// using JKFrame;
// using RayAnimation;
// using UnityEngine;

// namespace RayPlayerState
// {
//     public class PlayerMoveState : PlayerStateBase
//     {
//         private enum MovePhase
//         {
//             WalkStart,
//             RunStart,
//             Loop,
//             RunEnd
//         }

//         private const string MoveStartLoopEvent = "MoveStartLoop";
//         private const string MoveStartCancelEvent = "MoveStartCancel";
//         private const string RunEndEndEvent = "RunEndEnd";
//         private const float PhaseTimeoutPadding = 0.05f;

//         private CharacterController characterController;
//         private float runTransition;
//         private bool applyRootMotionForMove;
//         private MovePhase movePhase;
//         private Action<Vector3, Quaternion> rootMotionAction;
//         private AnimationClip walkStartClip;
//         private AnimationClip runStartClip;
//         private AnimationClip runEndClip;
//         private float phaseElapsed;
//         private float phaseTimeout;

//         public override void Init(IStateMachineOwner owner)
//         {
//             base.Init(owner);
//             characterController = PlayerController.CharacterController;
//             applyRootMotionForMove = PlayerController.CharacterConfig.ApplyRootMotionForMove;
//         }

//         public override void Enter()
//         {
//             runTransition = 0;
//             rootMotionAction = applyRootMotionForMove ? OnRootMotion : null;
//             bool startAsRun = Input.GetKey(KeyCode.LeftShift);
//             movePhase = startAsRun ? MovePhase.RunStart : MovePhase.WalkStart;
//             runTransition = startAsRun ? 1f : 0f;
//             walkStartClip = PlayerController.CharacterConfig.GetAnimationClipByName(AnimationHelper.WalkStart);
//             runStartClip = PlayerController.CharacterConfig.GetAnimationClipByName(AnimationHelper.RunStart);
//             runEndClip = PlayerController.CharacterConfig.GetAnimationClipByName(AnimationHelper.RunEnd);
//             phaseElapsed = 0f;
//             phaseTimeout = 0f;

//             TryPlayStartAnimation();
//             animationController.AddAnimationEvent(MoveStartLoopEvent, OnMoveStartLoop);
//             animationController.AddAnimationEvent(MoveStartCancelEvent, OnMoveStartCancel);
//             animationController.AddAnimationEvent(RunEndEndEvent, OnRunEndEnd);
//             animationController.AddAnimationEvent("FootStep", OnFootStep);
//         }

//         public override void Update()
//         {
//             Vector2 moveInput = InputManager.Instance.GetMoveInput();
//             bool hasMoveInput = moveInput.x != 0 || moveInput.y != 0;
//             if (HandlePhaseTimeout(moveInput, hasMoveInput))
//                 return;

//             if (movePhase == MovePhase.Loop)
//             {
//                 bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
//                 if (UISystem.CheckMouseOnUI())
//                 {
//                     PlayerController.ChangeState(PlayerState.Idle);
//                     return;
//                 }
//                 if (CheckAndEnterSkillState())
//                     return;

//                 if (!hasMoveInput)
//                 {
//                     if (shiftPressed)
//                     {
//                         EnterRunEnd();
//                         return;
//                     }
//                     PlayerController.ChangeState(PlayerState.Idle);
//                     return;
//                 }

//                 UpdateRunTransition(shiftPressed);
//                 animationController.SetBlendWeight(1 - runTransition);
                
//                 // 更新根运动速度缩放
//                 if (applyRootMotionForMove)
//                     UpdateRootMotionSpeed();
//             }
//             else if (movePhase == MovePhase.RunEnd)
//             {
//                 if (!applyRootMotionForMove)
//                 {
//                     Vector3 motion = new Vector3(0, -9.8f * Time.deltaTime, 0);
//                     characterController.Move(motion);
//                 }
//                 return;
//             }

//             ApplyMovement(moveInput, hasMoveInput);
//         }

//         public override void Exit()
//         {
//             if (applyRootMotionForMove)
//             {
//                 animationController.ClearRootMotionAction();
//             }
//             animationController.RemoveAnimationEvent(MoveStartLoopEvent);
//             animationController.RemoveAnimationEvent(MoveStartCancelEvent);
//             animationController.RemoveAnimationEvent(RunEndEndEvent);
//             animationController.RemoveAnimationEvent("FootStep", OnFootStep);
//         }
        
//         private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
//         {
//             // 根据配置的速度缩放根运动位移，不改变动画播放速度
//             // deltaPos 已经在 AnimationController 中被缩放过了
//             deltaPos.y = -9.8f * Time.deltaTime;
//             characterController.Move(deltaPos);
//         }
        
//         /// <summary>
//         /// 更新根运动速度（根据 Walk/Run 混合）
//         /// </summary>
//         private void UpdateRootMotionSpeed()
//         {
//             float targetSpeed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
//             // 动画烘焙的基础速度：Walk=2m/s, Run=6m/s
//             float baseSpeed = Mathf.Lerp(2f, 6f, runTransition);
//             animationController.SetRootMotionSpeed(targetSpeed, baseSpeed);
//         }

//         private void PlayStartAnimation()
//         {
//             string clipName = movePhase == MovePhase.RunStart ? AnimationHelper.RunStart : AnimationHelper.WalkStart;
//             PlayerController.PlayAnimation(clipName, rootMotionAction, 1, true);
//         }

//         private void TryPlayStartAnimation()
//         {
//             AnimationClip clip = movePhase == MovePhase.RunStart ? runStartClip : walkStartClip;
//             if (clip == null)
//             {
//                 Vector2 moveInput = InputManager.Instance.GetMoveInput();
//                 if (moveInput.x != 0 || moveInput.y != 0)
//                 {
//                     EnterLoop();
//                 }
//                 else
//                 {
//                     PlayerController.ChangeState(PlayerState.Idle);
//                 }
//                 return;
//             }

//             phaseElapsed = 0f;
//             phaseTimeout = clip.length + PhaseTimeoutPadding;
//             PlayStartAnimation();
//         }

//         private void EnterLoop()
//         {
//             movePhase = MovePhase.Loop;
//             phaseElapsed = 0f;
//             phaseTimeout = 0f;
//             PlayerController.PlayBlendAnimation(AnimationHelper.Walk, AnimationHelper.Run, rootMotionAction);
//             animationController.SetBlendWeight(1 - runTransition);
            
//             // 初始化根运动速度缩放
//             if (applyRootMotionForMove)
//                 UpdateRootMotionSpeed();
//         }

//         private void EnterRunEnd()
//         {
//             movePhase = MovePhase.RunEnd;
//             runTransition = 1f;
//             if (runEndClip == null)
//             {
//                 PlayerController.ChangeState(PlayerState.Idle);
//                 return;
//             }

//             phaseElapsed = 0f;
//             phaseTimeout = runEndClip.length + PhaseTimeoutPadding;
//             PlayerController.PlayAnimation(AnimationHelper.RunEnd, rootMotionAction, 1, true);
//         }

//         private void UpdateRunTransition(bool shiftPressed)
//         {
//             if (shiftPressed)
//             {
//                 runTransition = Mathf.Clamp01(runTransition + Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
//             }
//             else
//             {
//                 runTransition = Mathf.Clamp01(runTransition - Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
//             }
//         }

//         private void ApplyMovement(Vector2 moveInput, bool hasMoveInput)
//         {
//             Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);
//             if (hasMoveInput)
//                 PlayerController.Rotate(input);

//             if (!applyRootMotionForMove)
//             {
//                 float speed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
//                 float y = Camera.main.transform.rotation.eulerAngles.y;
//                 Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
//                 Vector3 motion = Time.deltaTime * speed * moveDir;
//                 motion.y = -9.8f * Time.deltaTime;
//                 characterController.Move(motion);
//             }
//         }

//         private void OnMoveStartLoop()
//         {
//             if (movePhase != MovePhase.WalkStart && movePhase != MovePhase.RunStart)
//                 return;

//             Vector2 moveInput = InputManager.Instance.GetMoveInput();
//             if (moveInput.x == 0 && moveInput.y == 0)
//             {
//                 PlayerController.ChangeState(PlayerState.Idle);
//                 return;
//             }

//             EnterLoop();
//         }

//         private void OnMoveStartCancel()
//         {
//             if (movePhase != MovePhase.WalkStart && movePhase != MovePhase.RunStart)
//                 return;

//             Vector2 moveInput = InputManager.Instance.GetMoveInput();
//             if (moveInput.x != 0 || moveInput.y != 0)
//                 return;

//             PlayerController.ChangeState(PlayerState.Idle);
//         }

//         private void OnRunEndEnd()
//         {
//             if (movePhase != MovePhase.RunEnd)
//                 return;

//             Vector2 moveInput = InputManager.Instance.GetMoveInput();
//             if (moveInput.x != 0 || moveInput.y != 0)
//             {
//                 PlayerController.ChangeState(PlayerState.Move, true);
//                 return;
//             }

//             PlayerController.ChangeState(PlayerState.Idle);
//         }

//         private bool HandlePhaseTimeout(Vector2 moveInput, bool hasMoveInput)
//         {
//             if (phaseTimeout <= 0f)
//                 return false;

//             if (movePhase != MovePhase.WalkStart && movePhase != MovePhase.RunStart && movePhase != MovePhase.RunEnd)
//                 return false;

//             phaseElapsed += Time.deltaTime;
//             if (phaseElapsed < phaseTimeout)
//                 return false;

//             if (movePhase == MovePhase.RunEnd)
//             {
//                 if (hasMoveInput)
//                 {
//                     PlayerController.ChangeState(PlayerState.Move, true);
//                 }
//                 else
//                 {
//                     PlayerController.ChangeState(PlayerState.Idle);
//                 }
//                 return true;
//             }

//             if (hasMoveInput)
//             {
//                 EnterLoop();
//             }
//             else
//             {
//                 PlayerController.ChangeState(PlayerState.Idle);
//             }
//             return true;
//         }
//     }
// }
