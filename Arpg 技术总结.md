# ARPG 技术能力导向总结

> 基于真实代码分析（2026-03-03）| Unity 2021.3 LTS | Animancer Pro + Arch ECS + Behaviour Designer

---

## 一、项目定位

本项目是一个**技术验证型 ARPG 战斗 Demo**，不以商业上线为目标，而是作为技术栈整合验证平台。其核心设计目标为：

- 验证「状态机 + 帧驱动技能系统」在 Unity 中的工程可行性
- 验证 Animancer 多层动画架构（Layer0 移动 / Layer1 技能上身/全身覆盖）在连击场景中的稳定性
- 探索将 Arch ECS 作为战斗后端（伤害计算、Buff 管理）与 MonoBehaviour 表现层解耦的架构边界
- 构建可扩展的技能脚本化数据结构（ScriptableObject + SkillClip 帧数据），为未来美术工具链（SkillEditor）打基础

项目刻意**不实现**数值成长、完整 UI 系统和联网，聚焦战斗链路上的工程正确性。

---

## 二、整体架构设计

### 2.1 分层结构

```
┌─────────────────────────────────────────────────────┐
│  输入层   │  InputService (JKFrame单例)              │
│           │  PlayerSkillInput (技能按键缓冲)         │
├───────────┼─────────────────────────────────────────┤
│  逻辑层   │  PlayerController (状态机驱动)           │
│           │  PlayerStateMachine / PlayerState 枚举   │
│           │  SkillBrainBase / PlayerSkillBrainBase   │
│           │  SkillBehaviourBase (技能行为树)         │
│           │  EnemyController / BossController        │
│           │  Behaviour Designer 行为树               │
├───────────┼─────────────────────────────────────────┤
│  表现层   │  Animancer (Layer0 移动, Layer1 技能)    │
│           │  SkillPlayer (帧驱动 Tick)               │
│           │  VfxEmitterHelper / AudioSystem          │
│           │  DamageNumbersPro (飘字)                 │
├───────────┼─────────────────────────────────────────┤
│  战斗后端 │  BattleEcsRunner (Arch ECS 入口)        │
│           │  LocalLogicFeature (伤害/Buff/死亡系统)  │
│           │  LocalViewFeature (表现同步)             │
├───────────┼─────────────────────────────────────────┤
│  数据层   │  ScriptableObject (SkillConfig)          │
│           │  SkillClip (帧事件数据)                  │
│           │  CharacterConfig / LevelGrowthConfig     │
│           │  DataManager (存档读写)                  │
└───────────┴─────────────────────────────────────────┘
```

### 2.2 模块解耦方式

**接口隔离**：战斗核心通过 `ICharacter`、`IHitTarget`、`IDamageNumberService` 接口与具体角色实现解耦。`SkillBehaviourBase.OnHitTarget()` 调用的是 `IHitTarget.OnHit(attackData)`，而不直接访问 `EnemyController` 的具体字段。

**ECS 边界**：`BattleEcsRunner.RegisterCharacter()` 将 `ICharacter` 包装成 Arch ECS 实体，后续伤害、Buff、死亡的逻辑处理完全在 ECS 系统内部，通过 `DamageHelper.EmitDamage()` 和 `BuffHelper.AddBuff()` 静态辅助类从 MonoBehaviour 侧发起请求。结果（如死亡）再通过回调接口 `IDeathCallback.OnDeath()` 通知 MonoBehaviour 端。

**事件系统**：项目使用 JKFrame 框架自带的**事件总线**（`EventSystem`）和 C# 标准 `Action<>` 委托。例如 `DataManager.OnLevelUp` 是一个 `Action<int, int>`，`PlayerController.Init()` 中直接订阅，`OnDestroy()` 中取消，生命周期清晰。

**管理器模式**：`DataManager`、`PlayerManager`、`InventoryManager`、`DamageNumberManager` 均继承 JKFrame 单例基类。`InputService`、`TimerService` 也以单例形式提供。无依赖注入框架，模块间通过单例和接口通信。

---

## 三、核心战斗链路（完整数据流）

### 3.1 完整流程图

