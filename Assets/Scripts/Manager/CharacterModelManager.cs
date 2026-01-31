using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using JKFrame;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using JKLog = JK.Log.JKLog;

namespace Manager
{
    /// <summary>
    /// 角色模型资源管理器：负责管理角色模型预制体和配置的加载/卸载
    /// </summary>
    public class CharacterModelManager : SingletonMono<CharacterModelManager>
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
            if (_loadedPrefabs.TryGetValue(characterId, out var existHandle))
            {
                return existHandle.Result;
            }
            
            var entry = _characterTable.GetCharacterById(characterId);
            if (entry == null)
            {
                JKLog.Error($"[{nameof(CharacterModelManager)}] 角色ID {characterId} 不存在于配置表中！");
                return null;
            }
            
            var handle = entry.CharacterModelPrefab.LoadAssetAsync<GameObject>();
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedPrefabs[characterId] = handle;
                JKLog.Log($"[{nameof(CharacterModelManager)}] 成功加载角色模型预制体: {entry.CharacterName} (ID: {characterId})");    
                return handle.Result;
            }
            else
            {
                JKLog.Error($"[{nameof(CharacterModelManager)}] 加载角色模型预制体失败: {entry.CharacterName} (ID: {characterId})");   
                return null;
            }
        }

        /// <summary>
        /// 异步加载角色配置
        /// </summary>
        public async UniTask<CharacterConfig> LoadCharacterConfigAsync(int characterId)
        {
            // 如果已经加载，直接返回
            if (_loadedConfigs.TryGetValue(characterId, out var existingHandle))
            {
                return existingHandle.Result;
            }

            var entry = _characterTable.GetCharacterById(characterId);
            if (entry == null)
            {
                JKLog.Error($"[{nameof(CharacterModelManager)}] 角色ID {characterId} 不存在于配置表中！");
                return null;
            }

            // 加载配置资源
            var handle = entry.CharacterConfig.LoadAssetAsync<CharacterConfig>();
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedConfigs[characterId] = handle;
                JKLog.Log($"[{nameof(CharacterModelManager)}] 成功加载角色配置: {entry.CharacterName} (ID: {characterId})");      
                return handle.Result;
            }
            else
            {
                JKLog.Error($"[{nameof(CharacterModelManager)}] 加载角色配置失败: {entry.CharacterName} (ID: {characterId})");     
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
            JKLog.Log($"[{nameof(CharacterModelManager)}] 已为Player替换外观: {entry.CharacterName}");

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

            JKLog.Log($"[{nameof(CharacterModelManager)}] 已卸载角色ID {characterId} 的资源");
        }
        
        /// <summary>
        /// 卸载所有角色资源（场景切换时调用）
        /// </summary>
        public void UnloadAllCharacters()
        {
            foreach (var handle in _loadedPrefabs.Values)
            {
                Addressables.Release(handle);
            }
            _loadedPrefabs.Clear();

            foreach (var handle in _loadedConfigs.Values)
            {
                Addressables.Release(handle);
            }
            _loadedConfigs.Clear();

            JKLog.Log($"[{nameof(CharacterModelManager)}] 已卸载所有角色资源");
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