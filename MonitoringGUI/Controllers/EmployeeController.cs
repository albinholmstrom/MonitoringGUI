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
            System.Diagnostics.Debug.WriteLine("🟢 Index-metoden startad.");

            // Hämtar sessionens ID och kontrollerar att den är giltig
            var sessionId = HttpContext.Session.GetString("SessionID");
            if (string.IsNullOrEmpty(sessionId))
            {
                System.Diagnostics.Debug.WriteLine("❌ Ingen aktiv session hittades.");
                ViewBag.ErrorMessage = "Ingen aktiv session. Logga in igen.";
                return RedirectToAction("Login", "Account");
            }

            System.Diagnostics.Debug.WriteLine($"✅ SessionID hittades: {sessionId}");

            // Lägg till sessionID i http-headern
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"SessionID={sessionId}");

            // Anropar /protected för att verifiera sessionen
            var response = await _httpClient.GetAsync("protected");
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("❌ Sessionen är ogiltig eller har gått ut.");
                ViewBag.ErrorMessage = "Sessionen är ogiltig eller har gått ut.";
                return RedirectToAction("Login", "Account");
            }

            // Läser av API-svaret och kontrollerar att userID och roleID finns med
            var responseBody = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"🔎 API-svar för sessionen: {responseBody}");

            var sessionData = JsonDocument.Parse(responseBody).RootElement;

            if (!sessionData.TryGetProperty("userId", out JsonElement userIdElement) ||
                !sessionData.TryGetProperty("roleId", out JsonElement roleIdElement))
            {
                System.Diagnostics.Debug.WriteLine("❌ Fel vid hämtning av session - userId/roleId saknas.");
                ViewBag.ErrorMessage = "Fel vid hämtning av session.";
                return RedirectToAction("Login", "Account");
            }

            // Uppdaterar sessionen om nödvändigt
            int userId = userIdElement.GetInt32();
            int roleId = roleIdElement.GetInt32();

            System.Diagnostics.Debug.WriteLine($"✅ Session uppdaterad → UserID: {userId}, RoleID: {roleId}");

            if (HttpContext.Session.GetInt32("UserID") != userId || HttpContext.Session.GetInt32("UserRole") != roleId)
            {
                HttpContext.Session.SetInt32("UserID", userId);
                HttpContext.Session.SetInt32("UserRole", roleId);
            }

            // Hämta lista över anställda
            var employeesResponse = await _httpClient.GetAsync("employees");
            if (!employeesResponse.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("❌ Kunde inte hämta anställda från API.");
                ViewBag.ErrorMessage = "Kunde inte hämta anställda.";
                return View(new List<User>());
            }

            var employeesJson = await employeesResponse.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<User>>(employeesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            System.Diagnostics.Debug.WriteLine($"🔎 Antal användare hämtade från API: {employees.Count}");

            // Logga varje användare för att se roller och data
            foreach (var user in employees)
            {
                System.Diagnostics.Debug.WriteLine($"👤 User: {user.Username}, RoleID: {user.RoleID}");
            }

            ViewBag.IsAdmin = roleId == 1; // Används i View för att visa/dölja admin-funktioner

            System.Diagnostics.Debug.WriteLine("✅ Index-metoden slutförd – data skickad till vyn.");

            return View(employees);
        }




        [HttpPost]
        public async Task<IActionResult> AddEmployee(User newEmployee)
        {
            System.Diagnostics.Debug.WriteLine("🟢 AddEmployee-metoden anropad!");

            System.Diagnostics.Debug.WriteLine($"➡️ Användarnamn: {newEmployee.Username}");
            System.Diagnostics.Debug.WriteLine($"📧 E-post: {newEmployee.EmailAddress}");
            System.Diagnostics.Debug.WriteLine($"🔑 RoleID: {newEmployee.RoleID}");

            newEmployee.UserID = 0; // Nollställ UserID

            // ✅ Om rollen är 2 (anställd) → Kolla om användaren är admin
            if (newEmployee.RoleID == 2)
            {
                var adminRole = HttpContext.Session.GetInt32("UserRole");

                System.Diagnostics.Debug.WriteLine($"🔍 Kontroll av admin-behörighet – AdminRole: {adminRole}");

                if (adminRole != 1)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Du är inte admin → Avbryter registrering");
                    TempData["ErrorMessage"] = "Du måste vara inloggad som administratör.";
                    return RedirectToAction("Index");
                }
            }

            // ✅ Konvertera data till JSON-format
            var json = JsonSerializer.Serialize(new
            {
                Username = newEmployee.Username,
                PasswordHash = newEmployee.PasswordHash,
                RoleID = newEmployee.RoleID,
                EmailAddress = newEmployee.EmailAddress
            });

            System.Diagnostics.Debug.WriteLine($"🔄 JSON som skickas till API: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("register", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"🔎 API-responsstatus: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"🔎 API-responsdata: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid skapande av användare: {response.StatusCode} - {responseBody}");
                TempData["ErrorMessage"] = $"Fel vid skapande av användare: {response.StatusCode} - {responseBody}";
                return RedirectToAction("Index");
            }

            System.Diagnostics.Debug.WriteLine("✅ Registreringen lyckades!");

            // ✅ Om det är en kund (RoleID = 3) → Omdirigera till login + visa meddelande
            if (newEmployee.RoleID == 3)
            {
                System.Diagnostics.Debug.WriteLine("✅ RoleID = 3 → Användare är kund → Omdirigerar till Login");
                TempData["SuccessMessage"] = "✅ Kontot har registrerats! Du kan nu logga in.";
                return RedirectToAction("Login", "Account");
            }

            // ✅ Om det är en anställd (RoleID = 2) → Gå tillbaka till listan direkt
            System.Diagnostics.Debug.WriteLine("✅ RoleID = 2 → Användare är anställd → Omdirigerar till Index");
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