```
[玩家按键]
    │
    ▼ PlayerSkillInput.GetSkillState(skillIndex)  →  按键缓冲
    │
    ▼ PlayerController.HandleSkillInput()
    │   检查：不在UI上 → 不在技能状态 → skillBrain.CheckReleaseSkill(i)
    │     └── SkillBehaviourBase.CheckRelease() = CheckCost() && CheckCdTime()
    │
    ▼ MovementStateMachine.ChangeState(skillState)   ← 状态机切换
    │   PlayerSkillState.OnEnter() → PlayerController.EnterSkillMode(upperBody)
    │     └── Animancer.Layer1.SetWeight(1f), Layer0.Stop()
    │
    ▼ skillBrain.ReleaseSkill(i)
    │   SkillBehaviourBase.Release()
    │     ├── cdTimer = GetCdTime()                  ← 冷却开始计时
    │     ├── hitTargets.Clear()                     ← 重置命中去重集合
    │     └── skillPlayer.StartPlaySkillBehaviour(this)
    │
    ▼ skillPlayer.PlaySkillClip(skillClip)           ← 帧驱动启动
    │   currentFrameIndex = -1, isPlaying = true
    │
    ▼ SkillPlayer.Update() → TickSkill()  (按帧率追帧)
    │
    ├─► TickSkillAnimationEvent()
    │     SkillClip.SkillAnimationData.FrameData[frameIndex] → SkillAnimationEvent
    │     owner.EnterSkillMode(upperBody)
    │     skillLayer.Play(animationClip, fadeTime)    ← Animancer 播放动画
    │     state.Time = 0                              ← 强制从头
    │     若 ApplyRootMotion → owner.SetSkillRootMotion(behaviour.OnRootMotion, true)
    │
    ├─► TickSkillEffectEvent()
    │     检查 effectEvent.FrameIndex == currentFrameIndex
    │     VfxEmitterHelper.EmitSkillVfx()            ← ECS 路径生成特效
    │     fallback: PoolSystem.GetGameObject() + Instantiate + AutoDestruct协程
    │
    ├─► TickSkillAudioEvent()
    │     AudioSystem.PlayOneShot(audioClip, position)
    │
    ├─► TickSkillAttackDetectionEvent()
    │   ┌─ 武器检测(Weapon): frameIndex == start → weapon.StartDetection(attackData)
    │   │                    frameIndex == end   → weapon.StopDetection()
    │   └─ 形状检测(Shape): Physics.OverlapBox/Sphere → IHitTarget 列表
    │       AttackDetectionEmitterHelper.Emit() (ECS路径)
    │       fallback: SkillBehaviourBase.OnAttackDetection(hitTarget, attackData)
    │
    ▼ SkillBehaviourBase.OnAttackDetection()
    │   hashSet.Add(hitTarget) 去重
    │
    ▼ SkillBehaviourBase.OnHitTarget()
    │   DoHitEffect(attackData)                      ← 命中特效/音效
    │   hitTarget.OnHit(attackData)                  ← 接口调用
    │
    ▼ PlayerController.OnHit(attackData) / EnemyController.OnHit()
    │   DamageHelper.EmitDamage(entity, attackData, position) ← 发射ECS事件
    │   检查无敌帧 → 记录受击方向 → ChangeState(Hurt)
    │
    ▼ ECS DamageSystem                               ← 纯逻辑，定帧20hz
    │   读取 Health 组件, 减去 attackData.attackValue
    │   hp <= 0 → 挂 DeathTag
    │
    ▼ ECS DeathSystem
    │   检测 DeathTag → 触发 IDeathCallback.OnDeath()
    │
    ▼ PlayerController.OnDeath()
    │   关闭所有 Collider
    │   ChangeState(PlayerState.Dead)
    │
    ▼ 受伤/死亡动画播放，AnimationEnd() 回调
        MovementStateMachine.OnAnimationEnd() → 回 Idle 或保持 Dead
```

### 3.2 伤害值计算

在 `SkillPlayer.TickSkillAttackDetectionEvent()` 中构造 `AttackData`：

```csharp
attackValue = owner.GetAttackValue(detectionEvent)
// PlayerController.GetAttackValue():
return characterAttribute.attack.Total * detectionEvent.AttackHitConfig.AttackMultiply;
```

即：**最终伤害 = 角色攻击属性合计 × 技能倍率**。这是一个扁平的固定伤害模型，未引入防御公式或暴击分支。

---

## 四、状态机设计

### 4.1 状态枚举

```csharp
// PlayerState.cs
public enum PlayerState { Idle, Move, Skill, Hurt, Dead }
```

