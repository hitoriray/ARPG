using System;
using System.Collections.Generic;
using Attribute;
using JKFrame;
using Manager;
using UnityEngine;

namespace UI
{
    [DefaultExecutionOrder(800)]
    public sealed class WorldHeadUIManager : MonoBehaviour
    {
        private sealed class TrackEntry
        {
            public Transform Target;
            public CharacterAttribute Attribute;
            public Action<float, float> HpChangedHandler;
            public UI_WorldHeadItem Item;
            public Vector3 Offset;
            public bool IsNpc;
            public bool IsBoss;
            public bool ShowHp;
            public bool DistanceVisible;
            public float CurrentHp;
            public float MaxHp;
            public float CombatVisibleUntil;
        }

        private sealed class PendingRegister
        {
            public Transform Target;
            public CharacterAttribute Attribute;
            public string DisplayName;
            public bool IsNpc;
            public bool IsBoss;
            public bool ShowHp;
            public Vector3 Offset;
        }

        private static WorldHeadUIManager _instance;
        private static bool _isQuitting;
        private static bool _missingInstanceLogged;
        private const string ItemPoolKey = "WorldHeadUIManager_ItemPool";

        [Header("Canvas")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform itemRoot;
        [SerializeField] private UI_WorldHeadItem itemPrefab;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int sortingOrder = 18;
        [SerializeField] private bool applyCanvasSorting = false;
        [SerializeField] private bool autoResolveCanvasAtRuntime = true;
        [SerializeField] private string preferredLayoutName = "Layout4";
        [SerializeField] private string itemRootName = "WorldHeadRoot";
        [SerializeField] private bool autoCreateItemRoot = true;
        [SerializeField] private bool hideWhenModalUIOpen = true;

        [Header("Update")]
        [SerializeField, Min(0.01f)] private float updateInterval = 0.033f;
        [SerializeField] private bool hideWhenBehindCamera = true;

        [Header("Distance - Hostile")]
        [SerializeField, Min(0f)] private float enemyShowDistance = 28f;
        [SerializeField, Min(0f)] private float enemyHideDistance = 32f;
        [SerializeField, Min(0f)] private float bossShowDistance = 40f;
        [SerializeField, Min(0f)] private float bossHideDistance = 46f;
        [SerializeField, Range(0f, 1f)] private float hostileFadeStartRatio = 0.8f;

        [Header("Distance - NPC")]
        [SerializeField, Min(0f)] private float npcShowDistance = 12f;
        [SerializeField, Min(0f)] private float npcHideDistance = 14f;
        [SerializeField, Range(0f, 1f)] private float npcFadeStartRatio = 0.8f;

        [Header("Elden Ring Style")]
        [SerializeField, Min(0f)] private float hostileDisplayHoldSeconds = 4f;
        [SerializeField, Min(0f)] private float hostileHeadOffsetY = 2.1f;
        [SerializeField, Min(0f)] private float npcHeadOffsetY = 1.9f;
        [SerializeField] private bool keepFixedSize = true;
        [SerializeField, Range(0.7f, 1f)] private float minDistanceScale = 0.85f;

        private readonly Dictionary<int, TrackEntry> _entries = new();
        private readonly Dictionary<int, PendingRegister> _pendingRegistrations = new();
        private float _updateTimer;
        private bool _canvasMissingLogged;
        private bool _itemRootMissingLogged;

        public static WorldHeadUIManager Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<WorldHeadUIManager>(FindObjectsInactive.Include);
                if (_instance == null && !_missingInstanceLogged)
                {
                    Debug.LogError("[WorldHeadUIManager] No scene instance found. Please place and configure WorldHeadUIManager in the scene.");
                    _missingInstanceLogged = true;
                }
                return _instance;
            }
        }

        public static WorldHeadUIManager EnsureInstance() => Instance;

