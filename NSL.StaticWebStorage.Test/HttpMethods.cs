using NSL.StaticWebStorage.Models;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NSL.StaticWebStorage.Test
{
    internal class HttpMethods
    {
        #region Utils

        static HttpClient CreateClient(string? token
            , string? tokenCode)
        {
            var client = new HttpClient() { BaseAddress = new Uri("http://localhost:5000") };

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

        public static async Task<HttpResponseMessage> DevClearAsync()
        {
            using var client = CreateClient(default, default);

            var request = CreateJsonRequest("/dev/clear", default);

            return await client.SendAsync(request);
        }

        #endregion

        #region Storage

        public static async Task<HttpResponseMessage> CreateStorageAsync(string? token
            , string? tokenCode
            , string id
            , bool shared)
        {
            using var client = CreateClient(token, tokenCode);

            var request = CreateJsonRequest("/storage/create", new { id, shared });

            return await client.SendAsync(request);
        }

        #endregion

        #region Access

        public static async Task<HttpResponseMessage> ShareAccessAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , CreateStorageTokenRequestModel requestData)
        {
            using var client = CreateClient(token, tokenCode);

            var url = "/access/share";

            if (storageName != default)
                url = $"/{storageName}{url}";

            if (path != default)
                url = $"{url}/{path}";

            var request = CreateJsonRequest(url, requestData);

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> RecallAccessAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , string recallToken)
        {
            using var client = CreateClient(token, tokenCode);

            var url = "/access/recall";

            if (storageName != default)
                url = $"/{storageName}{url}";

            if (path != default)
                url = $"{url}/{path}";

            var request = CreateJsonRequest(url, recallToken);

            return await client.SendAsync(request);
        }

        #endregion

        #region Files

        public static async Task<HttpResponseMessage> UploadAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path
            , string? uploadType
            , Stream requestData)
        {
            using var client = CreateClient(token, tokenCode);

            var url = "/upload";

            //if (storageName != default)
            url = $"/{storageName}{url}";
            //if (path != default)
            url = $"{url}/{path}";


            var request = new HttpRequestMessage(HttpMethod.Post, url);

            var content = new MultipartFormDataContent();   

            content.Add(new StreamContent(requestData), "file", "file");


            request.Content = content;

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> DeleteAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path)
        {
            using var client = CreateClient(token, tokenCode);

            var url = "/delete";

            //if (storageName != default)
            url = $"/{storageName}{url}";

            //if (path != default)
            url = $"{url}/{path}";

            var request = CreateJsonRequest(url, default);

            return await client.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> DownloadAsync(string? token
            , string? tokenCode
            , string? storageName
            , string? path)
        {
            using var client = CreateClient(token, tokenCode);

            var url = "/download";

            //if (storageName != default)
            url = $"/{storageName}{url}";

            //if (path != default)
            url = $"{url}/{path}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            return await client.SendAsync(request);
        }


        #endregion
    }
}
