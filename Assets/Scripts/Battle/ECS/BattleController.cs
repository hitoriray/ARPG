// using Arch.Core;
// using Arch.Core.Extensions;
// using Battle.ECS.Components;
// using Battle.ECS.Core;
// using Battle.ECS.Features;
// using FixMath;
// using UnityEngine;
//
// namespace Battle.ECS
// {
//     /// <summary>
//     /// 战斗控制器 - MonoBehaviour入口，驱动ECS战斗系统
//     /// </summary>
//     public class BattleController : MonoBehaviour
//     {
//         /// <summary>
//         /// 单例
//         /// </summary>
//         public static BattleController Instance { get; private set; }
//         
//         /// <summary>
//         /// 战斗上下文
//         /// </summary>
//         public BattleContext Context { get; private set; }
//         
//         /// <summary>
//         /// 逻辑Feature
//         /// </summary>
//         public LogicFeature LogicFeature { get; private set; }
//         
//         /// <summary>
//         /// View Feature
//         /// </summary>
//         public ViewFeatureBase ViewFeatureBase { get; private set; }
//         
//         [Header("战斗设置")]
//         [SerializeField] private int randomSeed = 12345;
//         [SerializeField] private int frameRate = 20;
//         
//         private float _accumulator;
//
//         private void Awake()
//         {
//             Instance = this;
//         }
//
//         /// <summary>
//         /// 开始战斗
//         /// </summary>
//         public void StartBattle()
//         {
//             // 创建战斗上下文
//             Context = new BattleContext(randomSeed);
//             
//             // 创建Feature
//             LogicFeature = new LogicFeature(Context);
//             ViewFeatureBase = new ViewFeatureBase(Context);
//             
//             // 将Feature添加到Context的主Feature中
//             Context.Feature.Add(LogicFeature);
//             Context.Feature.Add(ViewFeatureBase);
//             
//             // 初始化
//             Context.Initialize();
//             
//             Debug.Log("[BattleController] 战斗开始!");
//         }
//
//         /// <summary>
//         /// 结束战斗
//         /// </summary>
//         public void EndBattle()
//         {
//             if (Context != null)
//             {
//                 Context.End();
//                 Context.Dispose();
//                 Context = null;
//                 Debug.Log("[BattleController] 战斗结束!");
//             }
//         }
//
//         private void Update()
//         {
//             if (Context == null || Context.State != BattleState.Running)
//                 return;
//             
//             // 固定帧率更新
//             _accumulator += Time.deltaTime;
//             float logicDeltaTime = Context.LogicTime.DeltaTime;
//             
//             while (_accumulator >= logicDeltaTime)
//             {
//                 _accumulator -= logicDeltaTime;
//                 Context.Update();
//             }
//         }
//
//         private void OnDestroy()
//         {
//             EndBattle();
//             Instance = null;
//         }
//
//         // ============================================
//         // 便捷方法
//         // ============================================
//
//         /// <summary>
//         /// 创建实体并添加Position组件
//         /// </summary>
//         public Entity CreateEntity(TSVector3 position)
//         {
//             if (Context?.World == null) return Entity.Null;
//             
//             return Context.World.Create(new Position(position));
//         }
//
//         /// <summary>
//         /// 创建带View的实体
//         /// </summary>
//         public Entity CreateEntityWithView(TSVector3 position, GameObject viewObject)
//         {
//             if (Context?.World == null) return Entity.Null;
//             
//             return Context.World.Create(
//                 new Position(position),
//                 new ViewReference(viewObject, viewObject.GetComponent<ICharacterView>())
//             );
//         }
//
//         /// <summary>
//         /// 给实体设置速度
//         /// </summary>
//         public void SetEntityVelocity(Entity entity, TSVector3 velocity)
//         {
//             if (!entity.IsAlive()) return;
//             
//             if (entity.Has<Velocity>())
//             {
//                 entity.Set(new Velocity(velocity));
//             }
//             else
//             {
//                 entity.Add(new Velocity(velocity));
//             }
//         }
//     }
// }