        public static WorldHeadUIManager TryGetExistingInstance()
        {
            if (_isQuitting)
                return null;

            if (_instance != null)
                return _instance;

            _instance = FindAnyObjectByType<WorldHeadUIManager>(FindObjectsInactive.Include);
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _isQuitting = false;
            _missingInstanceLogged = false;
            _instance = this;
            EnsureCanvas();
        }

        private void LateUpdate()
        {
            FlushPendingRegistrations();

            if (_entries.Count == 0)
                return;

            _updateTimer += Time.unscaledDeltaTime;
            if (_updateTimer < updateInterval)
                return;

            _updateTimer = 0f;
            UpdateEntries();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            foreach (var entry in _entries.Values)
            {
                UnsubscribeHp(entry);
            }

            _entries.Clear();
            _pendingRegistrations.Clear();
        }

        public void RegisterHostile(Transform target, CharacterAttribute characterAttribute, string displayName, bool isBoss = false)
        {
            RegisterInternal(target, characterAttribute, displayName, isNpc: false, isBoss: isBoss, showHp: true,
                new Vector3(0f, hostileHeadOffsetY, 0f));
        }

        public void RegisterNpc(Transform target, string displayName)
        {
            RegisterInternal(target, null, displayName, isNpc: true, isBoss: false, showHp: false,
                new Vector3(0f, npcHeadOffsetY, 0f));
        }

        public void Unregister(Transform target)
        {
            if (target == null)
                return;

            int key = target.GetInstanceID();
            _pendingRegistrations.Remove(key);
            if (!_entries.TryGetValue(key, out var entry))
                return;

            _entries.Remove(key);
            UnsubscribeHp(entry);
            ReleaseItem(entry.Item);
        }

        private void RegisterInternal(
            Transform target,
            CharacterAttribute characterAttribute,
            string displayName,
            bool isNpc,
            bool isBoss,
            bool showHp,
            Vector3 offset)
        {
            if (target == null)
                return;

            int key = target.GetInstanceID();
            if (!EnsureCanvas())
            {
                QueuePendingRegistration(key, target, characterAttribute, displayName, isNpc, isBoss, showHp, offset);
                return;
            }

            _pendingRegistrations.Remove(key);

            if (_entries.TryGetValue(key, out var existing))
            {
                UnsubscribeHp(existing);
                ReleaseItem(existing.Item);
                _entries.Remove(key);
            }

            UI_WorldHeadItem item = AcquireItem();
            if (item == null)
                return;

            var entry = new TrackEntry
            {
                Target = target,
                Attribute = characterAttribute,
                Item = item,
                Offset = offset,
                IsNpc = isNpc,
                IsBoss = isBoss,
                ShowHp = showHp,
                DistanceVisible = false,
                CurrentHp = characterAttribute != null ? characterAttribute.currentHp : 0f,
                MaxHp = characterAttribute != null ? Mathf.Max(0.01f, characterAttribute.maxHp.Total) : 1f,
                CombatVisibleUntil = isNpc ? float.MaxValue : Time.unscaledTime + hostileDisplayHoldSeconds
            };

            entry.Item.SetDisplayName(displayName);
            entry.Item.SetHpVisible(showHp);
            entry.Item.SetHpRatio(entry.MaxHp > 0f ? entry.CurrentHp / entry.MaxHp : 0f);
            entry.Item.SetAlpha(0f);
            entry.Item.gameObject.SetActive(true);

            if (characterAttribute != null)
            {
                entry.HpChangedHandler = (current, max) =>
                {
                    max = Mathf.Max(0.01f, max);
                    bool tookDamage = current < entry.CurrentHp - 0.001f;
                    entry.CurrentHp = current;
                    entry.MaxHp = max;
                    entry.Item.SetHpRatio(current / max);
                    if (tookDamage)
                        entry.CombatVisibleUntil = Time.unscaledTime + hostileDisplayHoldSeconds;
                };
                characterAttribute.OnHpChanged += entry.HpChangedHandler;
            }

            _entries[key] = entry;
        }

