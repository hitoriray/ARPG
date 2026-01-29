// using System;
// using Arch.Core;
// using Battle.ECS.Helper;
// using FixMath;
// using GameLogic.Battle.Core.Collision;
//
// namespace Battle.ECS.Core.Collision.Grid
// {
//     /// <summary>
//     /// 均匀网格管理器
//     /// </summary>
//     public sealed class UniformGridMgr : ISpaceQuery
//     {
//         private readonly BattleContext _context;
//         public UniformEntityGrid[] Grids { get; private set; }
//         public readonly int DefaultCellSize = 3;
//
//         public UniformGridMgr(BattleContext context)
//         {
//             _context = context;
//             Grids = new UniformEntityGrid[Enum.GetValues(typeof(EnumGridChannel)).Length];
//         }
//
//         public void Add(EnumGridChannel channel, QueryDescription desc, FP cellSize)
//         {
//             Grids[(int) channel] = new UniformEntityGrid(channel, desc, cellSize);
//         }
//
//         /// <summary>
//         ///  获取指定通道的网格
//         /// </summary>
//         /// <param name="channel"></param>
//         /// <returns></returns>
//         public UniformEntityGrid GetGrid(EnumGridChannel channel)
//         {
//             return Grids[(int) channel];
//         }
//
//         public void Dispose()
//         {
//             foreach (var grid in Grids)
//             {
//                 grid?.Dispose();
//             }
//             Grids = null;
//         }
//
//         #region 暂时没用
//         public bool BuildAgentSpace()
//         {
//             return true;
//         }
//
//         public void BuildObstacleSpace()
//         {
//         }
//
//         public bool QueryVisibility(TSVector2 point1, TSVector2 point2, FP range)
//         {
//             return false;
//         }
//         #endregion
//
//         public void ComputeObstacleNeighbors(Agent agent, FP rangeSq, FP checkRadius)
//         {
//             ProfilerHelper.Begin("ComputeAgentNeighbors-RvoObstacle");
//             GetGrid(EnumGridChannel.RvoObstacle).ComputeObstacleNeighbors(agent, ref rangeSq, checkRadius);
//             ProfilerHelper.End();
//         }
//
//         public void ComputeAgentNeighbors(Agent agent, ref FP rangeSq, FP checkRadius)
//         {
//             ProfilerHelper.Begin("ComputeAgentNeighbors-Monster");
//             GetGrid(EnumGridChannel.Monster).FindNearestEntity(agent, checkRadius, ref rangeSq, _context, false);
//             ProfilerHelper.End();
//         }
//     }
// }
