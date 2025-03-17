using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MonitoringGUI.Models;
using Newtonsoft.Json;

namespace MonitoringGUI.Controllers
{
    [Route("Monitoring")]
    public class MonitoringController : Controller
    {
        private readonly HttpClient _httpClient;

        public MonitoringController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            var sessionId = Request.Cookies["SessionID"];//hämtar sessionsID från cookies

            if (string.IsNullOrEmpty(sessionId))//om ingen session finns
            {
                return RedirectToAction("Login", "Account");//dirigeras till login-sida
            }

            return View();
        }


        public async Task<IActionResult> Health()
        {
            var sessionId = HttpContext.Session.GetString("SessionID");//hämtar session 

            if (string.IsNullOrEmpty(sessionId))//om ingen session finns
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _httpClient.GetAsync("https://informatik2.ei.hv.se/MonitoringService/api/monitoring/health");
            var content = await response.Content.ReadAsStringAsync();//anropar apiet för att hämta status på olika tjänster


            var apiStatuses = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);//gör om svaret till en Dictionary
            ViewBag.ApiStatuses = apiStatuses;//lagrar statusarna i viewbag

            return View();
        }



        [HttpGet("Stats")]
        public async Task<IActionResult> Stats()
        {
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _httpClient.GetAsync("https://informatik2.ei.hv.se/MonitoringService/api/monitoring/stats");
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"API Response från MonitoringService: {content}");

            var apiStats = JsonConvert.DeserializeObject<Dictionary<string, List<dynamic>>>(content);

            if (apiStats == null || apiStats.Count == 0)
            {
                Console.WriteLine("Ingen statistik hämtades från API:et!");
                apiStats = new Dictionary<string, List<dynamic>>();
            }

            ViewBag.ApiStats = apiStats;

            return View();
        }




    }

}
