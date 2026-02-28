using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 角色成长曲线配置（ScriptableObject）。
    /// 挂在 CharacterConfig 里，定义该角色等级→属性倍率、升级所需经验等。
    /// 创建路径：Create → Config/Character/LevelGrowthConfig
    /// </summary>
    [CreateAssetMenu(fileName = "LevelGrowthConfig", menuName = "Config/Character/LevelGrowthConfig")]
    public class LevelGrowthConfig : ScriptableObject
    {
        [LabelText("最大等级")]
        [MinValue(1)]
        public int MaxLevel = 100;

        [Title("属性成长曲线（X轴=等级归一化0~1，Y轴=属性倍率）")]
        [InfoBox("倍率=1 时属性与 CharacterConfig 基础值相同。等级越高倍率越大。")]
        [LabelText("HP 成长曲线")]
        public AnimationCurve HpGrowthCurve = DefaultCurve();

        [LabelText("攻击力 成长曲线")]
        public AnimationCurve AttackGrowthCurve = DefaultCurve();

        [LabelText("防御力 成长曲线")]
        public AnimationCurve DefenseGrowthCurve = DefaultCurve();

        [Title("经验需求（升每一级需要多少经验）")]
        [LabelText("升级经验曲线（X=等级归一化，Y=该级所需经验量）")]
        [InfoBox("例如 Y=1 → 各级经验均等于 BaseExpPerLevel；Y 值越大升级越难。")]
        public AnimationCurve ExpRequiredCurve = DefaultCurve();

        [LabelText("基础每级经验量")]
        [MinValue(1)]
        public long BaseExpPerLevel = 1000;

        // ── 工具方法 ──────────────────────────────────────────────

        /// <summary>
        /// 计算 level 级时，HP 基础值相对 1 级的倍率。
        /// </summary>
        public float GetHpMultiplier(int level) => EvalCurve(HpGrowthCurve, level);

        /// <summary>
        /// 计算 level 级时，攻击力基础值相对 1 级的倍率。
        /// </summary>
        public float GetAttackMultiplier(int level) => EvalCurve(AttackGrowthCurve, level);

        /// <summary>
        /// 计算 level 级时，防御力基础值相对 1 级的倍率。
        /// </summary>
        public float GetDefenseMultiplier(int level) => EvalCurve(DefenseGrowthCurve, level);

        /// <summary>
        /// 获取从 level 级升到 level+1 级所需的经验值。
        /// </summary>
        public long GetExpRequiredForNextLevel(int level)
        {
            if (level >= MaxLevel) return long.MaxValue; // 已满级
            float t = Mathf.Clamp01((float)level / MaxLevel);
            float multiplier = ExpRequiredCurve.Evaluate(t);
            return Math.Max(1L, (long)(BaseExpPerLevel * multiplier));
        }

        /// <summary>
        /// 计算当前累计经验对应的等级（从1开始）。
        /// 返回等级以及该等级下的剩余经验。
        /// </summary>
        public (int level, long remainExp) CalcLevelFromExp(long totalExp)
        {
            int lv = 1;
            long remaining = totalExp;
            while (lv < MaxLevel)
            {
                long need = GetExpRequiredForNextLevel(lv);
                if (remaining < need) break;
                remaining -= need;
                lv++;
            }
            return (lv, remaining);
        }

        // ── 内部 ──────────────────────────────────────────────────
        private float EvalCurve(AnimationCurve curve, int level)
        {
            float t = Mathf.Clamp01((float)(level - 1) / Mathf.Max(1, MaxLevel - 1));
            return Mathf.Max(0.01f, curve.Evaluate(t));
        }

        private static AnimationCurve DefaultCurve()
        {
            // 线性从 1 → 3（100 级是 1 级属性的 3 倍，可在 Inspector 里自由修改）
            return AnimationCurve.Linear(0f, 1f, 1f, 3f);
        }
    }
}
