using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using Data;
using JKFrame;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_CharacterSelectionWindow), false, nameof(UI_CharacterSelectionWindow), 2)]
    public class UI_CharacterSelectionWindow : UI_WindowBase
    {
        [Header("Data")]
        [SerializeField] private CharacterTable characterTable;

        [Header("Orbit")]
        [SerializeField] private RectTransform orbitContainer;
        [SerializeField] private UI_CharacterOrbitSlot orbitSlotPrefab;
        [SerializeField, Range(5, 11)] private int visibleSlotCount = 7;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Preview")]
        [SerializeField] private UI_CharacterPreviewStage previewStage;

        [Header("Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text memoryText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        private readonly List<CharacterEntry> playableCharacters = new();
        private readonly Dictionary<int, int> idToIndex = new();
        private readonly List<int> preloadIds = new(3);
        private UI_CharacterOrbitSlot[] orbitSlots;
        private int selectedIndex = -1;

        public override void Init()
        {
            base.Init();

            if (characterTable == null)
            {
                JKLog.Error($"[{nameof(UI_CharacterSelectionWindow)}] 未配置CharacterTable");
                return;
            }

            BuildPlayableList();
            CreateOrbitSlots();
            BindEvents();
            if (playableCharacters.Count == 0)
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 没有可用角色");
                return;
            }

            SelectIndex(FindDefaultIndex(), true);
        }

        public override void OnClose()
        {
            base.OnClose();

            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(SelectPrev);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(SelectNext);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmButtonClick);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackButtonClick);
            }

            if (previewStage != null)
            {
                previewStage.ReleaseAll(true);
            }
        }

        private void BuildPlayableList()
        {
            playableCharacters.Clear();
            idToIndex.Clear();

            foreach (var entry in characterTable.Characters)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!entry.IsPlayable || entry.CharacterType != CharacterType.Player)
                {
                    continue;
                }

                idToIndex[entry.CharacterId] = playableCharacters.Count;
                playableCharacters.Add(entry);
            }
        }

        private void CreateOrbitSlots()
        {
            if (orbitSlotPrefab == null || orbitContainer == null)
            {
                JKLog.Error($"[{nameof(UI_CharacterSelectionWindow)}] 未配置Orbit槽位或容器");
                return;
            }

            int count = Mathf.Clamp(visibleSlotCount, 5, 11);
            if (count % 2 == 0)
            {
                count += 1;
            }

            orbitSlots = new UI_CharacterOrbitSlot[count];
            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(orbitSlotPrefab, orbitContainer);
                orbitSlots[i] = slot;
            }
        }

        private void BindEvents()
        {
            if (prevButton != null)
            {
                prevButton.onClick.AddListener(SelectPrev);
            }
            else
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 未配置prevButton");
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(SelectNext);
            }
            else
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 未配置nextButton");
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClick);
            }
            else
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 未配置confirmButton");
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClick);
            }
            else
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 未配置backButton");
            }
        }

        private int FindDefaultIndex()
        {
            if (DataManager.GameData != null)
            {
                if (idToIndex.TryGetValue(DataManager.GameData.SelectedCharacterId, out int index))
                {
                    return index;
                }
            }

            return playableCharacters.Count > 0 ? 0 : -1;
        }

        private void SelectPrev()
        {
            if (playableCharacters.Count == 0) return;
            int index = selectedIndex - 1;
            if (index < 0) index = playableCharacters.Count - 1;
            SelectIndex(index, false);
        }

        private void SelectNext()
        {
            if (playableCharacters.Count == 0) return;
            int index = selectedIndex + 1;
            if (index >= playableCharacters.Count) index = 0;
            SelectIndex(index, false);
        }

        private void SelectIndex(int index, bool force)
        {
            if (index < 0 || index >= playableCharacters.Count)
            {
                return;
            }

            if (!force && selectedIndex == index)
            {
                return;
            }

            selectedIndex = index;
            RefreshOrbitSlots();
            UpdateInfoPanel();
            PreloadNeighbors();
            RequestPreview();
        }

        private void RefreshOrbitSlots()
        {
            if (orbitSlots == null || orbitSlots.Length == 0 || playableCharacters.Count == 0)
            {
                return;
            }

            int half = orbitSlots.Length / 2;
            int start = selectedIndex - half;
            int total = playableCharacters.Count;

            for (int i = 0; i < orbitSlots.Length; i++)
            {
                int listIndex = start + i;
                while (listIndex < 0) listIndex += total;
                while (listIndex >= total) listIndex -= total;

                var entry = playableCharacters[listIndex];
                bool selected = listIndex == selectedIndex;
                bool unlocked = IsUnlocked(entry.CharacterId);
                orbitSlots[i].Bind(entry, selected, unlocked, OnSlotClicked);
            }
        }

        private void OnSlotClicked(int characterId)
        {
            if (!idToIndex.TryGetValue(characterId, out int index))
            {
                return;
            }

            if (!IsUnlocked(characterId))
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 角色未解锁: {characterId}");
                return;
            }

            SelectIndex(index, false);
        }

        private bool IsUnlocked(int characterId)
        {
            if (DataManager.GameData == null)
            {
                return true;
            }

            return DataManager.GameData.UnlockedCharacterIds.List.Contains(characterId);
        }

        private void UpdateInfoPanel()
        {
            if (selectedIndex < 0 || selectedIndex >= playableCharacters.Count)
            {
                return;
            }

            var entry = playableCharacters[selectedIndex];
            if (nameText != null)
            {
                nameText.SetText(entry.CharacterName);
            }

            if (memoryText != null)
            {
                memoryText.SetText("{0} MB", entry.MemoryCost);
            }
        }

        private void PreloadNeighbors()
        {
            if (playableCharacters.Count <= 1)
            {
                return;
            }

            preloadIds.Clear();

            int prevIndex = selectedIndex - 1;
            if (prevIndex < 0) prevIndex = playableCharacters.Count - 1;
            int nextIndex = selectedIndex + 1;
            if (nextIndex >= playableCharacters.Count) nextIndex = 0;

            preloadIds.Add(playableCharacters[selectedIndex].CharacterId);
            preloadIds.Add(playableCharacters[prevIndex].CharacterId);
            preloadIds.Add(playableCharacters[nextIndex].CharacterId);

            CharacterModelManager.Instance.PreloadCharacters(preloadIds);
        }

        private void RequestPreview()
        {
            if (previewStage == null)
            {
                return;
            }

            int characterId = playableCharacters[selectedIndex].CharacterId;
            previewStage.ShowCharacterAsync(characterId).Forget();
        }

        private void OnConfirmButtonClick()
        {
            if (selectedIndex < 0 || selectedIndex >= playableCharacters.Count)
            {
                JKLog.Warning($"[{nameof(UI_CharacterSelectionWindow)}] 未选择角色");
                return;
            }

            int characterId = playableCharacters[selectedIndex].CharacterId;
            DataManager.CreateArchive(characterId);
            SceneSystem.LoadScene("GameScene");
        }

        private void OnBackButtonClick()
        {
            UISystem.Close<UI_CharacterSelectionWindow>();
            SceneSystem.LoadScene("Menu");
        }
    }
}
