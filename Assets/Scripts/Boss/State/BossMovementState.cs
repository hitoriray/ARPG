using UnityEngine;

namespace Boss
{
    public abstract class BossMovementState : BossStateBase
    {
        protected BossMovementState(BossController boss) : base(boss) { }

        protected void UpdateRotation(Vector3 worldDir, float rotationSpeed = 0f)
        {
            if (worldDir.sqrMagnitude <= 0.0001f)
                return;

            if (rotationSpeed <= 0f)
                rotationSpeed = boss.RotateSpeed;

            Vector3 dir = worldDir.normalized;
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, Quaternion.LookRotation(dir),
                Time.deltaTime * rotationSpeed);

            if (reusableData != null)
            {
                reusableData.targetDir = dir;
                float angle = ToolFunction.GetDeltaAngle(boss.transform, dir);
                reusableData.targetAngle.Value = angle;
                reusableData.rotationValueParameter.TargetValue = angle * Mathf.Deg2Rad;
            }
        }

        protected void UpdateSpeedParam(float value)
        {
            if (reusableData != null)
                reusableData.speedValueParameter.TargetValue = value;
        }
    }
}
