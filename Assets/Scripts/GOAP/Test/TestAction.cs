using GOAP.Action;
using GOAP.Plan;
using UnityEngine;

namespace GOAP
{
    public class TestAction : GOAPActionBase
    {
        public float time;
        public float timer;

        public override void OnStart()
        {
            timer = 0;
        }

        public override GOAPRunState OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer > time)
            {
                ApplyEffect();
                return GOAPRunState.Succeed;
            }
            return GOAPRunState.Running;
        }
    }
}