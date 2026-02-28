using JKFrame;
using UnityEngine;
using UnityEngine.AI;

namespace Npc
{
    public class NpcController : CharacterControllerBase, IStateMachineOwner
    {
        public NavMeshAgent navMeshAgent;
        public float hpDownSpeed = 1;
        public StateMachine stateMachine;

        private void Start()
        {
            stateMachine = new();
            stateMachine.Init(this);
        }

        public void PlayAnimation(string animationName)
        {
            animator.CrossFadeInFixedTime(animationName, 0.25f);
        }

        public void StartMove()
        {
            navMeshAgent.enabled = true;
        }

        public void StopMove()
        {
            navMeshAgent.enabled = false;
        }

        public void SetDestination(Vector3 pos)
        {
            navMeshAgent.SetDestination(pos);
        }
    }
}