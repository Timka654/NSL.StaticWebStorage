using Microsoft.AspNetCore.Mvc;
using NSL.StaticWebStorage.Utils.Route;

namespace NSL.StaticWebStorage.Controllers
{
    [TokenAccessFilter()]
    public class FilesController : Controller
    {
        [TokenAccessFilter(canDownload: true)]
        [HttpGet("/{storage}/download/{path}")]
        public IActionResult Download([FromQuery] string storage, [FromQuery] string path)
        {
            var epath = Path.Combine("data", "storage", storage, path);

            if (!System.IO.File.Exists(epath))
                return NotFound();

            return File(System.IO.File.OpenRead(epath), "application/octet-stream", Path.GetFileName(epath));
        }

        [TokenAccessFilter(canUpload: true)]
        [HttpPost("/{storage}/upload/{path}")]
        public IActionResult Upload([FromQuery] string storage, [FromQuery] string path, IFormFile file)
        {
            var epath = Path.Combine("data", "storage", storage, path);

            if (!Directory.Exists(Path.GetDirectoryName(epath)))
                Directory.CreateDirectory(Path.GetDirectoryName(epath)!);

            using (var stream = System.IO.File.Create(epath))
            {
                file.CopyTo(stream);
            }

            return Ok();
        }

        [TokenAccessFilter(canShareAccess: true)]
        [HttpPost("/{storage}/share/{path}")]
        public IActionResult ShareAccess([FromQuery] string storage, [FromQuery] string path, IFormFile file)
        {
            return View();
        }

        [TokenAccessFilter(canShareAccess: true)]
        [HttpPost("/{storage}/share")]
        public IActionResult ShareAccess([FromQuery] string storage)
        {
            return View();
        }

        [TokenAccessFilter(canShareAccess: true)]
        [HttpPost("/{storage}/recall/{path}")]
        public IActionResult Recall([FromQuery] string storage, [FromQuery] string path, IFormFile file)
        {
            return View();
        }

        [TokenAccessFilter(canShareAccess: true)]
        [HttpPost("/{storage}/recall")]
        public IActionResult Recall([FromQuery] string storage, IFormFile file)
        {
            return View();
        }
    }
}
