using CompassionCloud.Data;
using CompassionCloud.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompassionateCloud.Controllers
{
    [Authorize]
    public class TaxCertificateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TaxCertificateController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Generate() => View();

        [HttpPost]
        public async Task<IActionResult> Generate(int taxYear)
        {
            var user = await _userManager.GetUserAsync(User);
            var donations = await _context.Donations
                .Where(d => d.UserId == user.Id && d.DonationDate.Year == taxYear)
                .ToListAsync();

            if (!donations.Any())
            {
                ModelState.AddModelError("", $"No donations found for the {taxYear} tax year.");
                return View();
            }

            var total = donations.Sum(d => d.Amount);
            ViewBag.TaxYear = taxYear;
            ViewBag.Total = total;
            ViewBag.DonationCount = donations.Count;
            ViewBag.DonorName = user.UserName;

            return View("CertificateView");
        }
    }
}