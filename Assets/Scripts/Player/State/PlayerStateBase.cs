using JKFrame;

namespace Player.State
{
    public abstract class PlayerStateBase : StateBase
    {
        protected PlayerController PlayerController;
        
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            PlayerController = owner as PlayerController;
        }
    }
}