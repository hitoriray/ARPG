using System;
using JKFrame;
using Manager;
using Michsky.MUIP;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_ConfirmWindow), true, nameof(UI_ConfirmWindow), 2)]
    public class UI_ConfirmWindow : UI_WindowBase
    {
        [SerializeField] private ModalWindowManager modalWindowManager;

        public override void Init()
        {
            base.Init();
            if (modalWindowManager == null)
                modalWindowManager = GetComponent<ModalWindowManager>();
        }

        public override void OnShow()
        {
            base.OnShow();
            PlayerService.Instance?.PushUICursor();
            UIModalStack.Push(CloseSelf);
        }

        public override void OnClose()
        {
            base.OnClose();
            UIModalStack.Remove(CloseSelf);
            PlayerService.Instance?.PopUICursor();
        }

        public void Show(string title, string message, Action confirmAction, Action cancelAction, Sprite icon = null)
        {
            if (modalWindowManager == null) return;

            if (icon != null) modalWindowManager.icon = icon;
            modalWindowManager.titleText = title;
            modalWindowManager.descriptionText = message;

            modalWindowManager.onConfirm.RemoveAllListeners();
            modalWindowManager.onCancel.RemoveAllListeners();

            if (confirmAction != null) modalWindowManager.onConfirm.AddListener(() => confirmAction());
            if (cancelAction != null) modalWindowManager.onCancel.AddListener(() => cancelAction());

            modalWindowManager.onConfirm.AddListener(() => UISystem.Close<UI_ConfirmWindow>());
            modalWindowManager.onCancel.AddListener(() => UISystem.Close<UI_ConfirmWindow>());

            modalWindowManager.UpdateUI();
            modalWindowManager.OpenWindow();
        }

        private void CloseSelf()
        {
            UISystem.Close<UI_ConfirmWindow>();
        }
    }
}
