using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MonitoringGUI.Models;
using Newtonsoft.Json;

namespace MonitoringGUI.Controllers
{
    //Anger att alla routes börjar med /Monitoring
    [Route("Monitoring")]
    public class MonitoringController : Controller
    {
        private readonly HttpClient _httpClient;

        //Konstruktor som tar in HttpClient för att göra API-anrop
        public MonitoringController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        
        //Monitoring/Index
        [HttpGet("Index")]
        public IActionResult Index()
        {
            //Hämtar sessionID från webbläsarens cookies
            var sessionId = Request.Cookies["SessionID"];

            //Om ingen session finns, omdirigera till login
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            //Annars visa Index-vyn
            return View();
        }

       
        //Monitoring/Health
        public async Task<IActionResult> Health()
        {
            //Hämtar sessionID från sessionen
            var sessionId = HttpContext.Session.GetString("SessionID");

            //Kontrollerar om sessionen är aktiv
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            //Anropar MonitoringService API för att hämta hälsostatus
            var response = await _httpClient.GetAsync("https://informatik2.ei.hv.se/MonitoringService/api/monitoring/health");
            var content = await response.Content.ReadAsStringAsync();

            //Tolkar JSON-svaret som en Dictionary med nyckel/värde-par
            var apiStatuses = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

            //Skickar datan till vyn via ViewBag
            ViewBag.ApiStatuses = apiStatuses;

            return View();
        }

       
        //Monitoring/Stats
        [HttpGet("Stats")]
        public async Task<IActionResult> Stats()
        {
            //Hämtar sessionID från sessionen
            var sessionId = HttpContext.Session.GetString("SessionID");

            //Om session saknas, gå till login
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            //Anropar API för att hämta statistik
            var response = await _httpClient.GetAsync("https://informatik2.ei.hv.se/MonitoringService/api/monitoring/stats");
            var content = await response.Content.ReadAsStringAsync();

            //Skriver ut svaret i konsolen (för felsökning)
            Console.WriteLine($"API Response från MonitoringService: {content}");

            // Tolkar svaret som en Dictionary där varje nyckel har en lista med dynamiskt innehåll
            var apiStats = JsonConvert.DeserializeObject<Dictionary<string, List<dynamic>>>(content);

            //Om API-svaret är tomt eller null
            if (apiStats == null || apiStats.Count == 0)
            {
                Console.WriteLine("Ingen statistik hämtades från API:et!");
                apiStats = new Dictionary<string, List<dynamic>>();
            }

            //Skickar statistiken till vyn via ViewBag
            ViewBag.ApiStats = apiStats;

            return View();
        }
    }
}
