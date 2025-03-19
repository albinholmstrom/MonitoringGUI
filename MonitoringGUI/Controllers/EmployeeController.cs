using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using MonitoringGUI.Models;
using Microsoft.AspNetCore.Http;

namespace MonitoringGUI.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://informatik2.ei.hv.se/LoginService/api/auth/");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            //hämtar sessionens ID och kontrollerar så att den är giltig
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                ViewBag.ErrorMessage = "Ingen aktiv session. Logga in igen.";
                return RedirectToAction("Login", "Account");
            }

            //lägger till sessionsID i http-headern
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"SessionID={sessionId}");

            //anropar /protected för att verifiera sessionen
            var response = await _httpClient.GetAsync("protected");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Sessionen är ogiltig eller har gått ut.";
                return RedirectToAction("Login", "Account");
            }

            //nedan läser av API-svaret och kontrollerar att userID och roleID finns med.
            var responseBody = await response.Content.ReadAsStringAsync();
            var sessionData = JsonDocument.Parse(responseBody).RootElement;

            if (!sessionData.TryGetProperty("userId", out JsonElement userIdElement) ||
                !sessionData.TryGetProperty("roleId", out JsonElement roleIdElement))
            {
                ViewBag.ErrorMessage = "Fel vid hämtning av session.";
                return RedirectToAction("Login", "Account");
            }

            //nedan uppdaterar sessionen om nödvändigt.
            int userId = userIdElement.GetInt32();
            int roleId = roleIdElement.GetInt32();

            if (HttpContext.Session.GetInt32("UserID") != userId || HttpContext.Session.GetInt32("UserRole") != roleId)
            {
                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetInt32("UserRole", roleId);
            }

            //hämta lista över anställda
            var employeesResponse = await _httpClient.GetAsync("employees");
            if (!employeesResponse.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = "Kunde inte hämta anställda.";
                return View(new List<User>());
            }

            //deserialiserar JSON-svaret till en lista av User-objekt.
            var employeesJson = await employeesResponse.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<User>>(employeesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.IsAdmin = roleId == 1; // Används i View för att visa/dölja admin-funktioner

            //returnerar listan
            return View(employees);
        }



        [HttpPost]
        public async Task<IActionResult> AddEmployee(User newEmployee)
        {
            System.Diagnostics.Debug.WriteLine("GUI: AddEmployee-metoden anropad.");

            newEmployee.RoleID = 2;//sätter RoleId till 2
            newEmployee.UserID = 0; //nollställ UserID

            var adminRole = HttpContext.Session.GetInt32("UserRole");

            if (adminRole != 1)
            {
                TempData["ErrorMessage"] = "Du måste vara inloggad som administratör.";
                return RedirectToAction("Index");
            }//kollar om användaren är admin, annars kan man inte skapa anställda.

            var json = JsonSerializer.Serialize(new
            {
                Username = newEmployee.Username,
                PasswordHash = newEmployee.PasswordHash,
                RoleID = newEmployee.RoleID,
                EmailAddress = newEmployee.EmailAddress
            });//konverterar den nya anställdas data till JSON-format.


            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("register", content);//skickar post-förfrågan till /register.
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)//om anropet misslyckas
            {
                TempData["ErrorMessage"] = $"Fel vid skapande av anställd: {response.StatusCode} - {responseBody}";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
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





        [HttpPut("updateEmployee/{userId}")]
        public async Task<IActionResult> UpdateEmployee(int userId, [FromForm] User updatedEmployee, [FromForm] string _method)
        {
            System.Diagnostics.Debug.WriteLine($"🚀 GUI: UpdateEmployee-metoden anropad för UserID: {userId}");

            // Kontrollera om metoden är PUT (för att säkerställa att den simulerar PUT korrekt).
            if (_method != "PUT")
            {
                System.Diagnostics.Debug.WriteLine("❌ Felaktig HTTP-metod mottagen!");
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

            // Skickar PUT-anrop till API:et
            var response = await _httpClient.PutAsync($"updateEmployee/{userId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = $"Fel vid uppdatering av anställd: {response.StatusCode} - {responseBody}";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }






        [HttpGet("Edit/{userId}")]
        public async Task<IActionResult> Edit(int userId)
        {
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                return RedirectToAction("Login", "Account");
            }

            var response = await _httpClient.GetAsync($"employees");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Kunde inte hämta anställda.";
                return RedirectToAction("Index");
            }

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

            return View(employee);
        }



    }
}
