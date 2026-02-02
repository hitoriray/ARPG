# Buff 系统 ECS 化 - 使用文档

## 📦 系统架构

### 核心组件 (Components)
- **Buff**: Buff 基础信息（ID、Config、Caster、Target、事件标志）
- **BuffStack**: Buff 堆叠信息（每层的施法者和剩余时间）
- **BuffProperty**: Buff 属性（持续时间、最大层数、叠加模式、溢出策略）
- **BuffList**: 实体的 Buff 列表（挂载在目标实体上）
- **Tick**: 周期效果组件（DOT 等）
- **Attribute**: 实体属性（攻击、防御、生命等，支持修改器栈）

### 核心系统 (Systems)
- **BuffSystem**: Buff 生命周期管理（时间更新、过期处理）
- **TickSystem**: 周期效果处理（DOT、HOT 等）

### 核心工具 (Helpers)
- **BuffHelper**: Buff 的创建、添加、移除、叠加等工具方法

### 核心处理器 (Process)
- **BuffProcess**: Buff 的事件处理（OnCreate、OnTick、OnDeath 等）

---

## 🚀 快速开始

### 1. 创建 Buff 配置

在 Unity 编辑器中：
1. 右键 → Create → Config → BuffConfig
2. 配置 Buff 属性：

```yaml
# 示例：中毒 Buff（DOT效果）
buffName: "中毒"
description: "持续损失生命值"
maxStack: 999                    # 可叠加999层
stackMode: IndependentDuration   # 每层独立计时
duration: 5.0                    # 每层持续5秒
tickInterval: 1.0                # 每秒触发一次
tickCount: -1                    # 无限次数

# 周期效果：每秒扣血
periodicEffect:
  type: Hp
  value: -5

# 属性修正：可选
AttrModifiers:
  - type: Speed
    value: -0.1      # 速度降低10%
    mode: Percent
```

```yaml
# 示例：狂暴 Buff（增益效果）
buffName: "狂暴"
description: "大幅提升攻击力"
maxStack: 1                      # 不可叠加
stackMode: RefreshDuration       # 重新施加时刷新时间
duration: 10.0

# 开始效果：立即生效
startEffect:
  type: AttackFixed
  value: 50          # 攻击力+50

# 属性修正：推荐用这个
AttrModifiers:
  - type: Attack
    value: 50
    mode: Fixed
  - type: Attack
    value: 0.2       # 攻击力+20%
    mode: Percent
```

---

### 2. 添加 Buff 到实体

```csharp
using Battle.ECS.Core.Helper;
using Config;

// 获取 Buff 配置
var buffConfig = ResSystem.LoadAsset<BuffConfig>("中毒Buff");

// 添加 Buff 到目标
var buffEntity = BuffHelper.AddBuff(
    context: battleContext,
    track: "SkillDamage",        // 追踪标签（用于调试）
    caster: attackerEntity,      // 施法者
    target: targetEntity,        // 目标
    buffConfig: buffConfig,      // Buff 配置
    stackCount: 3                // 初始层数
);

if (buffEntity.IsAlive())
{
    Debug.Log("Buff 添加成功！");
}
```

---

### 3. 查询实体的 Buff

```csharp
// 检查是否有 BuffList
if (targetEntity.Has<BuffList>())
{
    ref var buffList = ref targetEntity.Get<BuffList>();

    // 检查是否有特定 Buff
    bool hasPoisonBuff = buffList.HasBuff(buffId: 1001);

    // 获取特定 Buff
    var poisonBuffEntity = buffList.GetBuff(buffId: 1001);

    if (poisonBuffEntity.IsAlive())
    {
        ref var buffStack = ref poisonBuffEntity.Get<BuffStack>();
        Debug.Log($"中毒层数: {buffStack.Value.Count}");
    }
}
```

---

### 4. 移除 Buff

```csharp
// 方法1：标记死亡（推荐）
var buffEntity = buffList.GetBuff(buffId: 1001);
if (buffEntity.IsAlive())
{
    buffEntity.Add(new Death());  // 下一帧自动清理
}

// 方法2：减少堆叠
BuffHelper.RemoveStack(context, buffEntity, removeCount: 1);

// 方法3：清空目标所有 Buff
if (targetEntity.Has<BuffList>())
{
    ref var buffList = ref targetEntity.Get<BuffList>();
    foreach (var buffEntity in buffList.Value)
    {
        if (buffEntity.IsAlive())
            buffEntity.Add(new Death());
    }
}
```

---

## 🎯 高级功能

### 1. 叠加模式 (StackMode)

| 模式 | 行为 | 适用场景 |
|------|------|----------|
| **RefreshDuration** | 叠加时刷新时间，到期全部移除 | 护盾、增益状态 |
| **IndependentDuration** | 每层独立计时 | DOT、多人叠加 |
| **SequentialDuration** | 逐层过期 | 充能技能 |
| **Permanent** | 永久有效 | 被动光环 |

### 2. 溢出策略 (OverflowPolicy)

当堆叠超过 `maxStack` 时：

| 策略 | 行为 |
|------|------|
| **ReplaceOldest** | 替换最早添加的那一层 |
| **ReplaceLowestPriority** | 替换优先级最低的（TODO） |
| **DiscardNewest** | 丢弃新添加的 |

### 3. 属性修改器

```csharp
// 在 BuffConfig 中配置
AttrModifiers:
  - type: Attack
    value: 50
    mode: Fixed        # 固定加成 +50

  - type: Attack
    value: 0.2
    mode: Percent      # 百分比加成 +20%
```

