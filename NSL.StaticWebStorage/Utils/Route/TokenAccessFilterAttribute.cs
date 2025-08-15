using Microsoft.AspNetCore.Mvc.Filters;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;
using System.Net;

namespace NSL.StaticWebStorage.Utils.Route
{
    public class TokenAccessFilterAttribute(bool downloadCheck = false, bool uploadCheck = false, bool shareAccessCheck = false) : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var storageService = context.HttpContext.RequestServices.GetRequiredService<StoragesService>();

            bool isSharedStorage = false;

            if (context.RouteData.Values.TryGetValue("storage", out var storage))
            {
                var s = storageService.TryGetStorage((string)storage);

                if (s == null)
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;

                    return;
                }

                isSharedStorage = s.Shared;
            }

            if (context.HttpContext.Request.Headers.TryGetValue("token", out var _htoken))
                _htoken = _htoken.First().ToLower();
            else if (context.RouteData.Values.TryGetValue("token", out var _rtoken))
                _htoken = (string)_rtoken;

            context.HttpContext.Request.Headers.TryGetValue("token_code", out var _code);

            var tokenService = context.HttpContext.RequestServices.GetRequiredService<MasterTokensService>();

            var token = tokenService.TryGetToken(_htoken);

            context.RouteData.Values.TryGetValue("path", out var path);

            if (!isSharedStorage)
            {
                if (downloadCheck && token?.CheckDownloadAccess((string)path!, (string)storage!, _code) != true)
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                    return;
                }
            }

            if (uploadCheck && token?.CheckUploadAccess((string)path!, (string)storage!, _code) != true)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                return;
            }

            if (shareAccessCheck && token?.CheckShareAccess((string)path!, (string)storage!, _code) != true)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                return;
            }

            await next();
        }
    }
}
