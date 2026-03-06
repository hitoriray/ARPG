using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using JKFrame;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Manager
{
    /// <summary>
    /// 角色模型资源管理器：负责管理角色模型预制体和配置的加载/卸载
    /// </summary>
    public class CharacterModelManager : MonoSingleton<CharacterModelManager>
    {
        private CharacterTable _characterTable;
        // 已加载的角色模型预制体缓存  Key:角色ID
        private Dictionary<int, AsyncOperationHandle<GameObject>> _loadedPrefabs = new();
        // 已加载的角色配置缓存  Key:角色ID
        private Dictionary<int, AsyncOperationHandle<CharacterConfig>> _loadedConfigs = new();

        protected override void Awake()
        {
            base.Awake();
            _characterTable = ResSystem.LoadAsset<CharacterTable>("CharacterTable");
        }
        
        #region 核心加载方法

        /// <summary>
        /// 异步加载角色模型预制体（不实例化）
        /// </summary>
        public async UniTask<GameObject> LoadCharacterModelPrefabAsync(int characterId)
        {
            // 1. 先查内存缓存（字典中已有有效 handle）
            if (_loadedPrefabs.TryGetValue(characterId, out var existHandle))
            {
                if (existHandle.IsValid())
                    return existHandle.Result;

                // handle 已失效（场景切换等情况），从缓存中清除再重新加载
                _loadedPrefabs.Remove(characterId);
            }

            var entry = _characterTable.GetCharacterById(characterId);
            if (entry == null)
            {
                RayDebug.Error($"[{nameof(CharacterModelManager)}] 角色ID {characterId} 不存在于配置表中！");
                return null;
            }

            // 2. 若 AssetReference 已有有效的操作 handle（被其他路径加载过），直接复用
            var assetRef = entry.CharacterModelPrefab;
            if (assetRef.OperationHandle.IsValid())
            {
                // 等待已有加载操作完成（可能还在进行中）
                var existingOp = assetRef.OperationHandle.Convert<GameObject>();
                await existingOp.ToUniTask();
                if (existingOp.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedPrefabs[characterId] = existingOp;
                    return existingOp.Result;
                }
                RayDebug.Error($"复用已有 Handle 失败: {entry.CharacterName} (ID: {characterId})");
                return null;
            }

            // 3. 全新加载
            var handle = assetRef.LoadAssetAsync<GameObject>();
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedPrefabs[characterId] = handle;
                RayDebug.Log($"成功加载角色模型预制体: {entry.CharacterName} (ID: {characterId})");
                return handle.Result;
            }

            RayDebug.Error($"加载角色模型预制体失败: {entry.CharacterName} (ID: {characterId})");
            return null;
        }

        /// <summary>
        /// 异步加载角色配置
        /// </summary>
        public async UniTask<CharacterConfig> LoadCharacterConfigAsync(int characterId)
        {
            // 如果已经加载且 handle 有效，直接返回
            if (_loadedConfigs.TryGetValue(characterId, out var existingHandle))
            {
                if (existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
                    return existingHandle.Result;

                _loadedConfigs.Remove(characterId);
            }

            var entry = _characterTable.GetCharacterById(characterId);
            if (entry == null)
            {
                RayDebug.Error($"角色ID {characterId} 不存在于配置表中！");
                return null;
            }

            // 加载配置资源
            var handle = entry.CharacterConfig.LoadAssetAsync<CharacterConfig>();
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedConfigs[characterId] = handle;
                RayDebug.Info($"成功加载角色配置: {entry.CharacterName} (ID: {characterId})");
                return handle.Result;
            }
            else
            {
                RayDebug.Error($"加载角色配置失败: {entry.CharacterName} (ID: {characterId})");
                return null;
            }
        }
        
        /// <summary>
        /// 为已存在的Player实例动态替换角色外观
        /// </summary>
        /// <param name="playerRoot">Player根节点（包含PlayerController的GameObject）</param>
        /// <param name="characterId">角色ID</param>
        /// <param name="modelParentName">PlayerModel的挂载点名称（默认为"PlayerModel"）</param>
        public async UniTask<GameObject> ReplaceCharacterModelAsync(GameObject playerRoot, int characterId, string
            modelParentName = "PlayerModel")
        {
            // 1. 加载新的外观预制体
            var modelPrefab = await LoadCharacterModelPrefabAsync(characterId);
            if (modelPrefab == null) return null;

            // 2. 查找并销毁旧的PlayerModel
            Transform oldModel = playerRoot.transform.Find(modelParentName);
            if (oldModel != null)
            {
                Destroy(oldModel.gameObject);
            }

            // 3. 实例化新的PlayerModel
            var newModel = Instantiate(modelPrefab, playerRoot.transform);
            newModel.name = modelParentName;

            var entry = _characterTable.GetCharacterById(characterId);
            RayDebug.Log($"已为Player替换外观: {entry.CharacterName}");

            return newModel;
        }
        
        #endregion
        
        #region 卸载方法

        /// <summary>
        /// 卸载指定角色的外观资源
        /// </summary>
        public void UnloadCharacterModel(int characterId)
        {
            if (_loadedPrefabs.TryGetValue(characterId, out var modelHandle))
            {
                Addressables.Release(modelHandle);
                _loadedPrefabs.Remove(characterId);
            }

            if (_loadedConfigs.TryGetValue(characterId, out var configHandle))
            {
                Addressables.Release(configHandle);
                _loadedConfigs.Remove(characterId);
            }

            RayDebug.Log($"已卸载角色ID {characterId} 的资源");
        }
        
        /// <summary>
        /// 卸载所有角色资源（场景切换时调用）
        /// </summary>
        public void UnloadAllCharacters()
        {
            foreach (var kvp in _loadedPrefabs)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            _loadedPrefabs.Clear();

            foreach (var kvp in _loadedConfigs)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            _loadedConfigs.Clear();

            RayDebug.Log($"已卸载所有角色资源");
        }
        
        #endregion
        
        #region 预加载与优化

        /// <summary>
        /// 预加载多个角色（后台加载，不阻塞）
        /// </summary>
        public void PreloadCharacters(List<int> characterIds)
        {
            foreach (var id in characterIds)
            {
                LoadCharacterModelPrefabAsync(id).Forget();
                LoadCharacterConfigAsync(id).Forget();
            }
        }

        /// <summary>
        /// 获取角色名称
        /// </summary>
        public string GetCharacterName(int characterId)
        {
            var entry = _characterTable.GetCharacterById(characterId);
            return entry?.CharacterName ?? "未知角色";
        }

        #endregion

        private void OnDestroy()
        {
            UnloadAllCharacters();
        }
    }
}
