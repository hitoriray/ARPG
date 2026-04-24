using System;
using Cysharp.Threading.Tasks;
using JKFrame;
using Manager;
using Manager.Server;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    [UIWindowData(typeof(UI_ServerAccountWindow), true, nameof(UI_ServerAccountWindow), 2)]
    public class UI_ServerAccountWindow : UI_WindowBase
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loggedInPanel;

        [Header("Login")]
        [SerializeField] private TMP_InputField loginUserNameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private ButtonManager loginButton;
        [SerializeField] private ButtonManager toRegisterButton;

        [Header("Register")]
        [SerializeField] private TMP_InputField registerUserNameInput;
        [SerializeField] private TMP_InputField registerPhoneInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private ButtonManager registerButton;
        [SerializeField] private ButtonManager toLoginButton;

        [Header("After Login")]
        [SerializeField] private TMP_Text accountInfoText;
        [SerializeField] private ButtonManager logoutButton;
        [SerializeField] private ButtonManager pullCloudSaveButton;
        [SerializeField] private ButtonManager pushCloudSaveButton;

        [Header("Common")]
        [SerializeField] private ButtonManager closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private int defaultInitCharacterId = 1001;
        [SerializeField] private bool defaultToRegisterPanel;

        private bool _busy;
        private bool _isLoggedIn;
        private string _currentUserName = string.Empty;

        public override void Init()
        {
            base.Init();

            BindButton(loginButton, OnLoginButtonClicked);
            BindButton(toRegisterButton, OnToRegisterButtonClicked);
            BindButton(registerButton, OnRegisterButtonClicked);
            BindButton(toLoginButton, OnToLoginButtonClicked);
            BindButton(logoutButton, OnLogoutButtonClicked);
            BindButton(pullCloudSaveButton, OnPullCloudSaveButtonClicked);
            BindButton(pushCloudSaveButton, OnPushCloudSaveButtonClicked);
            BindButton(closeButton, CloseSelf);
        }

        public override void OnShow()
        {
            base.OnShow();
            PlayerService.Instance?.PushUICursor();
            UIModalStack.Push(CloseSelf);

            _isLoggedIn = ApiClient.HasAccessToken;
            _currentUserName = string.Empty;
            ShowAuthPanel(defaultToRegisterPanel);
            RefreshButtonState();
            RefreshLoggedInPanel();
            SetStatus("Please enter account info.");
        }

        public override void OnClose()
        {
            base.OnClose();
            UIModalStack.Remove(CloseSelf);
            PlayerService.Instance?.PopUICursor();
        }

        private void OnToRegisterButtonClicked()
        {
            ShowAuthPanel(true);
        }

        private void OnToLoginButtonClicked()
        {
            ShowAuthPanel(false);
        }

        private void OnLoginButtonClicked()
        {
            LoginAsync().Forget();
        }

        private void OnRegisterButtonClicked()
        {
            RegisterAsync().Forget();
        }

        private void OnLogoutButtonClicked()
        {
            AuthService.Logout();
            _isLoggedIn = false;
            _currentUserName = string.Empty;
            ShowAuthPanel(false);
            RefreshButtonState();
            RefreshLoggedInPanel();
            SetStatus("Logged out.");
        }

        private void OnPullCloudSaveButtonClicked()
        {
            PullCloudSaveAsync().Forget();
        }

        private void OnPushCloudSaveButtonClicked()
        {
            PushCloudSaveAsync().Forget();
        }

        private async UniTask LoginAsync()
        {
            if (!TryGetLoginCredential(out string userName, out string password))
            {
                SetStatus("User name/password is empty.", true);
                return;
            }

            await RunBusyAction(async () =>
            {
                var response = await AuthService.LoginAsync(userName, password);
                _isLoggedIn = true;
                _currentUserName = response.userName;
                ShowAuthPanel(false);
                RefreshButtonState();
                RefreshLoggedInPanel();
                SetStatus($"Login success: {response.userName}");
            });
        }

        private async UniTask RegisterAsync()
        {
            if (!TryGetRegisterForm(
                    out string userName,
                    out string phoneNumber,
                    out string password,
                    out string validateError))
            {
                SetStatus(validateError, true);
                return;
            }

            await RunBusyAction(async () =>
            {
                var response = await AuthService.RegisterAsync(userName, phoneNumber, password);
                if (loginUserNameInput != null) loginUserNameInput.text = response.userName;
                if (loginPasswordInput != null) loginPasswordInput.text = password;

                ShowAuthPanel(false);
                SetStatus($"Register success: {response.userName}. Please login.");
            });
        }

        private async UniTask PushCloudSaveAsync()
        {
            if (!ApiClient.HasAccessToken)
            {
                SetStatus("Please login first.", true);
                return;
            }

            await RunBusyAction(async () =>
            {
                EnsureLocalArchiveReady();
                var response = await CloudSaveService.UploadCurrentArchiveAsync();
                SetStatus($"Cloud save updated: version={response.version}, updatedAt={response.updatedAtUtc}");
            });
        }

        private async UniTask PullCloudSaveAsync()
        {
            if (!ApiClient.HasAccessToken)
            {
                SetStatus("Please login first.", true);
                return;
            }

            await RunBusyAction(async () =>
            {
                var response = await CloudSaveService.DownloadAndImportArchiveAsync();
                SetStatus($"Cloud save applied locally: version={response.version}");
            });
        }

        private async UniTask RunBusyAction(Func<UniTask> action)
        {
            if (_busy || action == null) return;

            try
            {
                _busy = true;
                RefreshButtonState();
                await action();
            }
            catch (ApiException ex)
            {
                SetStatus(FormatApiError(ex), true);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                _busy = false;
                RefreshButtonState();
            }
        }

        private void EnsureLocalArchiveReady()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Please enter Play Mode before cloud save operations.");
            }

            if (DataManager.GameData != null) return;

            SaveSystem.Init();
            if (!DataManager.HasArchive || !DataManager.LoadCurrentArchive())
            {
                DataManager.CreateArchive(defaultInitCharacterId);
            }
        }

        private bool TryGetLoginCredential(out string userName, out string password)
        {
            userName = loginUserNameInput != null ? loginUserNameInput.text : string.Empty;
            password = loginPasswordInput != null ? loginPasswordInput.text : string.Empty;

            userName = userName?.Trim();
            password = password?.Trim();

            return !string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password);
        }

        private bool TryGetRegisterForm(
            out string userName,
            out string phoneNumber,
            out string password,
            out string validateError)
        {
            userName = registerUserNameInput != null ? registerUserNameInput.text : string.Empty;
            phoneNumber = registerPhoneInput != null ? registerPhoneInput.text : string.Empty;
            password = registerPasswordInput != null ? registerPasswordInput.text : string.Empty;
            string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : string.Empty;

            userName = userName?.Trim().ToLowerInvariant();
            phoneNumber = NormalizePhone(phoneNumber);
            password = password?.Trim();
            confirmPassword = confirmPassword?.Trim();

            if (string.IsNullOrWhiteSpace(userName))
            {
                validateError = "User name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                validateError = "Phone number is required.";
                return false;
            }

            if (!IsPhoneValid(phoneNumber))
            {
                validateError = "Phone number format is invalid.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                validateError = "Password is required.";
                return false;
            }

            if (password.Length < 8 || password.Length > 128)
            {
                validateError = "Password length must be 8-128.";
                return false;
            }

            if (registerConfirmPasswordInput != null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                validateError = "Password and confirm password do not match.";
                return false;
            }

            validateError = string.Empty;
            return true;
        }

        private static string NormalizePhone(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        private static bool IsPhoneValid(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            if (phoneNumber.StartsWith("+", StringComparison.Ordinal))
            {
                if (phoneNumber.Length < 7 || phoneNumber.Length > 21) return false;

                for (int i = 1; i < phoneNumber.Length; i++)
                {
                    if (!char.IsDigit(phoneNumber[i])) return false;
                }

                return true;
            }

            if (phoneNumber.Length < 6 || phoneNumber.Length > 20) return false;

            for (int i = 0; i < phoneNumber.Length; i++)
            {
                if (!char.IsDigit(phoneNumber[i])) return false;
            }

            return true;
        }

        private static string FormatApiError(ApiException ex)
        {
            if (ex == null) return "Unknown API error.";

            string code = string.Empty;
            string message = ex.Message;

            if (!string.IsNullOrWhiteSpace(ex.ResponseText))
            {
                var error = JsonUtility.FromJson<ApiErrorEnvelope>(ex.ResponseText);
                if (error != null)
                {
                    if (!string.IsNullOrWhiteSpace(error.code)) code = error.code;
                    if (!string.IsNullOrWhiteSpace(error.message)) message = error.message;
                }
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return $"API {ex.StatusCode}: {message}";
            }

            return $"API {ex.StatusCode} [{code}]: {message}";
        }

        private void RefreshButtonState()
        {
            bool canClick = !_busy;
            bool hasToken = ApiClient.HasAccessToken;
            bool loggedIn = _isLoggedIn && hasToken;

            SetInteractable(toRegisterButton, canClick && !loggedIn);
            SetInteractable(registerButton, canClick && !loggedIn);
            SetInteractable(toLoginButton, canClick && !loggedIn);
            SetInteractable(loginButton, canClick && !loggedIn);
            SetInteractable(logoutButton, canClick && loggedIn);
            SetInteractable(pullCloudSaveButton, canClick && loggedIn);
            SetInteractable(pushCloudSaveButton, canClick && loggedIn);
            SetInteractable(closeButton, true);
        }

        private void RefreshLoggedInPanel()
        {
            bool loggedIn = _isLoggedIn && ApiClient.HasAccessToken;
            if (loggedInPanel != null) loggedInPanel.SetActive(loggedIn);

            if (accountInfoText != null)
            {
                accountInfoText.text = loggedIn
                    ? $"Current account: {_currentUserName}"
                    : "Not logged in";
            }
        }

        private void ShowAuthPanel(bool showRegister)
        {
            bool loggedIn = _isLoggedIn && ApiClient.HasAccessToken;
            if (loginPanel != null) loginPanel.SetActive(!loggedIn && !showRegister);
            if (registerPanel != null) registerPanel.SetActive(!loggedIn && showRegister);
        }

        private static void BindButton(ButtonManager button, UnityAction callback)
        {
            if (button == null || callback == null) return;
            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private static void SetInteractable(ButtonManager button, bool interactable)
        {
            if (button != null) button.Interactable(interactable);
        }

        private void CloseSelf()
        {
            UISystem.Close<UI_ServerAccountWindow>();
        }

        private void SetStatus(string message, bool isError = false)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (isError)
            {
                Debug.LogError($"[UI_ServerAccountWindow] {message}");
            }
            else
            {
                Debug.Log($"[UI_ServerAccountWindow] {message}");
            }
        }

        [Serializable]
        private sealed class ApiErrorEnvelope
        {
            public string code;
            public string message;
        }
    }
}
