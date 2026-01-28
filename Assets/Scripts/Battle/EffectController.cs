using JKFrame;
using UnityEngine;

public class EffectController : MonoBehaviour
{
    public float destroyTime;
    private float destroyTimer;

    public void Init()
    {
        destroyTimer = destroyTime;
    }

    private void Update()
    {
        destroyTimer -= Time.deltaTime;
        if (destroyTimer <= 0)
        {
            this.GameObjectPushPool();
        }
    }
}