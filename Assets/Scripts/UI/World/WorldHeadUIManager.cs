using System;
using System.Collections.Generic;
using Attribute;
using JKFrame;
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
        private float _updateTimer;

        public static WorldHeadUIManager Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<WorldHeadUIManager>();
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

            _instance = FindAnyObjectByType<WorldHeadUIManager>();
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

            if (!EnsureCanvas())
                return;
            int key = target.GetInstanceID();

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

        private void UpdateEntries()
        {
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
                bool inDistance = EvaluateDistanceVisibility(entry, distance);
                entry.DistanceVisible = inDistance;

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
            if (rootCanvas == null)
            {
                Debug.LogError("[WorldHeadUIManager] rootCanvas is not assigned. Please assign RootCanvas in Inspector.", this);
                return false;
            }

            if (itemRoot == null)
            {
                Debug.LogError("[WorldHeadUIManager] itemRoot is not assigned. Please assign WorldHeadRoot in Inspector.", this);
                return false;
            }

            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = sortingOrder;

            return true;
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
