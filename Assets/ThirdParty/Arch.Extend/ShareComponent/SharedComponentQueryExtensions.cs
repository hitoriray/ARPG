// using Arch.Core;
// using System;
// using System.Collections.Generic;
//
// namespace Arch.Extend.SharedComponent
// {
//     /// <summary>
//     /// 提供针对共享组件的查询扩展方法
//     /// </summary>
//     public static class SharedComponentQueryExtensions
//     {
//         /// <summary>
//         /// 创建查询，筛选具有特定共享组件的实体
//         /// </summary>
//         public static QueryDescription WithSharedComponent<T>(this QueryDescription query, in T sharedComponent, SharedComponentManager manager) 
//             where T : struct, ISharedComponent<T>
//         {
//             var id = manager.GetSharedComponentId(sharedComponent);
//             return query.WithAll<SharedComponentRef<T>>();
//         }
//
//         /// <summary>
//         /// 按共享组件分组处理实体
//         /// </summary>
//         public static void GroupBySharedComponent<TShared, TData>(
//             this World world, 
//             SharedComponentManager manager,
//             Action<TShared, Span<TData>> processGroup)
//             where TShared : struct, ISharedComponent<TShared>
//             where TData : struct
//         {
//             // 获取所有包含指定共享组件引用和数据组件的实体
//             var query = new QueryDescription().WithAll<SharedComponentRef<TShared>, TData>();
//         
//             // 按共享组件ID分组
//             var groups = new Dictionary<int, List<TData>>();
//             
//             world.Query(in query, (in SharedComponentRef<TShared> sharedRef, in TData data) =>
//             {
//                 if (!groups.ContainsKey(sharedRef.Id))
//                 {
//                     groups[sharedRef.Id] = new List<TData>();
//                 }
//                 groups[sharedRef.Id].Add(data);
//             });
//             
//             // 处理每个分组
//             foreach ((int id, List<TData> dataList) in groups)
//             {
//                 TShared sharedComponent = manager.GetSharedComponent<TShared>(id);
//                 processGroup(sharedComponent, dataList.AsSpan());
//             }
//         }
//     }
//
// }