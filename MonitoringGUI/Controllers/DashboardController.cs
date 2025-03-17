using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
