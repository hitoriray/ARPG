using Animancer;
using RayPlayer;
using UnityEngine;

public abstract class StateBase : IState
{
    protected InputService inputServer;
    protected TimerService timerServer;
    protected PlayerController player;
    protected AnimancerComponent animancer;
    public PlayerReusableData reusableData;
    public Transform cam;
    private PlayerReusableLogic _reusableLogic;
    public PlayerReusableLogic reusableLogic
    {
        get {
            if (_reusableLogic == null)
            {
                _reusableLogic = player.ReusableLogic;
                if (_reusableLogic == null)
                {
                    return null;
                }
            }
            return _reusableLogic; 
        }
    }

    public StateBase(PlayerController player)
    {
        this.player = player;
        inputServer = player.InputService;
        timerServer = player.TimerService;
        reusableData = player.ReusableData;
        cam = player.CameraTransform;
        animancer = player.animancer;
    }
    public abstract void OnEnter();
    public abstract void OnExit();
    protected abstract void AddEventListening();
    protected abstract void RemoveEventListening();
    public abstract void OnUpdate();
    public abstract void OnAnimationUpdate();
    public abstract void OnAnimationEnd();
}