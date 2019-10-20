using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using FaceBlurApplication.Models;
using Microsoft.AspNetCore.Http;

namespace FaceBlurApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _he;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment he)
        {
            _logger = logger;
            _he = he;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult UploadImage (IFormFile img)
        {
            if (img != null)
            {
                var fileName = Path.Combine(_he.WebRootPath, Path.GetFileName(img.FileName));

                img.CopyTo(new FileStream(fileName, FileMode.Create));

                ViewData["fileLocation"] = "/" + Path.GetFileName(img.FileName);
            }

            return View();
        }
    }
}
