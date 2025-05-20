using Microsoft.AspNetCore.Mvc;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;
using NSL.StaticWebStorage.Utils.Route;

namespace NSL.StaticWebStorage.Controllers
{
    [TokenAccessFilter(shareAccessCheck: true)]
    public class StorageController(StoragesService storagesService) : ControllerBase
    {
        [HttpPost("/storage/create")]
        public async Task<IActionResult> Create([FromBody] CreateStorageRequestModel storage)
        {
            if(storage.Id == default)
                storage.Id = Guid.NewGuid().ToString();

            storage.Id = storage.Id.ToLower();

            if (await storagesService.CreateStorageAsync(storage.Id, storage.Shared))
                return Ok(storage.Id);

            return Conflict("Storage already exists");
        }
    }
}
