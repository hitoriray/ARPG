using Cysharp.Threading.Tasks;
using JKFrame;
using UnityEngine;

namespace Manager.Server
{
    public sealed class ServerSmokeTest : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://localhost:5254";
        [SerializeField] private string userName = "unity_test";
        [SerializeField] private string password = "Password123";
        [SerializeField] private int initialCharacterId = 1001;

        [ContextMenu("Server Smoke Test/Register Login Upload Download")]
        private void RunSmokeTestFromContextMenu()
        {
            RunSmokeTestAsync().Forget();
        }

        public async UniTask RunSmokeTestAsync()
        {
            ApiClient.BaseUrl = serverUrl;

            try
            {
                if (!Application.isPlaying)
                {
                    Debug.LogError("[ServerSmokeTest] Please enter Play Mode before running this smoke test. JKFrame SaveSystem is not initialized in Edit Mode.");
                    return;
                }

                await TryRegisterAsync();

                var login = await AuthService.LoginAsync(userName, password);
                Debug.Log($"[ServerSmokeTest] Login ok: userName={login.userName}, userId={login.userId}");

                EnsureLocalArchive();

                var uploaded = await CloudSaveService.UploadCurrentArchiveAsync();
                Debug.Log($"[ServerSmokeTest] Upload ok: version={uploaded.version}, updatedAtUtc={uploaded.updatedAtUtc}");

                var downloaded = await CloudSaveService.DownloadArchiveAsync();
                Debug.Log($"[ServerSmokeTest] Download ok: version={downloaded.version}, jsonLength={downloaded.saveJson?.Length ?? 0}");
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[ServerSmokeTest] API failed: status={ex.StatusCode}, message={ex.Message}, response={ex.ResponseText}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServerSmokeTest] Failed: {ex}");
            }
        }

        private async UniTask TryRegisterAsync()
        {
            try
            {
                var registered = await AuthService.RegisterAsync(userName, password);
                Debug.Log($"[ServerSmokeTest] Register ok: userName={registered.userName}, userId={registered.userId}");
            }
            catch (ApiException ex) when (ex.StatusCode == 409)
            {
                Debug.Log("[ServerSmokeTest] User already exists, continue with login.");
            }
        }

        private void EnsureLocalArchive()
        {
            if (DataManager.GameData != null) return;

            SaveSystem.Init();

            if (!DataManager.HasArchive || !DataManager.LoadCurrentArchive())
            {
                DataManager.CreateArchive(initialCharacterId);
            }
        }
    }
}
