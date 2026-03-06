using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JKFrame;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// 基于 Addressables 的子场景动态加载管理器（单例）。
    /// 
    /// 用法：
    ///   1. 在场景中放置一个 GameObject 挂上此脚本（或让 GameSceneManager 持有引用）。
    ///   2. 在地图上放置 SceneLoadTrigger，配置好 addressKey 后，玩家进入触发器时自动加载子场景。
    /// </summary>
    public class SubSceneLoader : SingletonMono<SubSceneLoader>
    {
        // ── 私有状态 ──────────────────────────────────────────────
        // key = Addressables Address 字符串，value = 加载句柄
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _loaded = new();
        // 正在加载中的地址（防重复触发）
        private readonly HashSet<string> _loading = new();
        // 正在卸载中的地址（防重复触发）
        private readonly HashSet<string> _unloading = new();

        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// 异步加载一个 Addressables 场景（Additive 模式叠加到当前场景）。
        /// 已加载则跳过。
        /// </summary>
        public async UniTask LoadSceneAsync(string addressKey)
        {
            if (string.IsNullOrEmpty(addressKey)) return;
            if (_loaded.ContainsKey(addressKey)) return;   // 已加载
            if (_loading.Contains(addressKey)) return;      // 正在加载

            _loading.Add(addressKey);
            try
            {
                JKLog.Log($"[SubSceneLoader] 开始加载子场景：{addressKey}");

                var handle = Addressables.LoadSceneAsync(
                    addressKey,
                    LoadSceneMode.Additive,
                    activateOnLoad: true);

                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loaded[addressKey] = handle;
                    JKLog.Log($"[SubSceneLoader] 子场景加载完成：{addressKey}");
                }
                else
                {
                    JKLog.Error($"[SubSceneLoader] 子场景加载失败：{addressKey}");
                    Addressables.Release(handle);
                }
            }
            catch (Exception e)
            {
                JKLog.Error($"[SubSceneLoader] 加载异常 [{addressKey}]：{e.Message}");
            }
            finally
            {
                _loading.Remove(addressKey);
            }
        }

        /// <summary>
        /// 异步卸载一个已加载的 Addressables 子场景。
        /// </summary>
        public async UniTask UnloadSceneAsync(string addressKey)
        {
            if (string.IsNullOrEmpty(addressKey)) return;
            if (!_loaded.TryGetValue(addressKey, out var handle)) return;
            if (_unloading.Contains(addressKey)) return;

            _unloading.Add(addressKey);
            _loaded.Remove(addressKey);
            try
            {
                JKLog.Log($"[SubSceneLoader] 开始卸载子场景：{addressKey}");
                var unloadHandle = Addressables.UnloadSceneAsync(handle);
                await unloadHandle.ToUniTask();
                JKLog.Log($"[SubSceneLoader] 子场景卸载完成：{addressKey}");
            }
            catch (Exception e)
            {
                JKLog.Error($"[SubSceneLoader] 卸载异常 [{addressKey}]：{e.Message}");
            }
            finally
            {
                _unloading.Remove(addressKey);
            }
        }

        /// <summary>是否已加载指定子场景</summary>
        public bool IsLoaded(string addressKey) => _loaded.ContainsKey(addressKey);

        /// <summary>卸载所有已加载的子场景（切场景时调用）</summary>
        public async UniTask UnloadAllAsync()
        {
            var keys = new List<string>(_loaded.Keys);
            foreach (var key in keys)
                await UnloadSceneAsync(key);
        }

        private void OnDestroy()
        {
            // 同步清理（应用退出时不能 await）
            foreach (var kv in _loaded)
                Addressables.Release(kv.Value);
            _loaded.Clear();
        }
    }
}
