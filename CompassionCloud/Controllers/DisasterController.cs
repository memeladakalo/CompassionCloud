using Microsoft.AspNetCore.Mvc;

namespace CompassionCloud.Controllers
{
    public class DisasterController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
