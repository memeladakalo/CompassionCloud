using System.Text;
using System.Text.Json;
using CompassionCloud.Data;
using CompassionCloud.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompassionCloud.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmployeeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /Employee/Dashboard - stats + recent activity
        [HttpGet]
        public IActionResult Dashboard()
        {
            var model = new EmployeeDashboardViewModel
            {
                RecentUpdates = _context.ReliefProjectUpdates
                    .OrderByDescending(u => u.DatePosted).Take(10).ToList(),
                RecentVolunteers = _context.Volunteers
                    .OrderByDescending(v => v.RegisteredDate).Take(10).ToList(),
                TotalVolunteers = _context.Volunteers.Count(),
                TotalUpdates = _context.ReliefProjectUpdates.Count(),
                TotalDonations = _context.Donations.Count(),
                TotalDonationAmount = _context.Donations.Any() ? _context.Donations.Sum(d => d.Amount) : 0
            };

            return View(model);
        }

        // GET: /Employee/PostUpdate
        [HttpGet]
        public IActionResult PostUpdate() => View(new ReliefProjectUpdate());

        // POST: /Employee/PostUpdate - saves the update, then logs it to Azure Storage via the Function
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostUpdate(ReliefProjectUpdate update)
        {
            if (!ModelState.IsValid) return View(update);

            var user = await _userManager.GetUserAsync(User);
            update.PostedByUserId = user?.Id;
            update.DatePosted = DateTime.Now;

            _context.ReliefProjectUpdates.Add(update);
            await _context.SaveChangesAsync();

            await LogUpdateToFunctionAsync(update, user?.UserName);

            TempData["UpdateSuccess"] = "Update published successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult Volunteers()
        {
            var volunteers = _context.Volunteers.OrderByDescending(v => v.RegisteredDate).ToList();
            return View(volunteers);
        }

        [HttpGet]
        public IActionResult Donors()
        {
            var donations = _context.Donations.OrderByDescending(d => d.DonationDate).ToList();
            ViewBag.TotalAmount = donations.Any() ? donations.Sum(d => d.Amount) : 0;
            ViewBag.TotalCount = donations.Count;
            return View(donations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUpdate(int id)
        {
            var update = await _context.ReliefProjectUpdates.FindAsync(id);
            if (update != null)
            {
                _context.ReliefProjectUpdates.Remove(update);
                await _context.SaveChangesAsync();
                TempData["UpdateSuccess"] = "Update deleted.";
            }
            return RedirectToAction("Dashboard");
        }

        // The update is already saved; a Function outage must never fail the employee's request.
        private async Task LogUpdateToFunctionAsync(ReliefProjectUpdate update, string? postedBy)
        {
            try
            {
                var url = _configuration["Functions:LogReliefUpdateUrl"]
                          ?? "http://localhost:7071/api/reliefupdates/log";

                var payload = JsonSerializer.Serialize(new
                {
                    title = update.Title,
                    location = update.Location,
                    description = update.Description,
                    postedBy = postedBy ?? "unknown"
                });

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("LogReliefUpdate function returned {Status}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not log relief update to Azure Function.");
            }
        }
    }
}
