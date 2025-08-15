using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;
using System.Net;

namespace NSL.StaticWebStorage.Utils.Route
{
    public class DomainRoutingMiddleware(RequestDelegate next, IOptions<StaticStorageConfigurationModel> ssconfigurationOptions, StoragesService storagesService)
    {
        private StaticStorageConfigurationModel configuration => ssconfigurationOptions.Value;

        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;

            if (!context.Request.Path.StartsWithSegments("/__sws_api"))
            {
                var storage = storagesService.TryGetStorage(host);

                if (storage?.Shared != true)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                if (!Path.HasExtension(context.Request.Path.Value))
                {
                    context.Request.Path = $"/storage/{host}/index.html";
                }
                else
                    context.Request.Path = $"/storage/{host}{context.Request.Path}";
            }

            await next(context);
        }
    }
}
