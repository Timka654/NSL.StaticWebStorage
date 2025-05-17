using Microsoft.AspNetCore.Mvc.Filters;
using NSL.StaticWebStorage.Services;
using System.Net;

namespace NSL.StaticWebStorage.Utils.Route
{
    public class TokenAccessFilterAttribute(bool canDownload = false, bool canUpload = false, bool canShareAccess = false) : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("token", out var _token)
                || !context.HttpContext.Request.Headers.TryGetValue("code", out var _code))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<MasterTokensService>();

            var token = tokenService.TryGetToken(_token);

            if (token == null)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            if (!token.CheckAccess(_code))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            if (!context.RouteData.Values.TryGetValue("storage", out var storage))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            context.RouteData.Values.TryGetValue("path", out var path);

            if (canDownload && !token.CheckDownloadAccess((string)path!, (string)storage!))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            if (canUpload && !token.CheckUploadAccess((string)path!, (string)storage!))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            if (canShareAccess && !token.CheckShareAccess((string)path!, (string)storage!))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            await next();
        }
    }
}
