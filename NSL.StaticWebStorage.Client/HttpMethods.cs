using NSL.StaticWebStorage.Shared;
using NSL.StaticWebStorage.Shared.Models;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NSL.StaticWebStorage.Client
{
    public class HttpMethods
    {
        #region Utils

        static HttpClient CreateClient(string? token
            , string? tokenCode
            , string baseUrl)
        {
            var client = new HttpClient() { BaseAddress = new Uri(baseUrl) };

            if (token != default)
                client.DefaultRequestHeaders.Add("token", token);
            if (tokenCode != default)
                client.DefaultRequestHeaders.Add("token_code", tokenCode);

            return client;
        }

        static HttpRequestMessage CreateJsonRequest(string url, object? value)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            if (value != null)
                request.Content = new StringContent(JsonSerializer.Serialize(value, JsonSerializerOptions.Web)
                    , Encoding.UTF8
                    , "application/json");

            return request;
        }

#if DEBUG

        public static async Task<HttpResponseMessage> DevClearAsync(string baseUrl)
        {
            using var client = CreateClient(default, default, baseUrl);

            var request = CreateJsonRequest("/__sws_api/dev/clear", default);

            return await client.SendAsync(request);
        }

#endif

        #endregion

        #region Storage

        public static async Task<HttpResponseMessage> CreateStorageAsync(string? token
            , string? tokenCode
            , string id
            , bool shared
            , string baseUrl)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var request = CreateJsonRequest("/__sws_api/storage/create", new { id, shared });

            return await client.SendAsync(request);
        }

        #endregion

        #region Access

        public static async Task<HttpResponseMessage> ShareAccessAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , CreateStorageTokenRequestModel requestData
            , string baseUrl)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var url = "/access/share";

            if (storageName != default)
                url = $"/{storageName}{url}";

            if (path != default)
                url = $"{url}/{path}";

            url = $"/__sws_api{url}";

            var request = CreateJsonRequest(url, requestData);

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> RecallAccessAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , string recallToken
            , string baseUrl)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var url = "/__sws_api/access/recall";

            if (storageName != default)
                url = $"/{storageName}{url}";

            if (path != default)
                url = $"{url}/{path}";

            var request = CreateJsonRequest(url, recallToken);

            return await client.SendAsync(request);
        }

        #endregion

        #region Files

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="tokenCode"></param>
        /// <param name="storageName"></param>
        /// <param name="path"></param>
        /// <param name="requestData"></param>
        /// <param name="uploadType"><see cref="StorageUploadType"/></param>
        /// <param name="overwrite"><see cref="StorageOverwriteType"/></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> UploadAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , Stream requestData
            , string baseUrl
            , string? uploadType = StorageUploadType.None
            , string? overwrite = StorageOverwriteType.None)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var url = "/upload";

            //if (storageName != default)
            url = $"/{storageName}{url}";
            //if (path != default)
            url = $"{url}/{path}";

            url = $"/__sws_api{url}";


            var request = new HttpRequestMessage(HttpMethod.Post, url);

            if (uploadType != StorageUploadType.None)
                request.Headers.Add("upload-type", uploadType);

            if (overwrite != StorageOverwriteType.None)
                request.Headers.Add("overwrite", overwrite);

            var content = new MultipartFormDataContent();

            content.Add(new StreamContent(requestData), "file", "file");


            request.Content = content;

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> DeleteAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , string baseUrl)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var url = "/delete";

            //if (storageName != default)
            url = $"/{storageName}{url}";

            //if (path != default)
            url = $"{url}/{path}";

            url = $"/__sws_api{url}";

            var request = CreateJsonRequest(url, default);

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> DownloadAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , string baseUrl)
        {
            using var client = CreateClient(token, tokenCode, baseUrl);

            var url = "/download";

            //if (storageName != default)
            url = $"/{storageName}{url}";

            //if (path != default)
            url = $"{url}/{path}";

            url = $"/__sws_api{url}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            return await client.SendAsync(request);
        }


        #endregion
    }
}
