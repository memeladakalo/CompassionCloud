using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CompassionCloud.Data;
using CompassionCloud.Models;
using Microsoft.EntityFrameworkCore;

namespace CompassionCloud.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // GET: /Employee/Dashboard
        // Main coordinator dashboard — stats + recent activity
        // ============================================================
        [HttpGet]
        public IActionResult Dashboard()
        {
            var updates = _context.ReliefProjectUpdates
                .OrderByDescending(u => u.DatePosted)
                .Take(10)
                .ToList() ?? new List<ReliefProjectUpdate>();

            var volunteers = _context.Volunteers
                .OrderByDescending(v => v.RegisteredDate)
                .Take(10)
                .ToList() ?? new List<Volunteer>();

            // Compute dashboard stats (matches wireframe Screen 12)
            var model = new EmployeeDashboardViewModel
            {
                RecentUpdates = updates,
                RecentVolunteers = volunteers,
                TotalVolunteers = _context.Volunteers.Count(),
                TotalUpdates = _context.ReliefProjectUpdates.Count(),
                TotalDonations = _context.Donations.Count(),
                TotalDonationAmount = _context.Donations.Any()
                    ? _context.Donations.Sum(d => d.Amount)
                    : 0
            };

            return View(model);
        }

        // ============================================================
        // GET: /Employee/PostUpdate
        // Shows the "Post Relief Update" form (wireframe Screen 16)
        // ============================================================
        [HttpGet]
        public IActionResult PostUpdate()
        {
            return View(new ReliefProjectUpdate());
        }

        // ============================================================
        // POST: /Employee/PostUpdate
        // Saves the new relief update
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostUpdate(ReliefProjectUpdate update)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                update.PostedByUserId = user?.Id;
                update.DatePosted = DateTime.Now;

                _context.ReliefProjectUpdates.Add(update);
                await _context.SaveChangesAsync();

                TempData["UpdateSuccess"] = "Update published successfully!";
                return RedirectToAction("Dashboard");
            }

            // If model invalid, return the same view with errors
            return View(update);
        }

        // ============================================================
        // GET: /Employee/Volunteers
        // Full list of all volunteer sign-ups (wireframe Screen 12)
        // ============================================================
        [HttpGet]
        public IActionResult Volunteers()
        {
            var volunteers = _context.Volunteers
                .OrderByDescending(v => v.RegisteredDate)
                .ToList();

            return View(volunteers);
        }

        // ============================================================
        // GET: /Employee/Donors
        // Full list of donor donations (for reporting)
        // ============================================================
        [HttpGet]
        public IActionResult Donors()
        {
            var donations = _context.Donations
                .OrderByDescending(d => d.DonationDate)
                .ToList();

            ViewBag.TotalAmount = donations.Any() ? donations.Sum(d => d.Amount) : 0;
            ViewBag.TotalCount = donations.Count;

            return View(donations);
        }

        // ============================================================
        // POST: /Employee/DeleteUpdate/{id}
        // Removes an outdated relief update
        // ============================================================
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
    }

    // ============================================================
    // VIEWMODEL — Must be OUTSIDE the controller class
    // ============================================================
    public class EmployeeDashboardViewModel
    {
        public List<ReliefProjectUpdate> RecentUpdates { get; set; } = new();
        public List<Volunteer> RecentVolunteers { get; set; } = new();

        // Dashboard stats (for stat cards)
        public int TotalVolunteers { get; set; }
        public int TotalUpdates { get; set; }
        public int TotalDonations { get; set; }
        public decimal TotalDonationAmount { get; set; }
    }
}