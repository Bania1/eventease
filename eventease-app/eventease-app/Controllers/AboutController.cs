using Microsoft.AspNetCore.Mvc;

namespace eventease_app.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "About Page";
            return View();
        }
    }
}
