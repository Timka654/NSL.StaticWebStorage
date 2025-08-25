using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;
using System.Net;

namespace NSL.StaticWebStorage.Utils.Route
{
    public class DomainRoutingMiddleware(RequestDelegate next
        , ILogger<DomainRoutingMiddleware> logger
        , IOptions<StaticStorageConfigurationModel> staticStorageOptions
        , StoragesService storagesService)
    {
        StaticStorageConfigurationModel staticStorageConfiguration => staticStorageOptions.Value;

        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;

            if (!context.Request.Path.StartsWithSegments("/__sws_api"))
            {
                var storage = storagesService.TryGetStorage(host);

                //logger.LogInformation("Routing request for host: {Host}, storage: {Storage}, path: {path}", host, storage?.Id, context.Request.Path.Value);

                if (storage?.Shared != true)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }


                var oldPath = context.Request.Path.Value;


                var path = $"data/storages/{host}{oldPath}";

                if (Directory.Exists(path))
                    path = $"{path}/index.html";

                context.Request.Path = $"/{path}";

                //logger.LogInformation("Routing request for host: {Host}, storage: {Storage}, path: {Path} ({NewPath})", host, storage?.Id, oldPath, context.Request.Path.Value);
            }

            await next(context);
        }
    }
}
