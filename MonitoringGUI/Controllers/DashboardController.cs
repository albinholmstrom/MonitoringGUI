using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    // En controller som hanterar vyer relaterade till dashboarden
    public class DashboardController : Controller
    {
        
        public IActionResult Index()
        {
           
            return View();
        }
    }
}
