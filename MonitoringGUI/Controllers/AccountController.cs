using Microsoft.AspNetCore.Mvc; 
using System.Net.Http; 
using System.Text; 
using System.Threading.Tasks; 
using Newtonsoft.Json; 
using System.Text.Json; 

public class AccountController : Controller
{
    // HttpClient används för att skicka HTTP-anrop
    private readonly HttpClient _httpClient;

    // Konstruktor som tar emot en HttpClientFactory och skapar en HttpClient
    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    // Visar inloggningssidan (GET-förfrågan)
    [HttpGet]
    public IActionResult Login()
    {
        return View(); // Returnerar Login-vyn
    }

    // Hanterar inloggning (POST-förfrågan)
    [HttpPost]
    public async Task<IActionResult> Login(string Username, string Password)
    {
        // Skapar ett objekt som ska skickas till inloggnings-API:et
        var loginRequest = new
        {
            username = Username,
            passwordHash = Password // Här används "passwordHash", men lösenordet skickas som ren text
        };

        // Gör om objektet till JSON och skapar ett HTTP-innehåll
        var jsonContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

        // Skickar POST-anrop till inloggnings-API:et
        var response = await _httpClient.PostAsync("https://informatik2.ei.hv.se/LoginService/api/auth/login", jsonContent);

        // Om svaret inte är lyckat, visa felmeddelande
        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Fel användarnamn eller lösenord.";
            return View();
        }

        // Läser in svaret från servern som text
        var responseBody = await response.Content.ReadAsStringAsync();

        // Försöker tolka JSON-svaret till en Dictionary
        var sessionData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);

        // Om någon av de förväntade nycklarna saknas, visa fel
        if (sessionData == null || !sessionData.ContainsKey("sessionId") ||
            !sessionData.ContainsKey("userId") || !sessionData.ContainsKey("roleId"))
        {
            System.Diagnostics.Debug.WriteLine("Fel: sessionData saknar nödvändiga fält!");
            ViewBag.ErrorMessage = "Fel vid hantering av session.";
            return View();
        }

        // Hämtar sessionens information från svaret
        string sessionId = sessionData["sessionId"].ToString();
        int userId = Convert.ToInt32(sessionData["userId"]);
        int roleId = Convert.ToInt32(sessionData["roleId"]);

        // Sparar informationen i sessionen
        HttpContext.Session.SetString("SessionID", sessionId);
        HttpContext.Session.SetInt32("UserID", userId);
        HttpContext.Session.SetInt32("UserRole", roleId);
        HttpContext.Session.SetString("Username", Username);

        // Skickar användaren till hemsidan efter lyckad inloggning
        return RedirectToAction("Index", "Home");
    }

    // Hanterar utloggning (POST)
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Försöker logga ut från API:et
            var response = await _httpClient.PostAsync("https://informatik2.ei.hv.se/api/auth/logout", null);
        }
        catch
        {
            // Om något går fel, fortsätt ändå med att rensa sessionen
        }

        // Tömmer sessionen och tar bort sessionskakan
        HttpContext.Session.Clear();
        Response.Cookies.Delete("SessionID");

        // Skickar användaren tillbaka till hemsidan
        return RedirectToAction("Index", "Home");
    }

    // Visar registreringssidan (GET-förfrågan)
    [HttpGet]
    public IActionResult Register()
    {
        // Returnerar Register-vyn (måste finnas en vyfil: Views/Account/Register.cshtml)
        return View();
    }
}
