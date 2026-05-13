using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using tunav_backend.Models;

namespace tunav_backend.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Ok("Tunav Backend API est opérationnel. Accédez à /swagger pour tester les endpoints.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}