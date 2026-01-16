using JKFrame;

namespace Player.State
{
    public abstract class PlayerStateBase : StateBase
    {
        protected PlayerController PlayerController;
        
        public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
        {
            base.Init(owner, stateType, stateMachine);
            PlayerController = owner as PlayerController;
        }
    }
}