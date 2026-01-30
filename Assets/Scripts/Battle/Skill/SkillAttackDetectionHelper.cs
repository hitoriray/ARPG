using Config;
using UnityEngine;

namespace Skill
{
    public static class SkillAttackDetectionHelper
    {
        private static Collider[] detectionResults = new Collider[20];
        
        public static Collider[] ShapeDetection(Transform modelTransform, AttackDetectionDataBase detectionData, AttackDetectionType detectionType, LayerMask detectionLayer)
        {
            switch (detectionType)
            {
               case AttackDetectionType.Box:
                   return BoxDetection(modelTransform, (BoxDetectionData)detectionData, detectionLayer);
               case AttackDetectionType.Sphere:
                   return SphereDetection(modelTransform, (SphereDetectionData)detectionData, detectionLayer);
               case AttackDetectionType.Fan:
                   return FanDetection(modelTransform, (FanDetectionData)detectionData, detectionLayer);
               default:
                   return null;
            }
        }

        public static Collider[] BoxDetection(Transform modelTransform, BoxDetectionData data, LayerMask detectionLayer)
        {
            ClearDetectionResults();
            Physics.OverlapBoxNonAlloc(modelTransform.TransformPoint(data.Position), data.Scale / 2, detectionResults,
                modelTransform.rotation * Quaternion.Euler(data.Rotation), detectionLayer);
            return detectionResults;
        }
        
        public static Collider[] SphereDetection(Transform modelTransform, SphereDetectionData data, LayerMask detectionLayer)
        {
            ClearDetectionResults();
            Physics.OverlapSphereNonAlloc(modelTransform.TransformPoint(data.Position), data.Radius, detectionResults, detectionLayer);
            return detectionResults;
        }
        
        public static Collider[] FanDetection(Transform modelTransform, FanDetectionData data, LayerMask detectionLayer)
        {
            ClearDetectionResults();
            Vector3 size = new(data.Radius * 2, data.Height, data.Radius * 2);
            Vector3 fanPos = modelTransform.TransformPoint(data.Position);
            Physics.OverlapBoxNonAlloc(fanPos, size / 2, detectionResults,
                modelTransform.rotation * Quaternion.Euler(data.Rotation), detectionLayer);

            // 过滤掉无效检测
            Vector3 fanForward = modelTransform.rotation * Quaternion.Euler(data.Rotation) * Vector3.forward;
            for (int i = 0; i < detectionResults.Length; i++)
            {
                if (detectionResults[i] == null)
                    break;
                // 1.过滤内半径里面的 和 外半径外面的区域
                Vector3 point = detectionResults[i].ClosestPoint(modelTransform.position);
                float dist = Vector3.Distance(point, modelTransform.position);
                bool remove = dist < data.InsideRadius || dist > data.Radius;
                if (remove == false)
                {
                    // 2.过滤不在角度范围内的
                    Vector3 dir = point - fanPos;
                    float angle = Vector3.Angle(fanForward, dir);
                    remove = angle > data.Angle * 0.5f;
                }

                if (remove)
                {
                    detectionResults[i] = null;
                }
            }
            
            return detectionResults;
        }
        
        private static void ClearDetectionResults()
        {
            for (int i = 0; i < detectionResults.Length; i++)
            {
                detectionResults[i] = null;
            }
        }
    }
}