        private void QueuePendingRegistration(
            int key,
            Transform target,
            CharacterAttribute characterAttribute,
            string displayName,
            bool isNpc,
            bool isBoss,
            bool showHp,
            Vector3 offset)
        {
            _pendingRegistrations[key] = new PendingRegister
            {
                Target = target,
                Attribute = characterAttribute,
                DisplayName = displayName,
                IsNpc = isNpc,
                IsBoss = isBoss,
                ShowHp = showHp,
                Offset = offset
            };
        }

        private void FlushPendingRegistrations()
        {
            if (_pendingRegistrations.Count == 0)
                return;

            if (!EnsureCanvas())
                return;

            var pendingList = ListPool<PendingRegister>.Get();
            foreach (var pair in _pendingRegistrations)
            {
                if (pair.Value != null)
                    pendingList.Add(pair.Value);
            }
            _pendingRegistrations.Clear();

            for (int i = 0; i < pendingList.Count; i++)
            {
                var pending = pendingList[i];
                if (pending == null || pending.Target == null)
                    continue;

                RegisterInternal(
                    pending.Target,
                    pending.Attribute,
                    pending.DisplayName,
                    pending.IsNpc,
                    pending.IsBoss,
                    pending.ShowHp,
                    pending.Offset);
            }

            ListPool<PendingRegister>.Release(pendingList);
        }

        private void UpdateEntries()
        {
            if (hideWhenModalUIOpen && UIModalStack.HasAny)
            {
                HideAllEntries();
                return;
            }

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null || itemRoot == null)
                return;

            float now = Time.unscaledTime;
            var keysToRemove = ListPool<int>.Get();

            foreach (var pair in _entries)
            {
                int key = pair.Key;
                var entry = pair.Value;
                if (entry.Target == null || entry.Item == null)
                {
                    keysToRemove.Add(key);
                    continue;
                }

                var targetPos = entry.Target.position;
                float distance = Vector3.Distance(worldCamera.transform.position, targetPos);
                bool wasInDistance = entry.DistanceVisible;
                bool inDistance = EvaluateDistanceVisibility(entry, distance);
                entry.DistanceVisible = inDistance;

                if (!entry.IsNpc && inDistance && !wasInDistance)
                {
                    entry.CombatVisibleUntil = now + hostileDisplayHoldSeconds;
                }

                if (!inDistance)
                {
                    entry.Item.SetAlpha(0f);
                    entry.Item.gameObject.SetActive(false);
                    continue;
                }

                bool contentVisible = entry.IsNpc || now <= entry.CombatVisibleUntil || entry.CurrentHp < entry.MaxHp - 0.001f;
                if (!contentVisible)
                {
                    entry.Item.SetAlpha(0f);
                    entry.Item.gameObject.SetActive(false);
                    continue;
                }

                Vector3 worldPos = targetPos + entry.Offset;
                Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
                if (hideWhenBehindCamera && screenPos.z <= 0f)
                {
                    entry.Item.SetAlpha(0f);
                    entry.Item.gameObject.SetActive(false);
                    continue;
                }

                Camera uiCamera = null;
                if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    uiCamera = rootCanvas.worldCamera;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(itemRoot, screenPos, uiCamera, out var localPos))
                {
                    entry.Item.SetAlpha(0f);
                    entry.Item.gameObject.SetActive(false);
                    continue;
                }

                entry.Item.gameObject.SetActive(true);
                entry.Item.RootRect.anchoredPosition = localPos;
                entry.Item.SetHpVisible(entry.ShowHp);

                float alpha = EvaluateDistanceAlpha(entry, distance);
                entry.Item.SetAlpha(alpha);

                if (keepFixedSize)
                {
                    entry.Item.SetScale(1f);
                }
                else
                {
                    float scale = EvaluateDistanceScale(entry, distance);
                    entry.Item.SetScale(scale);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                int key = keysToRemove[i];
                if (!_entries.TryGetValue(key, out var entry))
                    continue;

                _entries.Remove(key);
                UnsubscribeHp(entry);
                ReleaseItem(entry.Item);
            }