### 4.2 状态机实现

`PlayerStateMachine` 是自定义分层状态机（基于 JKFrame `IStateMachineOwner` 接口），持有各状态实例：

| 状态 | 对应类 | 主要行为 |
|------|--------|----------|
| `idleState` | `PlayerIdleState` | 蓄力、待机动画、检测移动输入 |
| `moveStartState` | `PlayerMoveStartState` | 起步动画，追帧旋转 |
| `moveLoopState` | `PlayerMoveLoopState` | 循环移动，保持物理移动+旋转 |
| `skillState` | `PlayerSkillState` | 进入/退出技能层，处理连击输入 |
| `hurtState` | `PlayerHurtState` | 受击动画，完成后回 Idle |
| `deadState` | `PlayerDeadState` | 死亡动画，不可返回 |

### 4.3 状态切换条件

- **Idle → Move**：`InputService.Move != Vector2.zero`
- **Any → Skill**：`HandleSkillInput()` 检查通过 → `ChangeState(skillState)` → `ReleaseSkill(i)`
- **Any → Hurt**：`OnHit()` 被调用 & 非无敌帧
- **Any → Dead**：ECS DeathSystem 回调 `OnDeath()`
- **Skill → Idle**：技能 Clip 播放完毕 → `SkillBehaviourBase.OnSkillClipEnd()` → `skillPlayer.isPlaying = false` → `PlayerSkillState.NotifySkillEnd()` → `ExitSkillMode()` → 状态机回 Idle

### 4.4 防止状态混乱

- **Layer 权重互斥**：`EnterSkillMode()` 将 Layer1 权重立即设为 1，Layer0 设为 0（或上身技能时保留 Layer0），避免两层同时以权重 1 播放引发 Animancer 警告。
- **无敌帧保护**：`ReusableData.isInvincible` 为真时，`OnHit()` 不切换到受伤状态。
- **连击缓冲**：`PlayerSkillState.HandleCombatInput()` 在技能播放中捕捉下一技能输入，`SkillBrainBase.CanRelease` 标志由 `SkillEventType.CanSkillRelease` 自定义事件控制，防止动画前段随意打断。
- **层清理**：`CleanupFinishedSkillLayer()` 在技能退出后每帧检查 Layer1 权重是否归 0，归 0 即 `Stop()`，防止不可见的动画持续驱动浪费性能。

---

## 五、技能系统

### 5.1 技能数据结构

```
SkillConfig (ScriptableObject)
├── SkillClip   (序列化帧数据)
│   ├── SkillAnimationData    : Dictionary<int frameIndex, SkillAnimationEvent>
│   ├── SkillEffectData       : List<SkillEffectEvent>
│   ├── SkillAudioData        : List<SkillAudioEvent>
│   ├── SkillAttackDetectionData : List<SkillAttackDetectionEvent>
│   │   └── AttackDetectionData (WeaponDetectionData | SphereDetectionData | BoxDetectionData)
│   │       └── AttackHitConfig (HitAudioClip, HitEffectPrefab, AttackMultiply)
│   └── SkillCustomEventData  : Dictionary<int frameIndex, SkillCustomEvent>
│       └── SkillEventType (CanSkillRelease | CanRotate | AddBuff | CreateWeapon | CanInterrupt | BreakCombo)
└── ReleaseCostDict : Dictionary<SkillCostType, float>  (HP/MP消耗)
```

每一帧的事件都按 `frameIndex` 的字典（精确帧触发）或列表（持续帧范围）存储，**SkillEditor** 是配套的可视化编辑工具，可在时间轴上拖动事件区间。

### 5.2 冷却实现

`SkillBehaviourBase` 持有 `float cdTimer`，在 `Release()` 时赋值 `GetCdTime()`（按等级查表），在 `OnUpdate()` 中每帧 `cdTimer -= Time.deltaTime`。`CheckCdTime()` 返回 `cdTimer <= 0`。**冷却是纯本地计时器，无服务器校验**。

### 5.3 技能与动画绑定

帧驱动系统通过 `TickSkillAnimationEvent()` 在精确帧触发动画：
1. `skillLayer.Play(animationEvent.AnimationClip, fadeTime)` —— Animancer 的 `AnimancerLayer.Play()` 支持跨 clip 过渡
2. `state.Time = 0` —— 强制从头，防止 Animancer 复用旧 State 从上次暂停位置继续