**自动回退机制**：
- Buff 添加时：`Attribute.AddModifier()`
- Buff 移除时：`Attribute.RemoveModifier()`
- 使用修改器栈，精确追踪每个修改

---

## 🔧 集成到技能系统

### 示例：技能附带 Buff

```csharp
public class SkillBehaviourBase
{
    protected void ApplySkillBuff(Entity caster, Entity target, SkillConfig skillConfig)
    {
        if (skillConfig.ApplyBuff != null)
        {
            var context = GetBattleContext();
            BuffHelper.AddBuff(
                context,
                $"Skill_{skillConfig.skillId}",
                caster,
                target,
                skillConfig.ApplyBuff,
                stackCount: 1
            );
        }
    }
}
```

---

## 🎨 UI 集成

### 示例：显示 Buff 图标

```csharp
public class UI_BuffPanel : MonoBehaviour
{
    [SerializeField] private UI_BuffSlot buffSlotPrefab;
    private Dictionary<Entity, UI_BuffSlot> buffSlots = new();

    public void UpdateBuffList(Entity targetEntity)
    {
        if (!targetEntity.Has<BuffList>()) return;

        ref var buffList = ref targetEntity.Get<BuffList>();

        foreach (var buffEntity in buffList.Value)
        {
            if (!buffEntity.IsAlive()) continue;

            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();
            ref var buffProperty = ref buffEntity.Get<BuffProperty>();

            // 创建或更新 UI 槽位
            if (!buffSlots.TryGetValue(buffEntity, out var slot))
            {
                slot = Instantiate(buffSlotPrefab, transform);
                buffSlots[buffEntity] = slot;
            }

            // 更新显示
            slot.SetIcon(buff.Config.icon);
            slot.SetStack(buffStack.Value.Count);

            // 计算剩余时间百分比
            FP remainingTime = buffStack.Value.Count > 0
                ? buffStack.Value[^1].RemainingTime
                : FP.Zero;
            float progress = (float)(remainingTime / buffProperty.Duration);
            slot.SetProgress(progress);
        }
    }
}
```

---

## 🐛 调试工具

### 1. 使用调试窗口
- 菜单：游戏工具 → 战斗调试 → Buff调试
- 功能：
  - 添加任意 Buff 到玩家
  - 查看当前所有 Buff
  - 清除所有 Buff

### 2. 运行时查看 Buff

```csharp
// 在任意 MonoBehaviour 中添加
[ContextMenu("打印所有Buff")]
private void DebugPrintBuffs()
{
    var context = GetBattleContext();
    var playerEntity = context.PlayerIndex.GetEntity(0);

    if (!playerEntity.Has<BuffList>()) return;

    ref var buffList = ref playerEntity.Get<BuffList>();
    Debug.Log($"玩家当前有 {buffList.Value.Count} 个Buff:");

    foreach (var buffEntity in buffList.Value)
    {
        ref var buff = ref buffEntity.Get<Buff>();
        ref var buffStack = ref buffEntity.Get<BuffStack>();
        Debug.Log($"  - {buff.Config.buffName} x{buffStack.Value.Count}层");
    }
}
```

---

## ⚠️ 注意事项

### 1. 实体必须有 Attribute 组件
如果 Buff 使用了 `AttrModifiers`，目标实体必须有 `Attribute` 组件。

```csharp
// 创建实体时添加 Attribute
var entity = context.World.Create(
    new Attribute(
        attack: FP.FromFloat(100),
        maxHp: FP.FromFloat(1000),
        maxMp: FP.FromFloat(100),
        defense: FP.FromFloat(50),
        speed: FP.FromFloat(5)
    ),
    new Health(FP.FromFloat(1000))
);
```

### 2. DOT 效果需要 Tick 组件
系统会自动添加，但配置必须正确：
- `tickInterval > 0`：才会添加 Tick 组件
- `periodicEffect != null`：周期效果才会执行

### 3. Buff 移除时机
- **方式1**：`entity.Add<Death>()`（推荐）
  - 触发 `BuffProcess.OnDeath()`
  - 自动执行 `endEffect`
  - 自动清理属性修改器

- **方式2**：`BuffHelper.RemoveStack()`
  - 只减少层数
  - 层数为 0 时才触发死亡

### 4. 事件计数器优化
`BuffList` 的事件计数器用于性能优化：
- 只在计数 > 0 时才遍历 Buff 列表
- 示例：
```csharp
if (buffList.HurtEvent > 0)  // 快速判断
{
    // 遍历处理受伤事件
}
```

---

## 📈 性能优化

1. **使用 UnsafeList**：避免 GC
2. **InlineEntityQuery**：高性能批量查询
3. **事件计数器**：避免无效遍历
4. **修改器栈**：精确回退，无需遍历

---

## 🔗 相关文件

| 文件路径 | 说明 |
|---------|------|
| `Battle/ECS/Core/Helper/BuffHelper.cs` | Buff 工具类 |
| `Battle/ECS/Core/Process/BuffProcess.cs` | Buff 处理器 |
| `Battle/ECS/Core/Features/System/BuffSystem.cs` | Buff 系统 |
| `Battle/ECS/Core/Features/System/TickSystem.cs` | Tick 系统 |
| `Battle/ECS/Core/Features/Components/Buff/` | Buff 相关组件 |
| `Battle/ECS/Examples/BuffSystemExample.cs` | 使用示例 |
| `Editor/BattleDebug/BuffDebugWindow.cs` | 调试窗口 |

---

## 🎓 学习资源

参考项目：
- **kaji-client**: 商业级 Buff 系统实现
- **Arch ECS**: 高性能 ECS 框架

---

如有问题，请查看示例代码或联系技术支持。