            ListPool<int>.Release(keysToRemove);
        }

        private void HideAllEntries()
        {
            foreach (var entry in _entries.Values)
            {
                if (entry?.Item == null)
                    continue;

                entry.Item.SetAlpha(0f);
                entry.Item.gameObject.SetActive(false);
            }
        }

        private bool EvaluateDistanceVisibility(TrackEntry entry, float distance)
        {
            float showDist;
            float hideDist;
            GetDistanceParams(entry, out showDist, out hideDist, out _);

            if (entry.DistanceVisible)
                return distance <= hideDist;

            return distance <= showDist;
        }

        private float EvaluateDistanceAlpha(TrackEntry entry, float distance)
        {
            GetDistanceParams(entry, out _, out float hideDist, out float fadeStartRatio);
            float fadeStart = Mathf.Max(0f, hideDist * fadeStartRatio);
            if (distance <= fadeStart || hideDist <= fadeStart + 0.001f)
                return 1f;

            return 1f - Mathf.Clamp01((distance - fadeStart) / (hideDist - fadeStart));
        }

        private float EvaluateDistanceScale(TrackEntry entry, float distance)
        {
            GetDistanceParams(entry, out _, out float hideDist, out _);
            if (hideDist <= 0.001f)
                return 1f;

            float t = Mathf.Clamp01(distance / hideDist);
            return Mathf.Lerp(1f, minDistanceScale, t);
        }

        private void GetDistanceParams(TrackEntry entry, out float showDist, out float hideDist, out float fadeStartRatio)
        {
            if (entry.IsNpc)
            {
                showDist = Mathf.Max(0f, npcShowDistance);
                hideDist = Mathf.Max(showDist, npcHideDistance);
                fadeStartRatio = Mathf.Clamp01(npcFadeStartRatio);
                return;
            }

            if (entry.IsBoss)
            {
                showDist = Mathf.Max(0f, bossShowDistance);
                hideDist = Mathf.Max(showDist, bossHideDistance);
                fadeStartRatio = Mathf.Clamp01(hostileFadeStartRatio);
                return;
            }

            showDist = Mathf.Max(0f, enemyShowDistance);
            hideDist = Mathf.Max(showDist, enemyHideDistance);
            fadeStartRatio = Mathf.Clamp01(hostileFadeStartRatio);
        }

        private void UnsubscribeHp(TrackEntry entry)
        {
            if (entry == null || entry.Attribute == null || entry.HpChangedHandler == null)
                return;

            entry.Attribute.OnHpChanged -= entry.HpChangedHandler;
            entry.HpChangedHandler = null;
        }

        private UI_WorldHeadItem AcquireItem()
        {
            if (!EnsureCanvas())
                return null;

            if (itemPrefab == null)
            {
                Debug.LogError("[WorldHeadUIManager] itemPrefab is not assigned. Please assign UI_WorldHeadItem prefab in Inspector.", this);
                return null;
            }

            GameObject pooledObject = PoolSystem.GetGameObject(ItemPoolKey, itemRoot);
            if (pooledObject != null)
            {
                UI_WorldHeadItem pooledItem = pooledObject.GetComponent<UI_WorldHeadItem>();
                if (pooledItem != null)
                    return pooledItem;

                Debug.LogError($"[WorldHeadUIManager] Pooled object missing UI_WorldHeadItem component: {pooledObject.name}", pooledObject);
                Destroy(pooledObject);
            }

            UI_WorldHeadItem item = Instantiate(itemPrefab, itemRoot);
            item.gameObject.SetActive(true);
            return item;
        }

        private void ReleaseItem(UI_WorldHeadItem item)
        {
            if (item == null)
                return;

            item.SetAlpha(0f);
            PoolSystem.PushGameObject(ItemPoolKey, item.gameObject);
        }

        private bool EnsureCanvas()
        {
            if (autoResolveCanvasAtRuntime && (rootCanvas == null || itemRoot == null))
            {
                TryResolveCanvasReferences();
            }

            if (rootCanvas == null)
            {
                if (!_canvasMissingLogged)
                {
                    Debug.LogError("[WorldHeadUIManager] rootCanvas is not assigned. Please assign RootCanvas in Inspector.", this);
                    _canvasMissingLogged = true;
                }
                return false;
            }
            _canvasMissingLogged = false;

            if (itemRoot == null)
            {
                if (!_itemRootMissingLogged)
                {
                    Debug.LogError("[WorldHeadUIManager] itemRoot is not assigned. Please assign WorldHeadRoot in Inspector.", this);
                    _itemRootMissingLogged = true;
                }
                return false;
            }
            _itemRootMissingLogged = false;

            if (applyCanvasSorting)
            {
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = sortingOrder;
            }

            return true;
        }

        private void TryResolveCanvasReferences()
        {
            if (itemRoot != null && rootCanvas == null)
            {
                rootCanvas = itemRoot.GetComponentInParent<Canvas>(true);
            }

            if (rootCanvas == null)
            {
                rootCanvas = FindPreferredCanvas();
            }

            if (rootCanvas == null)
                return;

            if (itemRoot == null)
            {
                Transform existingRoot = string.IsNullOrWhiteSpace(itemRootName)
                    ? null
                    : rootCanvas.transform.Find(itemRootName);
                if (existingRoot is RectTransform existingRect)
                {
                    itemRoot = existingRect;
                    return;
                }

                if (!autoCreateItemRoot)
                    return;

                var rootGo = new GameObject(
                    string.IsNullOrWhiteSpace(itemRootName) ? "WorldHeadRoot" : itemRootName,
                    typeof(RectTransform));
                rootGo.transform.SetParent(rootCanvas.transform, false);

                itemRoot = rootGo.GetComponent<RectTransform>();
                itemRoot.anchorMin = Vector2.zero;
                itemRoot.anchorMax = Vector2.one;
                itemRoot.pivot = new Vector2(0.5f, 0.5f);
                itemRoot.offsetMin = Vector2.zero;
                itemRoot.offsetMax = Vector2.zero;
            }
        }

        private Canvas FindPreferredCanvas()
        {
            if (JKFrameRoot.RootTransform != null)
            {
                if (!string.IsNullOrWhiteSpace(preferredLayoutName))
                {
                    var preferred = JKFrameRoot.RootTransform.Find($"UISystem/{preferredLayoutName}");
                    if (preferred != null)
                    {
                        var preferredCanvas = preferred.GetComponent<Canvas>();
                        if (preferredCanvas != null)
                            return preferredCanvas;
                    }
                }

                var allInRoot = JKFrameRoot.RootTransform.GetComponentsInChildren<Canvas>(true);
                var canvas = PickBestCanvas(allInRoot);
                if (canvas != null)
                    return canvas;
            }

            var all = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return PickBestCanvas(all);
        }

        private Canvas PickBestCanvas(Canvas[] canvases)
        {
            if (canvases == null || canvases.Length == 0)
                return null;

            Canvas best = null;
            int bestOrder = int.MinValue;
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(preferredLayoutName) &&
                    string.Equals(canvas.name, preferredLayoutName, StringComparison.Ordinal))
                {
                    return canvas;
                }

                if (best == null || canvas.sortingOrder > bestOrder)
                {
                    best = canvas;
                    bestOrder = canvas.sortingOrder;
                }
            }

            return best;
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                if (Pool.Count > 0)
                {
                    var list = Pool.Pop();
                    list.Clear();
                    return list;
                }

                return new List<T>(32);
            }

            public static void Release(List<T> list)
            {
                if (list == null)
                    return;

                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
