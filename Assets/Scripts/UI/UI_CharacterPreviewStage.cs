using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JKFrame;
using Manager;
using UnityEngine;

namespace UI
{
    public class UI_CharacterPreviewStage : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Vector3 modelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 modelLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private bool resetRotationOnSelect = true;
        [SerializeField, Range(1, 12)] private int cacheCapacity = 5;
        [SerializeField] private float rotateSpeed = 0.2f;

        private readonly Dictionary<int, GameObject> cachedModels = new();
        private readonly LinkedList<int> lru = new();
        private int currentCharacterId = -1;
        private int requestId;

        private void Awake()
        {
            cacheCapacity = Mathf.Max(1, cacheCapacity);
        }

        public async UniTask ShowCharacterAsync(int characterId)
        {
            if (modelRoot == null)
            {
                JKLog.Error($"[{nameof(UI_CharacterPreviewStage)}] 未配置modelRoot");
                return;
            }

            if (characterId == currentCharacterId && currentCharacterId != -1)
            {
                return;
            }

            requestId += 1;
            int localRequestId = requestId;

            var model = await GetOrCreateModel(characterId);
            if (localRequestId != requestId || model == null)
            {
                return;
            }

            if (currentCharacterId != -1 && cachedModels.TryGetValue(currentCharacterId, out var oldModel))
            {
                oldModel.SetActive(false);
            }

            currentCharacterId = characterId;
            ApplyTransform(model);
            model.SetActive(true);

            if (resetRotationOnSelect)
            {
                modelRoot.localRotation = Quaternion.identity;
            }

            EvictIfNeeded(currentCharacterId);
        }

        public void RotateBy(float deltaX)
        {
            if (modelRoot == null)
            {
                return;
            }

            modelRoot.Rotate(0f, -deltaX * rotateSpeed, 0f, Space.Self);
        }

        public void ReleaseAll(bool destroyCached)
        {
            requestId += 1;

            if (currentCharacterId != -1 && cachedModels.TryGetValue(currentCharacterId, out var current))
            {
                current.SetActive(false);
            }

            currentCharacterId = -1;

            if (!destroyCached)
            {
                return;
            }

            foreach (var pair in cachedModels)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            cachedModels.Clear();
            lru.Clear();
        }

        private async UniTask<GameObject> GetOrCreateModel(int characterId)
        {
            if (cachedModels.TryGetValue(characterId, out var cached))
            {
                Touch(characterId);
                return cached;
            }

            var prefab = await CharacterModelManager.Instance.LoadCharacterModelPrefabAsync(characterId);
            if (prefab == null)
            {
                return null;
            }

            var instance = Instantiate(prefab, modelRoot);
            instance.name = prefab.name;
            instance.SetActive(false);
            cachedModels[characterId] = instance;
            lru.AddLast(characterId);
            EvictIfNeeded(characterId);
            return instance;
        }

        private void Touch(int characterId)
        {
            var node = lru.Find(characterId);
            if (node == null)
            {
                lru.AddLast(characterId);
                return;
            }

            lru.Remove(node);
            lru.AddLast(node);
        }

        private void EvictIfNeeded(int protectedId)
        {
            if (cachedModels.Count <= cacheCapacity)
            {
                return;
            }

            int guard = lru.Count;
            while (cachedModels.Count > cacheCapacity && lru.First != null && guard-- > 0)
            {
                int oldestId = lru.First.Value;
                lru.RemoveFirst();

                if (oldestId == currentCharacterId || oldestId == protectedId)
                {
                    lru.AddLast(oldestId);
                    continue;
                }

                if (cachedModels.TryGetValue(oldestId, out var model))
                {
                    cachedModels.Remove(oldestId);
                    if (model != null)
                    {
                        Destroy(model);
                    }
                }
            }
        }

        private void ApplyTransform(GameObject model)
        {
            var t = model.transform;
            t.SetParent(modelRoot, false);
            t.localPosition = modelLocalPosition;
            t.localEulerAngles = modelLocalEuler;
            t.localScale = modelLocalScale;
        }

        private void OnDestroy()
        {
            ReleaseAll(true);
        }
    }
}
