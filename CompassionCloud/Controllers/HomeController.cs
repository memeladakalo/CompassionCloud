using Microsoft.AspNetCore.Mvc;

namespace CompassionCloud.Controllers

// CI pipeline trigger test — Section C.2
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}