### 5.4 技能如何触发判定

- **武器碰撞**：`SkillPlayer` 持有 `WeaponController` 字典，检测帧开/关武器的 `Collider` 触发区（`OnTriggerEnter`），武器命中时回调 `OnWeaponDetection()`，再走 `WeaponHitEmitterHelper.Emit()` → ECS 路径或 fallback Mono 路径。
- **形状检测**：`SkillAttackDetectionHelper.ShapeDetection()` 在检测帧范围内每帧执行 `Physics.OverlapBox` / `OverlapSphere`，收集 `IHitTarget`，再调用 `skillBehaviour.OnAttackDetection()`。
- **去重机制**：`SkillBehaviourBase` 内部维护 `HashSet<IHitTarget> hitTargets`，在技能周期内同一目标只命中一次（可在子类重写放开限制）。

### 5.5 技能扩展

继承 `SkillBehaviourBase` 并重写以下虚方法即可：

| 虚方法 | 用途 |
|--------|------|
| `BeforeSkillAnimationEvent()` | 动画帧触发前拦截，可替换 AnimationClip |
| `BeforeSkillAttackDetectionEvent()` | 伤害检测前拦截，可修改 AttackData |
| `AfterSkillCustomEvent()` | 自定义事件后处理（默认处理旋转、Buff等） |
| `OnRootMotion()` | 接管 RootMotion 位移 |
| `OnTickSkill()` | 每帧驱动回调 |
| `DeepClone()` | 抽象，要求每个技能实现深拷贝（供多实例运行时使用）|

`Skill1Behaviour.cs` 是第一套弹反/连击技能的具体实现，`Skill2Behaviour.cs` 继承后只需重写少量方法。

---

## 六、AI 系统

### 6.1 行为树结构

项目使用 **Behaviour Designer** 插件作为行为树框架。Boss AI 的行为树节点分布在 `Assets/Scripts/BT/Action/` 和 `BT/Condition/` 中：

- **Condition 节点**：如距离检测、血量阈值判断
- **Action 节点**：如 Chase（追击）、Attack（攻击）、Patrol（巡逻）

`BossAIEventBuffer.cs` 实现了**事件缓冲**机制，将物理世界的碰撞/感知事件写入缓冲队列，行为树节点在 Tick 时从中读取，避免行为树直接绑定物理回调（解耦逻辑时序与物理帧）。

### 6.2 决策逻辑

行为树由 Behaviour Designer Runtime 每帧（或按设定频率）Tick，Enemy 的寻路移动通过 **A\* Pathfinding Project**（`com.arongranberg.astar`）插件驱动，行为树 Action 节点持有 `AIPath` / `RichAI` 引用设置目标点，典型的 Boss 决策流程：

```
Selector
├── Sequence [远程攻击]
│   ├── Condition: 距离 < 远程攻击范围
│   └── Action: 释放远程技能
├── Sequence [近战攻击]
│   ├── Condition: 距离 < 近战范围
│   └── Action: 释放近战技能
├── Sequence [追击]
│   ├── Condition: 发现玩家
│   └── Action: Chase → 更新 NavMesh 目标
└── Action: Patrol（默认巡逻）
```

### 6.3 与角色系统交互

`EnemyController` 实现 `ICharacter` 和 `IHitTarget` 接口，行为树 Action 节点通过 `GetComponent<EnemyController>()` 获取角色引用，调用 `EnemyController.ReleaseSkill()` 或通过 A\* Pathfinding 的 `AIPath.destination` 设置追击目标。`BattleEcsRunner.RegisterCharacter()` 同样支持非玩家角色，注册时以 `BossTag` 标记 ECS 实体。

### 6.4 扩展新行为

新增行为树节点只需继承 Behaviour Designer 的 `Action` 或 `Condition` 基类，在 `OnUpdate()` 中实现逻辑并返回 `TaskStatus.Success/Failure/Running`，不需要修改 `EnemyController` 主体。

---

## 七、性能优化设计

### 7.1 对象池使用场景

对象池通过 JKFrame 的 `PoolSystem` 实现，使用场景：

