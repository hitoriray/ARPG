using UnityEngine;

namespace Boss
{
    public sealed class BossAIContext
    {
        public Transform Target { get; set; }
        public Vector3 MoveDir { get; private set; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float MoveSpeedParam { get; private set; } = 0f; // 0 idle, 1 walk, 2 run
        public bool FaceTarget { get; set; } = true;
        public bool HasEvasionDirOverride { get; private set; }
        private Vector3 evasionDirOverride;

        public bool HasMove => MoveDir.sqrMagnitude > 0.0001f;

        public void SetMove(Vector3 worldDir, float moveSpeedMultiplier = 1f, float moveSpeedParam = 1f)
        {
            MoveDir = worldDir;
            MoveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier);
            MoveSpeedParam = moveSpeedParam;
        }

        public void ClearMove()
        {
            MoveDir = Vector3.zero;
            MoveSpeedMultiplier = 1f;
            MoveSpeedParam = 0f;
        }

        public void SetEvasionDir(Vector3 worldDir)
        {
            evasionDirOverride = worldDir;
            HasEvasionDirOverride = worldDir.sqrMagnitude > 0.0001f;
        }

        public bool TryConsumeEvasionDir(out Vector3 worldDir)
        {
            if (HasEvasionDirOverride)
            {
                worldDir = evasionDirOverride;
                evasionDirOverride = Vector3.zero;
                HasEvasionDirOverride = false;
                return true;
            }

            worldDir = Vector3.zero;
            return false;
        }
    }
}
