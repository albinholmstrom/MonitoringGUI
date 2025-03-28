using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using MonitoringGUI.Models; // För att använda User-modellen
using Microsoft.AspNetCore.Http;

namespace MonitoringGUI.Controllers
{
    // Anger route-prefix för alla metoder i denna controller: /Employee
    [Route("Employee")]
    public class EmployeeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Konstruktor som tar in HttpClient och HttpContextAccessor
        public EmployeeController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://informatik2.ei.hv.se/LoginService/api/auth/");
            _httpContextAccessor = httpContextAccessor;
        }

        // Startvy som visar alla anställda
        public async Task<IActionResult> Index()
        {
            // Hämtar sessionID från inloggningen
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                ViewBag.ErrorMessage = "Ingen aktiv session. Logga in igen.";
                return RedirectToAction("Login", "Account");
            }

            // Lägger till sessionen i HTTP-anropets headers
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"SessionID={sessionId}");

            // Kontrollerar att sessionen fortfarande är giltig
            var response = await _httpClient.GetAsync("protected");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Sessionen är ogiltig eller har gått ut.";
                return RedirectToAction("Login", "Account");
            }

            // Läser och tolkar sessionsdata från API-svaret
            var responseBody = await response.Content.ReadAsStringAsync();
            var sessionData = JsonDocument.Parse(responseBody).RootElement;

            if (!sessionData.TryGetProperty("userId", out JsonElement userIdElement) ||
                !sessionData.TryGetProperty("roleId", out JsonElement roleIdElement))
            {
                ViewBag.ErrorMessage = "Fel vid hämtning av session.";
                return RedirectToAction("Login", "Account");
            }

            // Sparar eller uppdaterar sessionens userId och roleId
            int userId = userIdElement.GetInt32();
            int roleId = roleIdElement.GetInt32();

            if (HttpContext.Session.GetInt32("UserID") != userId || HttpContext.Session.GetInt32("UserRole") != roleId)
            {
                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetInt32("UserRole", roleId);
            }

            // Hämtar alla anställda från API
            var employeesResponse = await _httpClient.GetAsync("employees");
            if (!employeesResponse.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Kunde inte hämta anställda.";
                return View(new List<User>());
            }

            // Tolkar JSON-data till en lista med User-objekt
            var employeesJson = await employeesResponse.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<User>>(employeesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Skickar med till vyn om användaren är admin (rollId == 1)
            ViewBag.IsAdmin = roleId == 1;

            return View(employees);
        }

        
        // Lägga till ny anställd
        [HttpPost("add")]
        public async Task<IActionResult> AddEmployee(User newEmployee)
        {
            newEmployee.UserID = 0; // Säkerställer att ID är 0 vid nyregistrering

            // Om rollen är admin, kontrollera att användaren också är admin
            if (newEmployee.RoleID == 2)
            {
                var adminRole = HttpContext.Session.GetInt32("UserRole");

                if (adminRole != 1)
                {
                    TempData["ErrorMessage"] = "Du måste vara inloggad som administratör.";
                    return RedirectToAction("Index");
                }
            }

            // Skapar JSON-data från användarens information
            var json = JsonSerializer.Serialize(new
            {
                Username = newEmployee.Username,
                PasswordHash = newEmployee.PasswordHash,
                RoleID = newEmployee.RoleID,
                EmailAddress = newEmployee.EmailAddress
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Skickar ett anrop till API:et för att skapa användaren
            var response = await _httpClient.PostAsync("register", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = $"Fel vid skapande av användare: {response.StatusCode} - {responseBody}";
                return RedirectToAction("Index");
            }

            // Om det är en "vanlig användare", skicka till inloggningen direkt
            if (newEmployee.RoleID == 3)
            {
                TempData["SuccessMessage"] = "Kontot har registrerats! Du kan nu logga in.";
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index");
        }

       
        // Ta bort anställd
        [HttpPost("delete/{userId}")]
        public async Task<IActionResult> DeleteEmployee(int userId)
        {
            var response = await _httpClient.DeleteAsync($"delete/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Kunde inte ta bort anställd.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        
        // Uppdatera anställd (via formulär)
        [HttpPut("updateEmployee/{userId}")]
        public async Task<IActionResult> UpdateEmployee(int userId, [FromForm] User updatedEmployee, [FromForm] string _method)
        {
            // Säkerställer att rätt metod används
            if (_method != "PUT")
            {
                return BadRequest("Fel metod!");
            }

            var adminRole = HttpContext.Session.GetInt32("UserRole");
            if (adminRole != 1)
            {
                TempData["ErrorMessage"] = "Du måste vara inloggad som administratör.";
                return RedirectToAction("Index");
            }

            var json = JsonSerializer.Serialize(new
            {
                Username = updatedEmployee.Username,
                EmailAddress = updatedEmployee.EmailAddress,
                PasswordHash = string.IsNullOrEmpty(updatedEmployee.PasswordHash) ? null : updatedEmployee.PasswordHash,
                RoleID = updatedEmployee.RoleID
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"updateEmployee/{userId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = $"Fel vid uppdatering av anställd: {response.StatusCode} - {responseBody}";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        
        // Proxy: Uppdatera anställd via API direkt
        [HttpPut("proxy/updateEmployee/{userId}")]
        public async Task<IActionResult> ProxyUpdateEmployee(int userId, [FromBody] User updatedEmployee)
        {
            var json = JsonSerializer.Serialize(updatedEmployee);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Tar bort tidigare headers (om några)
            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            _httpClient.DefaultRequestHeaders.Remove("UserRole");

            var sessionId = HttpContext.Session.GetString("SessionID");
            var roleId = HttpContext.Session.GetInt32("UserRole");

            if (string.IsNullOrEmpty(sessionId) || roleId == null)
            {
                return Unauthorized("Ingen aktiv session eller behörighet saknas.");
            }

            // Lägger till aktuella session- och rollvärden i header
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"SessionID={sessionId}");
            _httpClient.DefaultRequestHeaders.Add("UserRole", roleId.ToString());

            // Anropar externa API:t direkt
            var apiUrl = $"https://informatik2.ei.hv.se/LoginService/api/auth/updateEmployee/{userId}";
            var response = await _httpClient.PutAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Fel vid uppdatering av anställd: {response.StatusCode} - {responseBody}");
            }

            return Ok();
        }

        
        // Hämta en anställd för redigering
        [HttpGet("Edit/{userId}")]
        public async Task<IActionResult> Edit(int userId)
        {
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Hämtar hela listan med anställda
            var response = await _httpClient.GetAsync($"employees");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Kunde inte hämta anställda.";
                return RedirectToAction("Index");
            }

            // Tolkar svar och letar upp en anställd baserat på userId
            var employeesJson = await response.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<User>>(employeesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var employee = employees.FirstOrDefault(e => e.UserID == userId);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Anställd hittades inte.";
                return RedirectToAction("Index");
            }

            return View(employee); // Skickar med anställd till Edit-vyn
        }
    }
}