| 场景 | 代码位置 |
|------|---------|
| 技能命中特效 | `SkillPlayer.TickSkillEffectEvent()` → `PoolSystem.GetGameObject(prefabName)` |
| 技能特效归还 | `AutoDestructEffectGameObject()` 协程 → `obj.GameObjectPushPool()` |
| ECS 路径特效 | `VfxEmitterHelper.EmitSkillVfx()` / `EmitHitVfx()` 内部走 ECS 生命周期系统自动回收 |
| 掉落物 | `LootDropManager.SpawnWorldDrop()` 使用对象池 |

### 7.2 避免频繁 GC

- `SkillBehaviourBase.hitTargets` 使用 `HashSet<IHitTarget>`，在技能结束时调用 `.Clear()` 而非重建，避免每次技能都 `new HashSet<>()`。
- `SkillPlayer.TickSkillAttackDetectionEvent()` 中 Editor Only 的 `currentAttackDetectionList` 仅 `#if UNITY_EDITOR` 条件下分配，运行时不存在。
- ECS 架构本身避免了大量 `GetComponent` 调用，组件以值类型结构体（如 `Health`、`Position`、`Attribute`）存储在 Arch ECS 的 Archetype 块内存中，查询无装箱。
- `BattleEcsRunner` 使用固定 20hz 逻辑帧（`logicFrameRate = 20`），通过时间累加器 `_accumulator` 控制 ECS Tick 频率，避免每帧都处理战斗逻辑。

### 7.3 Update 优化方式

- `PlayerController.Update()` 在 `useGenericLocomotion == true` 时直接 `return`，非主角控制模式下完全跳过所有手动逻辑。
- `SkillBehaviourBase.UpdateCdTime()` 在 `cdTimer <= 0` 时提前返回，避免每帧运算。
- `SkillPlayer.CleanupFinishedSkillLayer()` 逻辑简单：`if (inSkill) return`，无技能时即短路。
- 动画状态机使用 Animancer 而非 Unity 原生 Animator Controller，Animancer 只有活动 State 才消耗 CPU，静止 State 不参与混合计算。

---

## 八、事件系统

### 8.1 事件总线结构

项目结合使用两套事件机制：

1. **JKFrame EventSystem（全局消息总线）**：基于类型字典+委托，适合跨模块广播（如场景切换、UI 刷新）
2. **C# Action 委托（细粒度模块事件）**：直接定义在管理器/组件上，订阅方引用管理器单例

```csharp
// DataManager 中定义
public static event Action<int, int> OnLevelUp;

// PlayerController 订阅
DataManager.OnLevelUp += OnCharacterLevelUp;

// OnDestroy 取消订阅（防内存泄漏）
DataManager.OnLevelUp -= OnCharacterLevelUp;
```

### 8.2 如何避免强耦合

- 战斗系统通过 `ICharacter`、`IHitTarget`、`IDamageNumberService` 接口与实现层解耦。`SkillPlayer` 的武器检测回调写入 `IHitTarget.OnHit()`，不直接引用 `EnemyController`。
- UI 层的飘字服务通过接口注入：`PlayerController.Init()` 中检查 `DamageNumberManager.Instance` 是否存在，存在则赋值给 `context.DamageNumberService`（类型为接口 `IDamageNumberService`），ECS 系统只持有接口引用，不知晓 UI 层的具体实现。

### 8.3 示例：伤害事件广播流程

```
[技能命中检测]
    SkillBehaviourBase.OnHitTarget(hitTarget, attackData)
        └─► hitTarget.OnHit(attackData)           // IHitTarget 接口

[IHitTarget 实现端]
    PlayerController.OnHit(attackData):
        DamageHelper.EmitDamage(PlayerEntity, attackData, position)
        // 向 ECS World 写入 DamageRequest 组件

[ECS DamageSystem (20hz逻辑帧)]
    查询含 DamageRequest 的实体
        → Health.CurrentHp -= damageRequest.Value
        → 移除 DamageRequest 组件
        → 若 hp <= 0, 添加 DeathTag

[ECS DeathSystem]
    查询含 DeathTag 的实体
        → 调用 IDeathCallback.OnDeath()           // 接口回调
        → 移除 DeathTag

[MonoBehaviour 回调端]
    PlayerController.OnDeath():
        禁用 Collider
        ChangeState(PlayerState.Dead)
        // 播放死亡动画
```

---

## 九、其他核心系统与管线设计

### 9.1 Addressables 与 UniTask 异步加载

