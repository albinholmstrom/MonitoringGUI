using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    public class EventController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
