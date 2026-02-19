public class StateMachineBase
{
    public IState currentState;
    public IState lastState;

    /// <summary>
    /// 切换状态
    /// </summary>
    public virtual void ChangeState(IState targetState)
    {
        currentState?.OnExit();
        lastState = currentState;
        currentState = targetState;
        currentState?.OnEnter();
    }

    public void OnAnimationEnd()
    {
        currentState.OnAnimationEnd();
    }

    public void OnUpdate()
    {
        currentState?.OnUpdate();
    }

    /// <summary>
    /// 按动画帧来更新
    /// </summary>
    public void OnAnimationUpdate()
    {
        currentState?.OnAnimationUpdate();
    }
}