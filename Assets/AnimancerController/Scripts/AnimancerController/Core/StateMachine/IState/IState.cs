public interface IState
{
    void OnEnter();
    void OnUpdate();
    void OnAnimationUpdate();
    void OnExit();
    void OnAnimationEnd();
}