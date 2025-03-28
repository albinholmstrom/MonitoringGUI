using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    // Controller som hanterar sidor relaterade till "Event"
    public class EventController : Controller
    {
        
        public IActionResult Index()
        {
            
            return View();
        }
    }
}
