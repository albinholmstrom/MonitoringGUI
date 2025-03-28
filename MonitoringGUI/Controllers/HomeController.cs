using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    // Controller som hanterar startsidan (Home)
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            
            return View();
        }
    }
}
