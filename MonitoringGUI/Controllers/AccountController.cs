using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json;

public class AccountController : Controller
{
    private readonly HttpClient _httpClient;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string Username, string Password)
    {
        var loginRequest = new
        {
            username = Username,
            passwordHash = Password
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://informatik2.ei.hv.se/LoginService/api/auth/login", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Fel användarnamn eller lösenord.";
            return View();
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        var sessionData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);

        if (sessionData == null || !sessionData.ContainsKey("sessionId") ||
            !sessionData.ContainsKey("userId") || !sessionData.ContainsKey("roleId"))
        {
            System.Diagnostics.Debug.WriteLine("Fel: sessionData saknar nödvändiga fält!");
            ViewBag.ErrorMessage = "Fel vid hantering av session.";
            return View();
        }

        string sessionId = sessionData["sessionId"].ToString();
        int userId = Convert.ToInt32(sessionData["userId"]);
        int roleId = Convert.ToInt32(sessionData["roleId"]);

        HttpContext.Session.SetString("SessionID", sessionId);
        HttpContext.Session.SetInt32("UserID", userId);
        HttpContext.Session.SetInt32("UserRole", roleId);
        HttpContext.Session.SetString("Username", Username);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var response = await _httpClient.PostAsync("https://informatik2.ei.hv.se/api/auth/logout", null);
        }
        catch
        {
            // Fortsätt ändå med att rensa sessionen om anropet misslyckas
        }

        HttpContext.Session.Clear();
        Response.Cookies.Delete("SessionID");

        return RedirectToAction("Index", "Home");
    }

    // ========================
    // REGISTER ACTION (NY)
    // ========================
    [HttpGet]
    public IActionResult Register()
    {
        return View(); // Måste finnas en Register.cshtml i Views/Account
    }
}