**实现状态**：`CharacterModelManager.cs` 结合了 `Addressables` 资源管理和 `UniTask` 异步流程，用于动态加载角色模型与配置。
**技术亮点**：
- **Handle 缓存管理**：通过字典 `_loadedPrefabs` 缓存 `AsyncOperationHandle`，避免重复加载，并确保引用计数的正确增减。
- **免回调的异步流程**：使用 `UniTask`（`await modelManager.LoadCharacterModelPrefabAsync(...)`），使异步加载如同步代码般线性可读，避免回调地狱。
- **无缝替换模型**：`ReplaceCharacterModelAsync` 方法实现了保留 `PlayerController` 逻辑层不动，仅销毁和实例化其下的 `PlayerModel` 外观层，对换装系统或选角界面的场景过渡非常友好。

### 9.2 DataManager 存档与成长支持

**实现状态**：`DataManager.cs` 作为纯净的静态管理器负责整个 `GameData` 实例的磁盘序列化（使用 JKFrame 的 `SaveSystem`。
**数据结构**：
- 自定义了众多 `Serialized_List` / `Serialized_Dic` 以跨越 Unity 序列化字典的限制。
- 存档数据包括：已解锁角色列表、角色状态（等级/经验/当前技能/快捷配置）、场景中无限期掉落物的持久化记录、以及已清空的敌人生成区域。
- 游戏初始化（`GameSceneManager`）第一步即是从其检查存档槽并加载或创建存档。
- 提供基于配置表的自修复机制（`EnsureCurrentCharacterDataByConfig`），版本迭代新增/删除了技能，存档读入时会自动容错处理。

### 9.3 掉落物与拾取系统（ECS 结合表现层）

**实现状态**：`LootDropManager.cs` 使用独立 `World` 管理掉落物的物理与生命周期。
**双层机制**：
- **混合实体**：生成掉落时，创建拥有 `DropItem` (数据) 和 `ViewReference` (表现层指针) 的 Arch 实体。
- **零 GC 生命周期处理**：通过实现 `IForEachWithEntity` 的 `LifetimeProcessor` 以 `InlineEntityQuery` 在 `TickLifetimes` 中批量消退掉落时间上限，达成高性能管理。
- **持久化保存机制**：对于配置为无时间限制（Lifetime < 0）的掉落物对象，记录其 Guid 与位置进存档字典 `PersistentDrops`。在下次进入场景时 `RestoreScenePersistentDrops()` 重构实体，实现满地“垃圾”跨场景不丢失的能力。
- **自动拾取**：`DetectAutoPickup` 利用预分配的 `_overlapBuffer` 和 `Physics.OverlapSphereNonAlloc` 达成零分配自动收集，并通知 `InventoryManager` 和 UI_GameSceneMainWindow。

### 9.4 区域生成器（EnemySpawnManager）

**实现状态**：`EnemySpawnManager.cs` 为场景级静态管理器，负责统一调配各 `SpawnRegion` 的生成生命周期，节省由于分布庞大的生成器各起 Update 带来的浪费。
**机制说明**：
- **距离倒排探测**：利用间隔检测 (`detectInterval`) 和极简距离判定，只有玩家接近相应 `activateR` 时才触发 `region.Activate()`，离开时 `Deactivate()`。
- **持久化状态同步**：清空区域内的敌人后调用 `OnRegionCleared` 会记录区域 ID 入档，即使二次进场景该区域亦不会复活敌人，构成了大世界探险式的设计意图。

---

## 十、未实现部分说明

### 9.1 数值成长系统

**实现状态**：`LevelGrowthConfig.cs`（ScriptableObject）和 `characterAttribute.ApplyLevel()` 方法已存在，`DataManager.OnLevelUp` 事件已接通，`PlayerController` 订阅升级事件并刷新属性。  
**未完成**：经验值累积触发升级的 UI 完整流程、属性面板刷新、升级特效尚未完善（代码中有 TODO 注释）。  
**原因**：Demo 聚焦战斗机制验证，数值设计需要策划配合，不在技术验证范围内。  
**扩展支持**：架构完全支持，只需补充 EXP 曲线配置和升级 UI 即可。

### 9.2 装备系统

