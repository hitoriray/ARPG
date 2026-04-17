using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Manager.Server
{
    public static class AuthService
    {
        public static async UniTask<RegisterResponse> RegisterAsync(string userName, string password)
        {
            var request = new AuthRequest
            {
                userName = userName,
                password = password
            };

            string responseJson = await ApiClient.PostJsonAsync("/api/auth/register", JsonUtility.ToJson(request));
            return JsonUtility.FromJson<RegisterResponse>(responseJson);
        }

        public static async UniTask<LoginResponse> LoginAsync(string userName, string password)
        {
            var request = new AuthRequest
            {
                userName = userName,
                password = password
            };

            string responseJson = await ApiClient.PostJsonAsync("/api/auth/login", JsonUtility.ToJson(request));
            var response = JsonUtility.FromJson<LoginResponse>(responseJson);
            ApiClient.SetAccessToken(response.accessToken);
            return response;
        }

        public static async UniTask<MeResponse> GetMeAsync()
        {
            string responseJson = await ApiClient.GetAsync("/api/auth/me", requireAuth: true);
            return JsonUtility.FromJson<MeResponse>(responseJson);
        }

        public static void Logout()
        {
            ApiClient.ClearAccessToken();
        }

        [Serializable]
        private sealed class AuthRequest
        {
            public string userName;
            public string password;
        }

        [Serializable]
        public sealed class RegisterResponse
        {
            public string userId;
            public string userName;
            public string createdAtUtc;
        }

        [Serializable]
        public sealed class LoginResponse
        {
            public string userId;
            public string userName;
            public string accessToken;
            public string expiresAtUtc;
        }

        [Serializable]
        public sealed class MeResponse
        {
            public string userId;
            public string userName;
        }
    }
}
