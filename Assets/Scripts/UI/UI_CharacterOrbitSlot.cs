using System;
using Config;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_CharacterOrbitSlot : MonoBehaviour
    {
        [SerializeField] private ButtonManager button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedFx;
        [SerializeField] private GameObject lockedMask;
        [SerializeField] private Sprite defaultIcon;

        private int characterId = -1;
        private Action<int> onClick;

        public void Bind(CharacterEntry entry, bool selected, bool unlocked, Action<int> clickHandler)
        {
            characterId = entry.CharacterId;
            onClick = clickHandler;

            if (nameText != null)
            {
                nameText.SetText(entry.CharacterName);
            }

            if (icon != null)
            {
                icon.sprite = defaultIcon;
                icon.enabled = icon.sprite != null;
            }

            SetSelected(selected);
            SetLocked(!unlocked);

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
                // button.interactable = unlocked;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedFx != null)
            {
                selectedFx.SetActive(selected);
            }
        }

        public void SetLocked(bool locked)
        {
            if (lockedMask != null)
            {
                lockedMask.SetActive(locked);
            }
        }

        private void HandleClick()
        {
            onClick?.Invoke(characterId);
        }
    }
}
