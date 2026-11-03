using CompassionCloud.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompassionCloud.Controllers
{
    [Authorize]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DonorController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var donations = user == null
                ? new List<Models.Donation>()
                : _context.Donations.Where(d => d.UserId == user.Id).ToList();

            ViewBag.TotalDonated = donations.Sum(d => d.Amount);
            ViewBag.RecurringCount = donations.Count(d => d.DonationType == "Recurring");
            // one certificate can be generated per tax year that has donations
            ViewBag.CertificateCount = donations.Select(d => d.DonationDate.Year).Distinct().Count();

            return View();
        }
    }
}
