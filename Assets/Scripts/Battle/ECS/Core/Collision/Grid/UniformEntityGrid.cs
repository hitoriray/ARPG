// using System;
// using Arch.Core;
// using Battle.Core.Collision.Grid;
// using Battle.ECS.Components;
// using Battle.ECS.Helper;
// using FixMath;
// using GameLogic.Battle.Core.Collision;
//
// namespace Battle.ECS.Core.Collision.Grid
// {
//     /// <summary>
//     ///  均匀网格
//     /// </summary>
//     public class UniformEntityGrid : UniformGridBase<Entity>
//     {
//         public readonly EnumGridChannel Channel;
//         public readonly QueryDescription Desc;
//         private readonly TSVector2[] _obbVerticesBuffer = new TSVector2[4];
//
//         /// <param name="channel">通道</param>
//         /// <param name="desc">在网络中的实体</param>
//         /// <param name="cellSize">格子大小</param>
//         public UniformEntityGrid(EnumGridChannel channel, QueryDescription desc, FP cellSize) : base(cellSize)
//         {
//             Channel = channel;
//             Desc = desc;
//         }
//
//         public bool TryAddEntity(Entity entity, ref Position position, ref UniformGridObj gridObj, ref Components.BoundingBox box)
//         {
//             if (gridObj.Channel != null && gridObj.Channel != Channel)
//             {
//                 ThrowHelper.Throw($"{nameof(OnObjUpdate)}: {entity.GetDebugInfo()} already in another channel: {gridObj.Channel}, can not add to {Channel}");
//                 return false;
//             }
//             var added = base.OnObjUpdate(entity, position.Value.XZToTSVector2(), box.Max);
//             gridObj.Channel = added ? Channel : null;
//             return added;
//         }
//
//         public bool TryAddEntityOBB(Entity entity, ref Position position, ref UniformGridObj gridObj, ref Components.BoundingBox box)
//         {
//             if (gridObj.Channel != null && gridObj.Channel != Channel)
//             {
//                 ThrowHelper.Throw($"{nameof(OnObjUpdate)}: {entity.GetDebugInfo()} already in another channel: {gridObj.Channel}, can not add to {Channel}");
//                 return false;
//             }
//
//             var relativeVertices = box.GetVertices();
//             var worldPos = position.Value.XZToTSVector2();
//             // 边函数扫描算法(Edge Function)在UniformGridBase中要求顺时针(CW)顶点序列
//             // 而BoundingBox返回的是逆时针(CCW)，因此需要调整顶点顺序：0->3->2->1
//             _obbVerticesBuffer[0] = worldPos + relativeVertices.Vertex0;
//             _obbVerticesBuffer[1] = worldPos + relativeVertices.Vertex3;
//             _obbVerticesBuffer[2] = worldPos + relativeVertices.Vertex2;
//             _obbVerticesBuffer[3] = worldPos + relativeVertices.Vertex1;
//
//             var added = base.OnObjUpdatePolygon(entity, _obbVerticesBuffer);
//             gridObj.Channel = added ? Channel : null;
//             return added;
//         }
//
//         public void FindNearestEntity(Agent agent, FP checkRadius, ref FP rangeSq, BattleContext context, bool isAgentObs)
//         {
//             if (WorldToGrid(agent.Position, out var center) == false)
//             {
//                 return;
//             }
//
//             int maxDistance = (int) TSMath.Ceiling(checkRadius / CellSize);
//             var maxLength = Math.Max(Col, Row);
//             maxDistance = Math.Min(maxDistance, maxLength);
//
//             var seekGrid = context.SeekGrid;
//             seekGrid.CacheClear();
//             for (int dis = 0; dis < maxDistance; dis++)
//             {
//                 bool ringGridExists = seekGrid.RingGrid.GetRingGrids(dis, out var ringGrids);
//                 if (ringGridExists)
//                 {
//                     foreach ((int col, int row) grid in ringGrids)
//                     {
//                         // 获取格子中的所有实体
//                         var gridEntityList = GetCellObjects(grid.col + center.x, grid.row + center.y);
//                         if (gridEntityList == null)
//                             continue;
//
//                         foreach (var entity in gridEntityList)
//                         {
//                             if (!seekGrid.CacheExcludeEntities.Add(entity))
//                             {
//                                 continue;
//                             }
//
//                             var agentNoById = agent._simulator.GetAgentNoById(entity);
//                             if (agentNoById != null)
//                             {
//                                 if (isAgentObs)
//                                 {
//                                     ProfilerHelper.Begin("InsertAgentObs");
//                                     agent.InsertObsAgent(agentNoById, ref rangeSq, checkRadius);
//                                     ProfilerHelper.End();
//                                 }
//                                 else
//                                 {
//                                     ProfilerHelper.Begin("InsertAgentNeighbor");
//                                     agent.InsertAgentNeighbor(agentNoById, ref rangeSq);
//                                     ProfilerHelper.End();
//                                 }
//                             }
//                         }
//                     }
//                 }
//
//                 if (agent.NeighborCount >= agent.MaxNeighbors)
//                 {
//                     break;
//                 }
//             }
//         }
//
//         // 计算RVO代理障碍物
//         public void ComputeObstacleNeighbors(Agent agent, ref FP rangeSq, FP checkRadius)
//         {
//             TSVector2 position = agent.Position;
//
//             var relativePos = position - Min;
//             var dis = TSMath.FloorToInt(checkRadius / CellSize);
//
//             var posCol = TSMath.FloorToInt(relativePos.x / CellSize); // X→Col
//             var posRow = TSMath.FloorToInt(relativePos.y / CellSize); // Y→Row
//
//             var minCol = posCol - dis;
//             var maxCol = posCol + dis;
//             var minRow = posRow - dis;
//             var maxRow = posRow + dis;
//
//             // 边界限制
//             minCol = Math.Max(0, minCol);
//             maxCol = Math.Min(Col - 1, maxCol);
//             minRow = Math.Max(0, minRow);
//             maxRow = Math.Min(Row - 1, maxRow);
//
//             for (int c = minCol; c <= maxCol; c++)
//             {
//                 for (int r = minRow; r <= maxRow; r++)
//                 {
//                     var key = GridToIndex(c, r); // (col, row)
//                     if (Cell2Objs.TryGetValue(key, out var entitiesList) == false)
//                         continue;
//                     foreach (var entity in entitiesList)
//                     {
//                         Agent agentNoById = agent._simulator.GetOnlyEvadedAgent(entity);
//                         if (agentNoById != null)
//                         {
//                             agent.InsertObsAgent(agentNoById, ref rangeSq, checkRadius);
//                         }
//                     }
//                 }
//             }
//
//         }
//     }
// }
