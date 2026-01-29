// using System;
// using System.Collections.Generic;
// using FixMath;
// using UnityEngine;
//
// namespace Battle.ECS.Core.Collision.Grid
// {
//     public class UniformGridBase<TObj> where TObj : struct
//     {
//         private readonly Stack<BetterList<int>> _betterListPool;
//         protected BetterList<int> CacheGridIds;
//         public readonly HashSet<TObj> CacheExcludeObjs = new HashSet<TObj>(1024);
//
//         public FP CellSize { get; }
//         public FP HalfCellSize { get; }
//         public FP InvCellSize { get; }
//         public int Col { get; private set; } // 列数，X轴方向的网格数量
//         public int Row { get; private set; } // 行数，Y轴方向的网格数量
//         public int MaxLength { get; private set; } // 最大边长
//
//         public Rect MapSize { get; private set; }
//         public TSVector2 Min => MapSize.Min;
//         public TSVector2 Max => MapSize.Max;
//         public TSVector2 Center => MapSize.Center;
//
//         protected readonly ObjectGridMapping<TObj> DefaultMapping;
//         protected Dictionary<int, BetterList<TObj>> Cell2Objs => DefaultMapping.Cell2Objs;
//         protected Dictionary<TObj, BetterList<int>> Obj2Cells => DefaultMapping.Obj2Cells;
//         public GridAABB ObjectBox => DefaultMapping.ObjectBox;
//         public int ObjectCount => DefaultMapping.ObjectCount;
//
//         public UniformGridBase(FP cellSize)
//         {
//             CellSize = cellSize;
//             HalfCellSize = cellSize * FP.Half;
//             InvCellSize = FP.One / cellSize;
//             DefaultMapping = new ObjectGridMapping<TObj>(this);
//             CacheGridIds = new BetterList<int>();
//
//             //预分配1024个到池子
//             int preallocated = 1024;
//             _betterListPool = new Stack<BetterList<int>>(preallocated);
//             for (int i = 0; i < preallocated; i++)
//             {
//                 _betterListPool.Push(new BetterList<int>());
//             }
//         }
//
//         /// <summary>
//         /// 使用中心位置和大小重置网格边界
//         /// </summary>
//         /// <param name="center">中心位置</param>
//         /// <param name="size">大小</param>
//         public void ResetBounds(TSVector2 center, TSVector2 size)
//         {
//             Clear();
//             size *= InvCellSize;
//             Col = TSMath.CeilToInt(size.x);
//             Row = TSMath.CeilToInt(size.y);
//             MaxLength = Math.Max(Col, Row);
//             MapSize = new Rect(center, new FP(Col) * CellSize * FP.Half, new FP(Row) * CellSize * FP.Half);
//             OnResetBounds(center, Row, Col);
//         }
//
//         protected virtual void OnResetBounds(TSVector2 center, int row, int col)
//         {
//         }
//
//         /// <summary>
//         /// 对象刷新时调用
//         /// </summary>
//         /// <param name="obj"></param>
//         /// <param name="position"></param>
//         /// <param name="halfSize"></param>
//         /// <returns>对象是否在网格中</returns>
//         public virtual bool OnObjUpdate(TObj obj, TSVector2 position, TSVector2 halfSize)
//         {
//             return DefaultMapping.OnObjUpdate(obj, position, halfSize);
//         }
//
//         public virtual bool OnObjUpdatePolygon(TObj obj, TSVector2[] vertices)
//         {
//             return DefaultMapping.OnObjUpdatePolygon(obj, vertices);
//         }
//
//         /// <summary>
//         /// 对象移除时调用
//         /// </summary>
//         /// <param name="obj"></param>
//         public virtual void OnObjRemove(TObj obj)
//         {
//             DefaultMapping.OnObjRemove(obj);
//         }
//
//         /// <summary>
//         /// 刷新objs的AABB
//         /// </summary>
//         public virtual void RefreshObjAABB()
//         {
//             DefaultMapping.RefreshObjAABB();
//         }
//
//         protected virtual void Clear()
//         {
//             DefaultMapping.Clear();
//         }
//
//         protected virtual void OnDispose()
//         {
//             Clear();
//         }
//
//         public void Dispose()
//         {
//             OnDispose();
//             _betterListPool.Clear();
//         }
//
//         /// <summary>
//         /// 获取一个BetterLis
//         /// </summary>
//         /// <returns></returns>
//         public BetterList<int> AcquireBetterList()
//         {
//             return _betterListPool.Count > 0 ? _betterListPool.Pop() : new BetterList<int>();
//         }
//
//         /// <summary>
//         /// 归还一个BetterList
//         /// </summary>
//         /// <param name="list"></param>
//         public void ReleaseBetterList(BetterList<int> list)
//         {
//             list.FastClear();
//             _betterListPool.Push(list);
//         }
//
//         /// <summary>
//         /// 获取包围盒覆盖那些格子
//         /// </summary>
//         /// <returns></returns>
//         public bool GetCoveredCells(in TSVector2 pos, FP halfX, FP halfY, ref BetterList<int> results, out GridAABB ab)
//         {
//             results.FastClear();
//             ab = default;
//             var size = new Rect(pos, halfX, halfY);
//             if (!MapSize.Intersects(size))
//             {
//                 return false;
//             }
//
//             var relativePos = pos - Min;
//             int minCol = Math.Max(TSMath.FloorToInt((relativePos.x - halfX) * InvCellSize), 0);
//             int maxCol = Math.Min(TSMath.CeilToInt((relativePos.x + halfX) * InvCellSize), Col);
//             int minRow = Math.Max(TSMath.FloorToInt((relativePos.y - halfY) * InvCellSize), 0);
//             int maxRow = Math.Min(TSMath.CeilToInt((relativePos.y + halfY) * InvCellSize), Row);
//             ab = new GridAABB(minCol, minRow, maxCol, maxRow);
//             for (int row = minRow; row < maxRow; row++)
//             for (int col = minCol; col < maxCol; col++)
//                 results.Add(GridToIndex(col, row));
//
//             return true;
//         }
//
//         private readonly TSVector2[] _polyGvBuffer = new TSVector2[8];
//         private readonly FP[] _polyDrBuffer = new FP[8];
//         private readonly FP[] _polyDcBuffer = new FP[8];
//         private readonly FP[] _polyMarginBuffer = new FP[8];
//         private readonly FP[] _polyRowStartEvalBuffer = new FP[8];
//         private readonly FP[] _polyCurrentEvalBuffer = new FP[8];
//
//         /// <summary>
//         /// 使用增量边函数扫描算法计算多边形覆盖的格子（保守覆盖）
//         /// </summary>
//         public bool GetCoveredCellsPolygon(TSVector2[] vertices, ref BetterList<int> results, out GridAABB ab)
//         {
//             results.FastClear();
//             ab = default;
//             if (vertices == null || vertices.Length < 3) return false;
//
//             int n = vertices.Length;
//             if (n > 8) return false; // 限制最大顶点数以避免越界
//
//             // 1. 计算AABB边界
//             FP minX = vertices[0].x, maxX = vertices[0].x;
//             FP minY = vertices[0].y, maxY = vertices[0].y;
//             for (int i = 1; i < n; i++)
//             {
//                 minX = TSMath.Min(minX, vertices[i].x);
//                 maxX = TSMath.Max(maxX, vertices[i].x);
//                 minY = TSMath.Min(minY, vertices[i].y);
//                 maxY = TSMath.Max(maxY, vertices[i].y);
//             }
//
//             var polyRect = new Rect(new TSVector2((minX + maxX) * FP.Half, (minY + maxY) * FP.Half), (maxX - minX) * FP.Half, (maxY - minY) * FP.Half);
//             if (!MapSize.Intersects(polyRect)) return false;
//
//             // 2. 转换到网格坐标空间
//             int minCol = Math.Max(TSMath.FloorToInt((minX - Min.x) * InvCellSize), 0);
//             int maxCol = Math.Min(TSMath.CeilToInt((maxX - Min.x) * InvCellSize), Col);
//             int minRow = Math.Max(TSMath.FloorToInt((minY - Min.y) * InvCellSize), 0);
//             int maxRow = Math.Min(TSMath.CeilToInt((maxY - Min.y) * InvCellSize), Row);
//             ab = new GridAABB(minCol, minRow, maxCol, maxRow);
//
//             // 3. 准备边函数参数 (在网格空间)
//             for (int i = 0; i < n; i++)
//             {
//                 _polyGvBuffer[i] = (vertices[i] - Min) * InvCellSize;
//             }
//
//             for (int i = 0; i < n; i++)
//             {
//                 var p0 = _polyGvBuffer[i];
//                 var p1 = _polyGvBuffer[(i + 1) % n];
//                 _polyDrBuffer[i] = p1.y - p0.y;
//                 _polyDcBuffer[i] = p1.x - p0.x;
//                 // 保守覆盖余量：0.5 * (|dr| + |dc|)
//                 _polyMarginBuffer[i] = (TSMath.Abs(_polyDrBuffer[i]) + TSMath.Abs(_polyDcBuffer[i])) * FP.Half;
//             }
//
//         // 4. 扫描AABB内的格子
//             // 缓存行初始化的边函数值
//             FP firstColCenter = new FP(minCol) + FP.Half;
//             FP firstRowCenter = new FP(minRow) + FP.Half;
//
//             for (int i = 0; i < n; i++)
//             {
//                 _polyRowStartEvalBuffer[i] = (firstColCenter - _polyGvBuffer[i].x) * _polyDrBuffer[i] - (firstRowCenter - _polyGvBuffer[i].y) * _polyDcBuffer[i];
//             }
//
//             for (int r = minRow; r < maxRow; r++)
//             {
//                 // 初始化当前行的边函数值
//                 for (int i = 0; i < n; i++)
//                 {
//                     _polyCurrentEvalBuffer[i] = _polyRowStartEvalBuffer[i];
//                 }
//
//                 for (int c = minCol; c < maxCol; c++)
//                 {
//                     bool inside = true;
//                     for (int i = 0; i < n; i++)
//                     {
//                         if (_polyCurrentEvalBuffer[i] + _polyMarginBuffer[i] < FP.Zero)
//                         {
//                             inside = false;
//                             break;
//                         }
//                     }
//
//                     if (inside)
//                     {
//                         results.Add(GridToIndex(c, r));
//                     }
//
//                     // 步进到下一个格子 (c+1)，更新所有边函数值
//                     for (int i = 0; i < n; i++)
//                     {
//                         _polyCurrentEvalBuffer[i] += _polyDrBuffer[i];
//                     }
//                 }
//
//                 // 换行时更新 rowStartEvals: E(minCol, r+1) = E(minCol, r) - dc
//                 for (int i = 0; i < n; i++)
//                 {
//                     _polyRowStartEvalBuffer[i] -= _polyDcBuffer[i];
//                 }
//             }
//
//             return results.Count > 0;
//         }
//
//         /// <summary>
//         ///  用一个包围盒查询所有对象
//         /// </summary>
//         /// <param name="position"></param>
//         /// <param name="halfSize"></param>
//         /// <param name="objects"></param>
//         public void QueryObjects(in TSVector2 position, TSVector2 halfSize, HashSet<TObj> objects)
//         {
//             QueryObjects(position, halfSize.x, halfSize.y, objects);
//         }
//
//         /// <summary>
//         /// 用宽/高查询所有对象
//         /// 用宽/高查询所有对象
//         /// </summary>
//         /// <param name="position"></param>
//         /// <param name="halfSizeX"></param>
//         /// <param name="halfSizeY"></param>
//         /// <param name="objects"></param>
//         public void QueryObjects(in TSVector2 position, in FP halfSizeX, in FP halfSizeY, HashSet<TObj> objects)
//         {
//             if (!GetCoveredCells(position, halfSizeX, halfSizeY, ref CacheGridIds, out var _))
//             {
//                 return;
//             }
//
//             var count = CacheGridIds.Count;
//             for (int i = 0; i < count; i++)
//             {
//                 if (Cell2Objs.TryGetValue(CacheGridIds[i], out var list))
//                 {
//                     for (int j = 0; j < list.Count; j++)
//                         objects.Add(list[j]);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// 获取对应格子的所有对象
//         /// </summary>
//         /// <param name="col"></param>
//         /// <param name="row"></param>
//         /// <returns></returns>
//         public BetterList<TObj> GetCellObjects(int col, int row)
//         {
//             var key = GridToIndex(col, row);
//             return Cell2Objs.GetValueOrDefault(key);
//         }
//
//         /// <summary>
//         /// 获取对应格子的对象数量
//         /// </summary>
//         /// <param name="col"></param>
//         /// <param name="row"></param>
//         /// <returns></returns>
//         public int GetCellCount(int col, int row)
//         {
//             var key = GridToIndex(col, row);
//             return Cell2Objs.TryGetValue(key, out var list) ? list.Count : 0;
//         }
//
//         /// <summary>
//         /// 世界坐标转换为格子坐标
//         /// </summary>
//         /// <param name="worldPos"></param>
//         /// <param name="gridPos"></param>
//         /// <returns>坐标在地图内返回true,否则返回false</returns>
//         public bool WorldToGrid(in TSVector2 worldPos, out (int x, int y) gridPos)
//         {
//             if (!MapSize.Contains(worldPos))
//             {
//                 gridPos = default;
//                 return false;
//             }
//
//             gridPos = WorldToGrid(in worldPos);
//             return true;
//         }
//
//         /// <summary>
//         /// 世界坐标转换为格子坐标
//         /// </summary>
//         /// <param name="worldPos"></param>
//         /// <returns>返回的坐标没有限制</returns>
//         public (int x, int y) WorldToGrid(in TSVector2 worldPos)
//         {
//            var tempPos = (worldPos - Min) * InvCellSize;
//             return new(TSMath.FloorToInt(tempPos.x), TSMath.FloorToInt(tempPos.y));
//         }
//         /// <summary>
//         /// 格子坐标转换为世界坐标 (格子中心点)
//         /// </summary>
//         /// <param name="col"></param>
//         /// <param name="row"></param>
//         /// <returns></returns>
//         public TSVector3 GridToWorld(int col, int row)
//         {
//             var x = Min.x + (new FP(col) + FP.Half) * CellSize;
//             var z = Min.y + (new FP(row) + FP.Half) * CellSize;
//             return new TSVector3(x, FP.Zero, z);
//         }
//
//         /// <summary>
//         /// 格子坐标转换为索引 (标准的2D到1D索引转换)
//         /// </summary>
//         /// <param name="col"></param>
//         /// <param name="row"></param>
//         /// <returns></returns>
//         public int GridToIndex(int col, int row)
//         {
//             return row * Col + col;
//         }
//
//         /// <summary>
//         /// 坐标是否在网格内
//         /// </summary>
//         /// <param name="col"></param>
//         /// <param name="row"></param>
//         /// <returns></returns>
//         public bool ContainsGrid(int col, int row)
//         {
//             return col >= 0 && col < Col && row >= 0 && row < Row;
//         }
//     }
// }
