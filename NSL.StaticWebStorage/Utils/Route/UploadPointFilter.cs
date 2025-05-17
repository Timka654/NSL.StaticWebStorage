namespace NSL.StaticWebStorage.Utils.Route
{
    public class UploadPointFilter : IEndpointFilter
    {
        public UploadPointFilter(string? token)
        {
            Token = token;
        }

        public string? Token { get; }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (Token == null || context.HttpContext.Request.Headers.TryGetValue("upload-token", out var auth) && Token == auth)
                return await next(context);

            return Results.NotFound();
        }
    }
}
