using Microsoft.AspNetCore.Mvc;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;
using NSL.StaticWebStorage.Shared.Models;
using NSL.StaticWebStorage.Utils.Route;

namespace NSL.StaticWebStorage.Controllers
{
    [TokenAccessFilter(shareAccessCheck: true)]
    public class AccessController(MasterTokensService tokensService) : ControllerBase
    {
        [HttpPost("/__sws_api/{storage}/access/share/{*path}")]
        public IActionResult Share([FromRoute] string storage, [FromRoute] string path, [FromBody] CreateStorageTokenRequestModel query)
        {
            storage = storage.Trim().ToLower();

            query.Storages = [storage];

            query.Paths = [path];

            var tokenData = StorageTokenBuilder.Create()
                .AddPath(query.Paths)
                .AddStorage(query.Storages)
                .AllowDownload(query.CanDownload)
                .AllowShareAccess(query.CanShareAccess)
                .AllowUpload(query.CanUpload)
                .SetCode(query.Code)
                .SetExpiredTime(query.Expired)
                .SetStorageOwner(storage)
                .SetPathOwner(path)
                .Build();

            string? token = query.Token?.ToLower();

            if (token != default)
            {
                if (!tokensService.CreateToken(token, tokenData))
                {
                    return Conflict();
                }
            }
            else
            {
                do
                {
                    token = string.Join("", Enumerable.Range(0, 2).Select(x => Guid.NewGuid())).ToLower();
                } while (!tokensService.CreateToken(token, tokenData));
            }

            return Ok(new
            {
                token,
                tokenData.Code
            });
        }

        [HttpPost("/__sws_api/{storage}/access/share")]
        public IActionResult Share([FromRoute] string storage, [FromBody] CreateStorageTokenRequestModel query)
        {
            storage = storage.Trim().ToLower();

            query.Storages = [storage];

            var tokenData = StorageTokenBuilder.Create()
                .AddPath(query.Paths)
                .AddStorage(query.Storages)
                .AllowDownload(query.CanDownload)
                .AllowShareAccess(query.CanShareAccess)
                .AllowUpload(query.CanUpload)
                .SetCode(query.Code)
                .SetExpiredTime(query.Expired)
                .SetStorageOwner(storage)
                .Build();

            string? token = query.Token?.ToLower();

            if (token != default)
            {
                if (!tokensService.CreateToken(token, tokenData))
                {
                    return Conflict();
                }
            }
            else
            {
                do
                {
                    token = string.Join("", Enumerable.Range(0, 2).Select(x => Guid.NewGuid())).ToLower();
                } while (!tokensService.CreateToken(token, tokenData));
            }

            return Ok(new
            {
                token,
                tokenData.Code
            });
        }

        [HttpPost("/__sws_api/access/share")]
        public IActionResult Share([FromBody] CreateStorageTokenRequestModel query)
        {
            var tokenData = StorageTokenBuilder.Create()
                .AddPath(query.Paths)
                .AddStorage(query.Storages)
                .AllowDownload(query.CanDownload)
                .AllowShareAccess(query.CanShareAccess)
                .AllowUpload(query.CanUpload)
                .SetCode(query.Code)
                .SetExpiredTime(query.Expired)
                .Build();

            string? token = query.Token?.ToLower();

            if (token != default)
            {
                if (!tokensService.CreateToken(token, tokenData))
                {
                    return Conflict();
                }
            }
            else
            {
                do
                {
                    token = string.Join("", Enumerable.Range(0, 2).Select(x => Guid.NewGuid())).ToLower();
                } while (!tokensService.CreateToken(token, tokenData));
            }

            return Ok(new
            {
                token,
                tokenData.Code
            });
        }

        [HttpPost("/__sws_api/{storage}/access/recall/{*path}")]
        public IActionResult Recall([FromRoute] string storage, [FromRoute] string path, [FromBody] string token)
        {
            storage = storage.Trim().ToLower();

            var tokenData = tokensService.TryGetToken(token);

            if (tokenData == null)
                return NotFound();

            if (tokenData.StorageOwner != storage 
                || tokenData.PathOwner != path)
                return Forbid();

            tokensService.TryRemoveToken(token);

            return Ok();
        }

        [HttpPost("/__sws_api/{storage}/access/recall")]
        public IActionResult Recall([FromRoute] string storage, [FromBody] string token)
        {
            storage = storage.Trim().ToLower();

            var tokenData = tokensService.TryGetToken(token);

            if (tokenData == null)
                return NotFound();

            if(tokenData.StorageOwner != storage)
                return Forbid();

            tokensService.TryRemoveToken(token);

            return Ok();
        }

        [HttpPost("/__sws_api/access/recall")]
        public IActionResult Recall([FromBody] string token)
        {
            var tokenData = tokensService.TryGetToken(token);

            if (tokenData == null)
                return NotFound();

            tokensService.TryRemoveToken(token);

            return Ok();
        }
    }
}
