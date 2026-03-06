using System;
using JKFrame;
using Michsky.MUIP;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_ConfirmWindow), true, nameof(UI_ConfirmWindow), 2)]
    public class UI_ConfirmWindow : UI_WindowBase
    {
        [SerializeField] private ModalWindowManager modalWindowManager;
        private string title;
        private string message;
        
        public override void Init()
        {
            base.Init();
            if (modalWindowManager == null)
                modalWindowManager = GetComponent<ModalWindowManager>();
        }

        public void Show(string title, string message, Action confirmAction, Action cancelAction, Sprite icon = null)
        {
            if (icon != null) modalWindowManager.icon = icon;
            modalWindowManager.titleText = title;
            modalWindowManager.descriptionText = message;
            
            modalWindowManager.onConfirm.RemoveAllListeners();
            modalWindowManager.onCancel.RemoveAllListeners();

            if (confirmAction != null) modalWindowManager.onConfirm.AddListener(() => confirmAction?.Invoke());
            if (cancelAction != null) modalWindowManager.onCancel.AddListener(() => cancelAction?.Invoke());
            
            modalWindowManager.onConfirm.AddListener(() => UISystem.Close<UI_ConfirmWindow>());
            modalWindowManager.onCancel.AddListener(() => UISystem.Close<UI_ConfirmWindow>());
            
            modalWindowManager.UpdateUI();
            modalWindowManager.OpenWindow();
        }
    }
}