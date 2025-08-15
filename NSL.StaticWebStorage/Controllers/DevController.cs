#if DEBUG
using Microsoft.AspNetCore.Mvc;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Services;

namespace NSL.StaticWebStorage.Controllers
{
    public class DevController(StoragesService storagesService) : ControllerBase
    {
        [HttpPost("/__sws_api/dev/clear")]
        public IActionResult Clear()
        {
            foreach (var item in Directory.GetDirectories("data"))
            {
                Directory.Delete(item, true);
            }

            return Ok();
        }
    }
}

#endif