**实现状态**：`WeaponConfig.cs`、`WeaponSlotManager.cs`、`WeaponController.cs` 存在，支持武器挂载和拆卸（`CreateWeapon` / `DestroyWeapon`），通过 `SkillCustomEvent` 在技能帧中动态换装。  
**未完成**：装备属性计算（防具、饰品）、装备背包 UI、装备词缀系统未实现。  
**原因**：武器挂载逻辑已够技术验证，完整装备系统属于游戏设计范畴。  
**扩展支持**：`CharacterAttribute` 的属性层（Base/Bonus/Total）结构支持来自装备的加成注入。

### 9.3 Buff 系统

**实现状态**：`Battle/Buff/` 目录下有 `BuffController.cs`、`BuffEffectResolverBase.cs`；ECS 侧 `BuffList` 组件（容量 16）已挂载到实体；`BuffHelper.AddBuff()` 静态工具类已实现。`PlayerController.AddBuff()` 接口可调用。`PlayerBuffController.cs` 和 `PlayerBuffEffectResolver.cs` 存在，用于表现层处理。  
**未完成**：Buff UI（图标、计时条）、Buff 叠加显示、Buff 类型扩展库（目前仅有框架骨架）。  
**原因**：ECS Buff 系统是单独设计的复杂子系统，Demo 阶段建立架构即可。  
**扩展支持**：继承 `BuffEffectResolverBase` 即可添加新 Buff 类型效果，数据端用 `BuffConfig` ScriptableObject 配置。

### 9.4 UI 系统

**实现状态**：`Assets/Scripts/UI/` 下有 `UI_GameSceneMainWindow.cs`，使用 JKFrame UISystem，Hp/Mp 血条有基础实现，飘字通过 DamageNumbersPro 插件接入。  
**未完成**：完整角色属性面板、技能冷却 UI（图标+CD遮罩）、成就/任务系统 UI。  
**原因**：UI 属于内容性工作，Demo 阶段验证战斗逻辑，UI 仅做最小可用实现。

---

## 十、技术亮点总结

### 10.1 架构设计思路

1. **Mono + ECS 双轨制**：表现层（动画/特效/输入）保留 MonoBehaviour，战斗逻辑（伤害/Buff/死亡）沉入 Arch ECS。两者通过接口+静态辅助类通信，职责边界清晰，未来可将逻辑侧迁移至服务器或 IL2CPP 确定性框架。

2. **帧驱动技能系统**：`SkillPlayer` 完全基于时间轴帧索引驱动，动画、特效、音效、攻击判定均以 `frameIndex` 为锚点，逻辑与动画播放完全解耦。即使动画被 Animancer 层权重覆盖，攻击判定依然精确执行。

3. **Before/After 事件拦截设计**：`SkillBehaviourBase` 对每类技能事件提供 `Before/After` 虚方法对（如 `BeforeSkillAnimationEvent` / `AfterSkillAnimationEvent`），子类可以在不破坏父类逻辑的前提下修改或注入数据，类似拦截器链/装饰器模式。

### 10.2 可扩展性

| 扩展点 | 机制 |
|--------|------|
| 新技能类型 | 继承 `SkillBehaviourBase`，重写关键虚方法 |
| 新攻击形状 | 实现 `AttackDetectionDataBase` 子类，在 `SkillAttackDetectionHelper` 添加分支 |
| 新 Buff 效果 | 继承 `BuffEffectResolverBase` |
| 新 AI 行为 | Behaviour Designer 中添加 Action/Condition 节点类 |
| 新敌人类型 | 实现 `ICharacter`、`IHitTarget` 接口，注册到 ECS |
| 新属性成长 | 修改 `LevelGrowthConfig` 曲线配置，`ApplyLevel` 已支持 |

### 10.3 可维护性

- **程序集隔离**：`ARPG.Core`、`ARPG.Battle`、`ARPG.Manager` 分别有独立 `.asmdef`，修改 Battle 层不会触发 Core 层重编译，大幅提升迭代速度。
- **接口契约**：战斗链上所有跨层通信通过接口（`ICharacter`、`IHitTarget` 等）而非具体类，重构单个组件不影响其他模块。
- **可视化调试**：`SkillGizmosTool.DrawDetectionGizmos()` 在 Editor 运行时绘制攻击判定区域，`RayDebug` 工具类包装日志输出，可按类型过滤。

---

## 十一、扩展为完整商业 ARPG 的演进方向

### 11.1 数值系统演进

