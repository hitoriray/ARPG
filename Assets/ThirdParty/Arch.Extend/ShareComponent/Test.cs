// using Arch.Core;
// using System.Collections.Generic;
// using GameLogic.Battle.Client;
// using GameLogic.Battle.Component.G2E.Render;
// using Libs.GoToEntity.Extend;
// using Libs.GoToEntity.Extend.ShareComponent;
// using UnityEngine;
//
// namespace Libs.GoToEntity.Runtime.Extend.ShareComponent
// {
//     public class GameInitialization
//     {
//         public void Initialize(World world)
//         {
//             // 1. 创建共享组件管理器
//             var sharedComponentManager = new SharedComponentManager(world);
//
//             // 2. 加载资源
//             var meshes = new Dictionary<string, Mesh>
//             {
//                 {"player", LoadMesh("player")},
//                 {"enemy", LoadMesh("enemy")}
//             };
//
//             var materials = new Dictionary<string, Material>
//             {
//                 {"red", LoadMaterial("red")},
//                 {"blue", LoadMaterial("blue")}
//             };
//
//             // 3. 创建使用相同共享组件的实体（会被分到同一Chunk）
//             CreateEntitiesWithSharedComponent(world, sharedComponentManager);
//         }
//
//         private void CreateEntitiesWithSharedComponent(World world, SharedComponentManager manager)
//         {
//             // 定义共享组件 - 玩家网格+红色材质
//             var playerRedShared = new MeshMaterialSharedComponent
//             {
//                 MeshName = "player",
//                 MaterialName = "red"
//             };
//
//             // 创建多个使用相同共享组件的实体
//             for (int i = 0; i < 100; i++)
//             {
//                 var entity = world.Create();
//
//                 // 添加共享组件
//                 manager.AddSharedComponent(entity, playerRedShared);
//
//                 // 添加实体特定的渲染数据
//                 world.Add(entity, new GpuSkinRender
//                 {
//                     Matrix = Matrix4x4.CreateTranslation(i * 2, 0, 0), // 位置不同
//                     AniParam = new Vector4(0, 1, 0, 0),
//                     ColorParam = new Vector4(1, 1, 1, 1)
//                 });
//             }
//
//             // 定义另一个共享组件 - 敌人网格+蓝色材质
//             var enemyBlueShared = new MeshMaterialSharedComponent
//             {
//                 MeshName = "enemy",
//                 MaterialName = "blue"
//             };
//
//             // 创建使用另一组共享组件的实体
//             for (int i = 0; i < 50; i++)
//             {
//                 var entity = world.Create();
//                 manager.AddSharedComponent(entity, enemyBlueShared);
//                 world.Add(entity, new GpuSkinRender
//                 {
//                     Matrix = Matrix4x4.CreateTranslation(i * 2, 5, 0),
//                     AniParam = new Vector4(1, 0, 0, 0),
//                     ColorParam = new Vector4(1, 1, 1, 1)
//                 });
//             }
//         }
//
//         // 模拟资源加载方法
//         private Mesh LoadMesh(string name) => new Mesh();
//         private Material LoadMaterial(string name) => new Material();
//     }
// }