using Pathfinding;
using UnityEngine;

namespace Boss
{
    /// <summary>
    /// Boss A* 寻路中间层。
    /// 挂在 Boss GameObject 上（需同时有 Seeker 组件）。
    ///
    /// 职责：
    ///   1. 接收外部传入的目标点（SetDestination）
    ///   2. 异步向 A* 请求路径（不阻塞主线程）
    ///   3. 每帧通过 DesiredDirection 提供当前应走的方向向量（已归一化）
    ///
    /// 与行为树的关系：
    ///   行为树节点仍然独立决策（追击/后退/绕行），
    ///   本组件只负责把"目标点"转换为"能绕过障碍的合法方向"。
    /// </summary>
    [RequireComponent(typeof(Seeker))]
    public class BossAStarMover : MonoBehaviour
    {
        [Header("寻路参数")]
        [Tooltip("重新计算路径的间隔（秒）。值越小越精准，但性能开销越大。")]
        [SerializeField] private float repathInterval = 0.25f;

        [Tooltip("到达中间路径点的距离阈值（骨节点切换）。")]
        [SerializeField] private float waypointReachDist = 0.6f;

        [Tooltip("到达最终目标点的距离阈值。")]
        [SerializeField] private float endReachDist = 1.2f;

        [Tooltip("当路径无效时是否降级为直线方向（否则返回 zero）。")]
        [SerializeField] private bool fallbackToStraightLine = true;

        // ── 公开状态 ──────────────────────────────────────────
        /// <summary>当前帧应该走的方向（已归一化）。为 zero 表示已到达或无有效路径。</summary>
        public Vector3 DesiredDirection { get; private set; }

        /// <summary>是否到达了当前目标点。</summary>
        public bool ReachedDestination { get; private set; }

        /// <summary>是否持有一条有效（无错误）的路径。</summary>
        public bool HasValidPath { get; private set; }

        // ── 私有字段 ──────────────────────────────────────────
        private Seeker seeker;
        private Path currentPath;
        private int waypointIndex;
        private float nextRepathTime;
        private Vector3 currentDestination;
        private bool hasDestination;

        // ── 生命周期 ──────────────────────────────────────────
        private void Awake()
        {
            seeker = GetComponent<Seeker>();
        }

        private void OnDisable()
        {
            ClearDestination();
        }

        // ── 公开 API ──────────────────────────────────────────

        /// <summary>
        /// 设置寻路目标点。每帧调用即可（内部限定重算频率）。
        /// 调用后当帧即可读取 <see cref="DesiredDirection"/>。
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            currentDestination = destination;
            hasDestination = true;
            ReachedDestination = false;

            // 按间隔触发异步重新寻路
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;
                if (seeker != null)
                    seeker.StartPath(transform.position, destination, OnPathComplete);
            }

            UpdateDesiredDirection();
        }

        /// <summary>
        /// 清除目标，停止寻路。DesiredDirection 将变为 zero。
        /// </summary>
        public void ClearDestination()
        {
            hasDestination = false;
            currentPath = null;
            HasValidPath = false;
            DesiredDirection = Vector3.zero;
            ReachedDestination = false;
            nextRepathTime = 0f;
        }

        // ── 私有方法 ──────────────────────────────────────────

        private void OnPathComplete(Path p)
        {
            if (p.error)
            {
                // 路径计算失败，维持上一条路径或降级
                HasValidPath = false;
                RayDebug.Warn($"[BossAStarMover] 路径计算失败: {p.errorLog}");
                return;
            }

            currentPath = p;
            waypointIndex = 0;
            HasValidPath = true;
        }

        private void UpdateDesiredDirection()
        {
            if (!hasDestination)
            {
                DesiredDirection = Vector3.zero;
                return;
            }

            // 无有效路径时降级
            if (!HasValidPath || currentPath == null || currentPath.error ||
                currentPath.vectorPath == null || currentPath.vectorPath.Count == 0)
            {
                DesiredDirection = fallbackToStraightLine
                    ? GetFallbackDirection()
                    : Vector3.zero;
                return;
            }

            var path = currentPath.vectorPath;

            // 推进到下一个合适的路径点
            AdvanceWaypoint(path);

            if (waypointIndex >= path.Count)
            {
                DesiredDirection = Vector3.zero;
                ReachedDestination = true;
                return;
            }

            // 计算方向向量（水平，不带 Y）
            Vector3 target = path[waypointIndex];
            Vector3 toWaypoint = target - transform.position;
            toWaypoint.y = 0f; // 当前阶段只做平面寻路，不处理爬墙

            DesiredDirection = toWaypoint.sqrMagnitude > 0.0001f
                ? toWaypoint.normalized
                : Vector3.zero;
        }

        private void AdvanceWaypoint(System.Collections.Generic.List<Vector3> path)
        {
            while (waypointIndex < path.Count)
            {
                Vector3 waypoint = path[waypointIndex];
                bool isLast = waypointIndex == path.Count - 1;
                float threshold = isLast ? endReachDist : waypointReachDist;

                // 只检查水平距离，避免高度差影响判定
                Vector3 toWaypoint = waypoint - transform.position;
                toWaypoint.y = 0f;

                if (toWaypoint.magnitude < threshold)
                    waypointIndex++;
                else
                    break;
            }
        }

        private Vector3 GetFallbackDirection()
        {
            Vector3 dir = currentDestination - transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        // ── Gizmo 调试 ────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!HasValidPath || currentPath == null || currentPath.vectorPath == null)
                return;

            var path = currentPath.vectorPath;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);

            if (waypointIndex < path.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(path[waypointIndex], 0.25f);
            }

            // 当前方向
            if (DesiredDirection.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, DesiredDirection * 2f);
            }
        }
#endif
    }
}