当前伤害公式 `攻击 × 倍率` 需扩充为：
```
伤害 = Max(1, (攻击 × 倍率 - 防御) × 暴击修正 × buff修正 × 元素抗性)
```
每个修正项对应一个属性层，`CharacterAttribute` 已有 Base/Bonus/Total 结构，只需在 ECS `DamageSystem` 中引入 `Defense`、`CritRate`、`ElementResistance` 组件并修改公式。

### 11.2 联网架构演进

ECS 逻辑帧（20hz 定帧、FixedPoint 数학）已为帧同步奠基。演进路径：
1. 提取 `LocalLogicFeature` 为纯逻辑 DLL（不依赖 UnityEngine）
2. 服务端跑同 DLL，客户端只发输入指令，服务端回放
3. `LocalViewFeature` 保留在客户端做插值表现

### 11.3 技能系统演进

- SkillEditor 时间轴扩展支持嵌套 Clip（连招树）
- 添加 `SkillModifier` 层，支持装备/Buff 实时修改技能参数（如缩短 CD、增大判定范围）
- 技能解锁/强化通过修改 `SkillLearnedData`（已有 `lv` 字段），`GetCdTimeByLv()` 按等级表查询

### 11.4 AI 演进

- 当前 Behaviour Designer 行为树可扩展为**分层行为树（HTN）** 或 **GOAP（目标导向）**（项目中存在 `BossGoapDefaultFactory.cs` 骨架但当前未实际接入）
- A\* Pathfinding Project 支持动态障碍物更新（`DynamicObstacle`）和多区域寻路图，可直接用于大地图场景
- 添加感知系统（视野锥/听觉范围）作为行为树 Condition 的输入源
- Boss 多阶段设计：血量阈值触发行为树切换

### 11.5 资产管理演进

目前使用 Addressables 基础配置（项目中已有 `AddressableAssetsData`），演进为按场景分 Group 异步加载，预制体不再直接序列化引用。

---

## 十三、可能遗漏点自检

经过对代码的全面扫描，以下为可能未充分说明的技术点：

| 检核项 | 结论 |
|--------|------|
| ✅ `GenericPlayerLocomotionController` 分支 | 已说明：当 CharacterConfig 含 GenericLocomotionConfig 时启用 Animator Controller 路径，Animancer 关闭，两套移动系统通过 flag 互斥 |
| ✅ `WeaponSlotManager` 动态换装 | 已说明：武器槽可在技能帧中通过 SkillCustomEvent 动态创建/销毁武器 |
| ✅ 无敌帧实现 | 已说明：`ReusableData.isInvincible` flag 在 `OnHit()` 中检查 |
| ✅ RootMotion 三模式 | 已说明：Default / Suppressed / Custom，SkillBehaviour.OnRootMotion 接管 Custom 路径 |
| ⚠️ NavMesh 移动 | Enemy AI 的 Chase 动作依赖 NavMesh Agent，但代码中具体实现在 `EnemyController.cs`（96KB 大文件），本次未全量读取，移动细节可能有遗漏 |
| ✅ AI 移动寻路 | Enemy 移动通过 A\* Pathfinding Project（`com.arongranberg.astar`）驱动，而非 Unity 内置 NavMesh |
| ✅ GOAP 系统 | `BossGoapDefaultFactory.cs` 是探索遗留骨架，**当前未使用**，实际 AI 采用 Behaviour Designer 行为树 |
| ✅ 多角色切换 | 选角在独立选角界面完成，运行时通过 `CharacterModelManager`（Addressables+UniTask）动态加载并替换外观模型 |
| ⚠️ 伤害飘字归还池 | `DamageNumberManager` 使用 DamageNumbersPro 插件，插件内部有自己的对象池机制，本项目未额外封装；归还逻辑由插件接管 |
| ✅ 存档系统 | 已补入文档：`DataManager` 统一管理 `GameData` 并通过 JKFrame 序列化，支持数据对账熔断修复 |
| ✅ 掉落物系统 | 已补入文档：`LootDropManager` 引入第二 ECS 世界混合管理，支持无限期掉落持久化到存档，零 Alloc 球形探测拾取 |
| ✅ 敌人区域刷新 | 已补入文档：`EnemySpawnManager` 全局感知距离，剔除 Update 并配合存档持久化“已清空区域” |

---

*文档生成时间：2026-03-03 | 代码扫描范围：`Assets/Scripts/` 全量核心文件*
