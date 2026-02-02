using Config;
using UnityEngine;

#if UNITY_EDITOR
namespace Skill
{
    public static class SkillGizmosTool
    {
        public static void DrawDetectionGizmos(SkillAttackDetectionEvent attackDetectionEvent, SkillPlayer skillPlayer)
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Transform modelTransform = skillPlayer.ModelTransform != null ? skillPlayer.ModelTransform : skillPlayer.transform;
            Matrix4x4 rotationMat = new();
            switch (attackDetectionEvent.AttackDetectionType)
            {
                case AttackDetectionType.Weapon:
                    WeaponDetectionData weaponDetectionData = (WeaponDetectionData)attackDetectionEvent.AttackDetectionData;
                    if (!string.IsNullOrEmpty(weaponDetectionData.WeaponName) &&
                        skillPlayer.WeaponDict.TryGetValue(weaponDetectionData.WeaponName, out var weaponController))
                    {
                        Collider collider = weaponController.GetComponentInChildren<Collider>();
                        rotationMat.SetTRS(collider.transform.position, collider.transform.rotation, collider.transform.localScale);
                        Gizmos.matrix = rotationMat;
                        if (collider is BoxCollider boxCollider)
                        {
                            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                        }
                        else if (collider is SphereCollider sphereCollider)
                        {
                            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                        }
                    }
                    break;
                case AttackDetectionType.Box:
                    BoxDetectionData boxDetectionData = (BoxDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos = modelTransform.TransformPoint(boxDetectionData.Position);
                    Quaternion rot = Quaternion.Euler(boxDetectionData.Rotation) * modelTransform.rotation;
                    rotationMat.SetTRS(pos, rot, Vector3.one);
                    Gizmos.matrix = rotationMat;
                    Gizmos.DrawCube(Vector3.zero, boxDetectionData.Scale);
                    break;
                case AttackDetectionType.Sphere:
                    SphereDetectionData sphereDetectionData = (SphereDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos2 = modelTransform.TransformPoint(sphereDetectionData.Position);
                    Gizmos.DrawSphere(pos2, sphereDetectionData.Radius);
                    break;
                case AttackDetectionType.Fan:
                    FanDetectionData fanDetectionData = (FanDetectionData)attackDetectionEvent.AttackDetectionData;
                    Vector3 pos3 = modelTransform.TransformPoint(fanDetectionData.Position);
                    Quaternion rot3 = Quaternion.Euler(fanDetectionData.Rotation) * modelTransform.rotation;
                    Mesh mesh = MeshGenerator.GenerateFanMesh(fanDetectionData.InsideRadius, fanDetectionData.Radius,
                        fanDetectionData.Height, fanDetectionData.Angle);
                    Gizmos.DrawMesh(mesh, pos3, rot3);
                    break;
            }
            
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
#endif