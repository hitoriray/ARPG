// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
//
// namespace Battle.ECS.Core.Collision.Grid
// {
//     /// <summary>
//     /// 环形网格, 用于快速获取指定半径的网格
//     /// </summary>
//     public class RingGrids
//     {
//         private (int, int)[] _rings;
//         private int[] _ringIndex; // 每个半径对应 [start, end]
//         private int _ringCount;
//         private int _usedCount;
//         private int _width;
//         private int _height;
//
//         public int RingCount => _ringCount;
//
//         public RingGrids(int width, int height)
//         {
//             Init(width, height);
//         }
//
//         public RingGrids()
//         {
//
//         }
//
//         /// <summary>
//         /// 初始化矩形地图范围的环形偏移。AOI的两倍大
//         /// </summary>
//         public void Init(int width, int height)
//         {
//             _width = width;
//             _height = height;
//
//             var sw = Stopwatch.StartNew();
//
//             int halfW = width / 2;
//             int halfH = height / 2;
//             int maxRadius = Math.Max(halfW, halfH);
//
//             var list = new List<(int, int)>(width * height);
//             _ringCount = maxRadius + 1;
//             _ringIndex = new int[_ringCount * 2];
//
//             for (int r = 0; r <= maxRadius; r++)
//             {
//                 _ringIndex[r * 2] = list.Count;
//
//                 if (r == 0)
//                 {
//                     list.Add((0, 0));
//                     _ringIndex[r * 2 + 1] = list.Count - 1;
//                     continue;
//                 }
//
//                 // 上下边
//                 for (int x = -r; x <= r; x++)
//                 {
//                     if (Math.Abs(x) <= halfW)
//                     {
//                         if (r <= halfH) list.Add((x, r));
//                         if (r <= halfH) list.Add((x, -r));
//                     }
//                 }
//
//                 // 左右边
//                 for (int y = -r + 1; y <= r - 1; y++)
//                 {
//                     if (Math.Abs(y) <= halfH)
//                     {
//                         if (r <= halfW) list.Add((-r, y));
//                         if (r <= halfW) list.Add((r, y));
//                     }
//                 }
//
//                 _ringIndex[r * 2 + 1] = list.Count - 1;
//             }
//
//             _rings = list.ToArray();
//             _usedCount = _rings.Length;
//             sw.Stop();
//
// #if UNITY_EDITOR
//             UnityEngine.Debug.Log($"[RingGrids] 初始化完成: 地图={width}x{height}, 圈数={_ringCount}, 精确分配={_usedCount}, 耗时={sw.Elapsed.TotalMilliseconds:F3} ms");
// #endif
//         }
//
//         /// <summary>
//         /// 获取指定半径的环
//         /// </summary>
//         /// <param name="radius">半径>=0</param>
//         /// <param name="result">迭代器</param>
//         /// <returns></returns>
//         public bool GetRingGrids(int radius, out RangeEnumerable<(int, int)> result)
//         {
//             if (radius < 0 || radius >= _ringCount)
//             {
//                 result = default;
//                 return false;
//             }
//
//             int idx = radius * 2;
//             result = _rings.AsRangeEnumerable(_ringIndex[idx], _ringIndex[idx + 1]);
//             return true;
//         }
//
//     }
// }