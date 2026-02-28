using UnityEngine;

namespace Boss
{
    /// <summary>
    /// Boss 攀爬障碍物检测器。
    /// 挂在 Boss GameObject 上，负责检测前方是否存在可攀越的障碍物。
    /// 算法直接移植自 PlayerReusableLogic.GetWallHight()，独立运行不依赖 PlayerController。
    /// </summary>
    public class BossClimbDetector : MonoBehaviour
    {
        [Header("检测参数（与玩家对齐）")]
        [Tooltip("最低检测起始高度（角色脚底偏移）")]
        [SerializeField] private float minDetectionHeight = 0.3f;
        [Tooltip("最高检测上限高度")]
        [SerializeField] private float maxDetectionHeight = 3.2f;
        [Tooltip("射线水平检测距离")]
        [SerializeField] private float detectionDistance = 1f;
        [Tooltip("垂直采样射线数量")]
        [SerializeField] private int samplingCount = 30;
        [Tooltip("翻越模式最大遮挡检测距离")]
        [SerializeField] private float vaultMaxDistance = 0.45f;
        [Tooltip("角色与墙面最大允许夹角（超过则不攀爬）")]
        [SerializeField] private float maxFaceAngle = 45f;

        // ── 输出结果（供 BossClimbState 读取）────────────────────
        public RaycastHit WallHit { get; private set; }
        public Vector3 VaultPos { get; private set; }
        public ObstructHeightLevel HeightLevel { get; private set; }
        public ClimbType ClimbType { get; private set; }

        private LayerMask groundLayer;

        private void Awake()
        {
            // 从挂载的 CharacterControllerBase 获取地面层级
            var ccBase = GetComponent<CharacterControllerBase>();
            if (ccBase != null)
                groundLayer = ccBase.whatIsGround;
        }

        /// <summary>
        /// 检测前方是否有可攀爬的障碍物。
        /// </summary>
        /// <param name="forwardDir">检测方向（通常为 Boss 的 transform.forward）</param>
        /// <returns>true 表示检测到可攀爬障碍物，结果输出到 WallHit / VaultPos / HeightLevel / ClimbType</returns>
        public bool TryDetectClimbable(Vector3 forwardDir)
        {
            float vaultHeight = 0f;
            float obstructHeight = 0f;
            RaycastHit hit = GetWallHeight(forwardDir, ref vaultHeight, ref obstructHeight);

            // 没有检测到墙壁
            if (hit.point == Vector3.zero)
                return false;

            // 角色正面与墙面角度太大（侧面撞墙，不攀爬）
            float angle = Vector3.Angle(-forwardDir, hit.normal);
            if (angle > maxFaceAngle)
                return false;

            // 计算翻越起点
            Vector3 vaultStartPos = new Vector3(hit.point.x, vaultHeight, hit.point.z);
            WallHit = hit;
            VaultPos = vaultStartPos;

            // 根据高度判断攀爬类型（与玩家 OnJump 中的分支对齐）
            if (obstructHeight >= 2f && obstructHeight < 2.5f)
            {
                // 中高攀爬：Boss 跳跃处理（暂不支持）
                return false;
            }
            else if (obstructHeight >= 1f && obstructHeight < 1.7f)
            {
                HeightLevel = ObstructHeightLevel.Medium;
                DetermineClimbOrVault(vaultStartPos, hit);
            }
            else if (obstructHeight >= 0.35f && obstructHeight < 1f)
            {
                HeightLevel = ObstructHeightLevel.LowMedium;
                DetermineClimbOrVault(vaultStartPos, hit);
            }
            else if (obstructHeight < 0.35f)
            {
                HeightLevel = ObstructHeightLevel.Low;
                ClimbType = ClimbType.Climb;
            }
            else
            {
                return false;
            }

            return true;
        }

        // ── 私有方法 ──────────────────────────────────────────

        /// <summary>
        /// 垂直多层射线采样，找出前方墙壁的最高碰撞点。
        /// 完整移植自 PlayerReusableLogic.GetWallHight()。
        /// </summary>
        private RaycastHit GetWallHeight(Vector3 direction, ref float vaultHeight, ref float obstructHeight)
        {
            RaycastHit currentHit = default;
            Vector3 highestSamplePos = Vector3.zero;
            Vector3 startPos = transform.position + Vector3.up * minDetectionHeight;
            float stepHeight = (maxDetectionHeight - minDetectionHeight) / samplingCount;

            for (int i = 0; i <= samplingCount + 1; i++)
            {
                Vector3 samplePos = startPos + Vector3.up * stepHeight * i;
                if (Physics.Raycast(samplePos, direction, out var hitInfo, detectionDistance, groundLayer))
                {
                    currentHit = hitInfo;
                    highestSamplePos = samplePos;
                }
            }

            obstructHeight = currentHit.point.y - transform.position.y;

            if (obstructHeight >= maxDetectionHeight || obstructHeight <= 0f)
                return default;

            // 精确取墙面碰撞点
            if (Physics.Raycast(highestSamplePos, -currentHit.normal, out var finalHit, detectionDistance, groundLayer))
            {
                currentHit.point = finalHit.point;
                vaultHeight = currentHit.point.y + stepHeight;
            }
            else
            {
                return default;
            }

            return currentHit;
        }

        /// <summary>
        /// 判断是攀爬（Climb）还是翻越（Vault）。
        /// 完整移植自 PlayerReusableLogic.VaultOrClimb()。
        /// </summary>
        private void DetermineClimbOrVault(Vector3 vaultStartPos, RaycastHit wallHit)
        {
            if (Physics.Raycast(vaultStartPos, -wallHit.normal, vaultMaxDistance, groundLayer))
            {
                ClimbType = ClimbType.Climb;
            }
            else
            {
                Vector3 vaultDetectionPos = vaultStartPos + (-wallHit.normal * vaultMaxDistance);
                ClimbType = Physics.Raycast(vaultDetectionPos, Vector3.down, 0.25f)
                    ? ClimbType.Climb
                    : ClimbType.Vault;
            }
        }

        // ── Gizmo ────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            Vector3 startPos = transform.position + Vector3.up * minDetectionHeight;
            float stepHeight = (maxDetectionHeight - minDetectionHeight) / samplingCount;

            Gizmos.color = Color.yellow;
            for (int i = 0; i <= samplingCount + 1; i++)
            {
                Vector3 samplePos = startPos + Vector3.up * stepHeight * i;
                Gizmos.DrawRay(samplePos, transform.forward * detectionDistance);
            }
        }
#endif
    }
}
