using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    public class OfferController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
