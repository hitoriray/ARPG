using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityWebRequestException = Cysharp.Threading.Tasks.UnityWebRequestException;

namespace Manager.Server
{
    public static class ApiClient
    {
        private const string DefaultBaseUrl = "http://localhost:5254";
        private const string BaseUrlKey = "ARPG.Server.BaseUrl";
        private const string AccessTokenKey = "ARPG.Server.AccessToken";

        public static string BaseUrl
        {
            get => PlayerPrefs.GetString(BaseUrlKey, DefaultBaseUrl);
            set
            {
                PlayerPrefs.SetString(BaseUrlKey, NormalizeBaseUrl(value));
                PlayerPrefs.Save();
            }
        }

        public static string AccessToken
        {
            get => PlayerPrefs.GetString(AccessTokenKey, string.Empty);
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    PlayerPrefs.DeleteKey(AccessTokenKey);
                else
                    PlayerPrefs.SetString(AccessTokenKey, value);

                PlayerPrefs.Save();
            }
        }

        public static bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

        public static void SetAccessToken(string accessToken)
        {
            AccessToken = accessToken;
        }

        public static void ClearAccessToken()
        {
            AccessToken = string.Empty;
        }

        public static UniTask<string> GetAsync(string path, bool requireAuth = false)
        {
            return SendAsync("GET", path, null, requireAuth);
        }

        public static UniTask<string> PostJsonAsync(string path, string json, bool requireAuth = false)
        {
            return SendAsync("POST", path, json, requireAuth);
        }

        public static UniTask<string> PutJsonAsync(string path, string json, bool requireAuth = false)
        {
            return SendAsync("PUT", path, json, requireAuth);
        }

        private static async UniTask<string> SendAsync(string method, string path, string bodyJson, bool requireAuth)
        {
            using var request = new UnityWebRequest(BuildUrl(path), method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (bodyJson != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Accept", "application/json");

            if (requireAuth)
            {
                if (!HasAccessToken)
                    throw new ApiException(401, string.Empty, "Access token is missing.");

                request.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            }

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                // UniTask throws for HTTP errors before UnityWebRequest.result can be inspected.
                // Keep response handling centralized below so callers can catch ApiException.
            }

            string responseText = request.downloadHandler?.text ?? string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new ApiException(
                    request.responseCode,
                    responseText,
                    $"HTTP {request.responseCode}: {request.error}");
            }

            return responseText;
        }

        private static string BuildUrl(string path)
        {
            string cleanPath = path.StartsWith("/") ? path : "/" + path;
            return NormalizeBaseUrl(BaseUrl) + cleanPath;
        }

        private static string NormalizeBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DefaultBaseUrl;
            return value.Trim().TrimEnd('/');
        }
    }
}
