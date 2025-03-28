using Microsoft.AspNetCore.Mvc;

namespace MonitoringGUI.Controllers
{
    // Controller som hanterar sidor relaterade till "Offer" (erbjudanden eller liknande)
    public class OfferController : Controller
    {
        
        public IActionResult Index()
        {
          
            return View();
        }
    }
}
