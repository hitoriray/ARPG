using Cysharp.Threading.Tasks;
using JKFrame;
using Manager;
using Michsky.MUIP;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_MenuSceneMenuWindow), false, nameof(UI_MenuSceneMenuWindow), 2)]
    public class UI_MenuSceneMenuWindow : UI_WindowBase
    {
        [SerializeField] private ButtonManager startButton;
        [SerializeField] private ButtonManager continueButton;
        [SerializeField] private ButtonManager serverButton;
        [SerializeField] private ButtonManager quitButton;

        public override void Init()
        {
            base.Init();

            startButton.onClick.AddListener(OnStartButtonClicked);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            if (serverButton != null) serverButton.onClick.AddListener(OnServerButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);

            if (!DataManager.HasArchive)
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void OnStartButtonClicked()
        {
            EnterCreateCharacterScene().Forget();
        }

        private void OnContinueButtonClicked()
        {
            ContinueGame().Forget();
        }

        private async UniTaskVoid EnterCreateCharacterScene()
        {
            SetButtonsInteractable(false);
            await UniTask.Yield();
            UISystem.Close<UI_MenuSceneMenuWindow>();
            GameManager.Instance.EnterCharacterSelectionWithLoading();
        }

        private async UniTaskVoid ContinueGame()
        {
            SetButtonsInteractable(false);
            await UniTask.Yield();
            UISystem.Close<UI_MenuSceneMenuWindow>();
            GameManager.Instance.UseCurrentArchiveAndEnterGame();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.Interactable(interactable);
            if (continueButton != null) continueButton.Interactable(interactable);
            if (serverButton != null) serverButton.Interactable(interactable);
            if (quitButton != null) quitButton.Interactable(interactable);
        }

        private void OnServerButtonClicked()
        {
            UISystem.Show<UI_ServerAccountWindow>();
        }

        private void OnQuitButtonClicked()
        {
            Application.Quit();
        }
    }
}
