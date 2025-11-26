using Microsoft.AspNetCore.Mvc;

namespace UniPool01.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult HowItWorks()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
