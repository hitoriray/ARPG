using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Manager.Server
{
    public static class CloudSaveService
    {
        public static async UniTask<CloudSaveResponse> UploadCurrentArchiveAsync()
        {
            string saveJson = DataManager.ExportCurrentArchiveJson();
            var request = new UpsertCloudSaveRequest
            {
                saveJson = saveJson
            };

            string responseJson = await ApiClient.PutJsonAsync(
                "/api/save/",
                JsonUtility.ToJson(request),
                requireAuth: true);

            return JsonUtility.FromJson<CloudSaveResponse>(responseJson);
        }

        public static async UniTask<CloudSaveResponse> DownloadArchiveAsync()
        {
            string responseJson = await ApiClient.GetAsync("/api/save/", requireAuth: true);
            return JsonUtility.FromJson<CloudSaveResponse>(responseJson);
        }

        public static async UniTask<CloudSaveResponse> DownloadAndImportArchiveAsync()
        {
            var response = await DownloadArchiveAsync();
            DataManager.ImportCloudArchiveJson(response.saveJson);
            return response;
        }

        [Serializable]
        private sealed class UpsertCloudSaveRequest
        {
            public string saveJson;
        }

        [Serializable]
        public sealed class CloudSaveResponse
        {
            public string saveJson;
            public int version;
            public string updatedAtUtc;
        }
    }
}
