using Battle.ECS.Component;
using Config;
using FixMath;
using UnityEngine;

namespace Battle.ECS.View.Helper
{
    public static class VfxEmitterHelper
    {
        public static bool EmitSkillVfx(Transform modelTransform, SkillEffectEvent effectEvent, int frameRate)
        {
            if (modelTransform == null || effectEvent == null || effectEvent.Prefab == null)
                return false;

            if (BattleEcsRunner.Instance == null || BattleEcsRunner.Instance.Context == null)
                return false;

            var position = modelTransform.TransformPoint(effectEvent.Position);
            var rotation = Quaternion.Euler(modelTransform.eulerAngles + effectEvent.Rotation);
            var duration = frameRate > 0 ? (float)effectEvent.Duration / frameRate : 0f;

            BattleEcsRunner.Instance.Context.World.Create(
                new VfxData
                {
                    Prefab = effectEvent.Prefab,
                    Position = position,
                    Rotation = rotation,
                    Scale = effectEvent.Scale,
                    AutoDestroy = effectEvent.AutoDestroy,
                    Duration = (FP)duration,
                    LookAtCamera = false
                });
            return true;
        }

        public static bool EmitHitVfx(Vector3 position, GameObject prefab, bool lookAtCamera)
        {
            if (prefab == null) return false;
            if (BattleEcsRunner.Instance == null || BattleEcsRunner.Instance.Context == null)
                return false;

            BattleEcsRunner.Instance.Context.World.Create(
                new VfxData
                {
                    Prefab = prefab,
                    Position = position,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,
                    AutoDestroy = true,
                    Duration = FP.Zero,
                    LookAtCamera = lookAtCamera
                });
            return true;
        }
    }
}
