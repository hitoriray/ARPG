// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using Arch.Core;
// using Arch.Core.Extensions;
// using Battle.Core.Collision.Grid;
// using Battle.ECS.Helper;
// using FixMath;
// namespace Battle.ECS.Core.Collision.Grid
// {
//     /// <summary>
//     /// 索敌网格，在索敌逻辑时清理并重新填充
//     /// </summary>
//     public class SeekGrid : UniformGridBase<Entity>
//     {
//         private readonly BattleContext _context;
//         public readonly HashSet<Entity> CacheEntities = new HashSet<Entity>(1024);
//         public readonly HashSet<Entity> CacheExcludeEntities = new HashSet<Entity>(1024);
//
//         // 搜索队列
//         public readonly Queue<(int col, int row)> SeekQueue = new Queue<(int col, int row)>(512);
//         // 搜索结果
//         public readonly HashSet<(int col, int row)> SeekVisited = new HashSet<(int col, int row)>(512);
//         public readonly (int col, int row)[] Neighbors = new (int col, int row)[]
//         {
//             (0, 1), // 上
//             (1, 0), // 右
//             (0, -1), // 下
//             (-1, 0), // 左
//         };
//
//         protected override void OnDispose()
//         {
//             CacheClear();
//         }
//
//         protected override void Clear()
//         {
//             CacheClear();
//             base.Clear();
//         }
//
//         public void CacheClear()
//         {
//             CacheEntities.Clear();
//             CacheExcludeEntities.Clear();
//             SeekQueue.Clear();
//             SeekVisited.Clear();
//         }
//
//         public SeekGrid(BattleContext context) : base(new FP(3))
//         {
//             _context = context;
//             RingGrid = new RingGrids(100, 100);
//         }
//
//         public void ClearAndQueryGrid(QueryDescription desc)
//         {
//             ProfilerHelper.Begin("SeekGrid.ClearAndQueryGrid");
//
//             ProfilerHelper.Begin("SeekGrid.ClearAndQueryGrid.ResetBounds");
//             CacheEntities.Clear();
//
//             var aoi = _context.Aoi;
//             // 使用AOI的中心位置和大小来重置边界
//             ResetBounds(aoi.Value.center, aoi.Value.size);
//             ProfilerHelper.End();
//
//             ProfilerHelper.Begin("SeekGrid.ClearAndQueryGrid.Query");
//             var query = _context.World.Query(desc);
//             ProfilerHelper.End();
//
//             ProfilerHelper.Begin("SeekGrid.ClearAndQueryGrid.TryAddEntity");
//             foreach (Chunk chunk in query)
//             {
//                 ref var firstEntity = ref chunk.Entity(0);
//                 ref var firstPosition = ref chunk.GetFirst<Position>();
//                 ref var firstBox = ref chunk.GetFirst<BoundingBox>();
//
//                 foreach (int entityIndex in chunk)
//                 {
//                     ref var entity = ref Unsafe.Add(ref firstEntity, entityIndex);
//                     ref var position = ref Unsafe.Add(ref firstPosition, entityIndex);
//                     ref var box = ref Unsafe.Add(ref firstBox, entityIndex);
//                     OnObjUpdate(entity, position.Value.XZToTSVector2(), box.Max);
//                 }
//             }
//             ProfilerHelper.End();
//
//             ProfilerHelper.End();
//         }
//
//         public HashSet<Entity> QueryEntities(in TSVector2 position, in FP x, in FP y)
//         {
//             CacheEntities.Clear();
//             base.QueryObjects(in position, in x, in y, CacheEntities);
//             return CacheEntities;
//         }
//
//         #region 密集区域搜索
//         public TSVector2 FindDensestPoint(TSVector2 checkSize, int minColStart, int minRowStart, int maxColEnd, int maxRowEnd, out int maxDensity, out TSVector2 startPos, out TSVector2 size)
//         {
//             int sideX = SizeNormalize(checkSize.x, Col);
//             int sideY = SizeNormalize(checkSize.y, Row);
//
//             // 边界修正：确保sideX和sideY不超过限定范围内的可用空间
//             int availableColSpace = maxColEnd - minColStart;
//             int availableRowSpace = maxRowEnd - minRowStart;
//             sideX = Math.Min(sideX, availableColSpace);
//             sideY = Math.Min(sideY, availableRowSpace);
//
//             maxDensity = -1;
//             int startX = 0;
//             int startY = 0;
//             startPos = TSVector2.zero;
//             size = TSVector2.zero;
//
//             // 遍历网格，从指定的起始位置到结束位置
//             maxDensity = GetMaxDensityAndIntPos(maxDensity, sideX, sideY, minColStart, minRowStart, maxColEnd, maxRowEnd, ref startX, ref startY);
//             Have = maxDensity > 0;
//             if (maxDensity <= 0)
//                 return TSVector2.zero;
//
//             startPos = Min + new TSVector2(startX, startY) * CellSize;
//             size = new TSVector2(sideX * CellSize, sideY * CellSize);
//
//             StartPos = startPos;
//             Size = size;
//
//             var point = CalAveragePos(startX, sideX, sideY, startY);
//             return point.XZToTSVector2();
//         }
//
//         private int SizeNormalize(FP size, int maxSize)
//         {
//             int side = TSMath.CeilToInt(size / CellSize);
//             return Math.Clamp(side, 1, maxSize);
//         }
//
//         private int GetMaxDensityAndIntPos(int maxDensity, int sideX, int sideY, int minColStart, int minRowStart, int maxColEnd, int maxRowEnd, ref int startX, ref int startY)
//         {
//             // 计算范围内的有效遍历区间
//             int maxColPos = Math.Min(maxColEnd - sideX, Col - sideX);
//             int maxRowPos = Math.Min(maxRowEnd - sideY, Row - sideY);
//
//             // 仅当有效起点小于等于结束点时才进行遍历
//             if (minColStart > maxColPos || minRowStart > maxRowPos)
//             {
//                 return maxDensity;
//             }
//
//             for (int col = minColStart; col <= maxColPos; col++) // X轴方向，限制于指定范围
//             {
//                 for (int row = minRowStart; row <= maxRowPos; row++) // Y轴方向，限制于指定范围
//                 {
//                     int density = 0;
//                     for (int dy = 0; dy < sideY; dy++)
//                     {
//                         for (int dx = 0; dx < sideX; dx++)
//                         {
//                             int gridCol = col + dx;
//                             int gridRow = row + dy;
//                             density += GetCellCount(gridCol, gridRow);
//                         }
//                     }
//
//                     if (density > maxDensity)
//                     {
//                         maxDensity = density;
//                         startX = col;
//                         startY = row;
//                     }
//                 }
//             }
//             return maxDensity;
//         }
//
//         private TSVector3 CalAveragePos(int startX, int sideX, int sideY, int startY)
//         {
//             int count = 0;
//             TSVector3 point = TSVector3.zero;
//
//             for (int row = startY; row < startY + sideY; row++) // Y轴
//             {
//                 for (int col = startX; col < startX + sideX; col++) // X轴
//                 {
//                     var list = GetCellObjects(col, row);
//                     if (list == null) continue;
//                     for (int index = 0; index < list.Count; index++)
//                     {
//                         Entity key = list[index];
//                         TSVector2 position = key.Get<Position>().Value.XZToTSVector2();
//                         point += position.ToTSVector3XZ();
//                         count++;
//                     }
//                 }
//             }
//
//             if (count > 0)
//                 point /= count;
//             return point;
//         }
//
//         #region 测试数据
//         public TSVector2 StartPos { get; private set; }
//         public TSVector2 Size { get; private set; }
//         public bool Have { get; private set; }
//         #endregion
//         #endregion
//
//         public RingGrids RingGrid { get; private set; }
//     }
// }
