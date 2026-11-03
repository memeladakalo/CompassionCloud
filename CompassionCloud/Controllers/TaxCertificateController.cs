using System.Security.Cryptography;
using System.Text;
using CompassionCloud.Data;
using CompassionCloud.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompassionCloud.Controllers
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int taxYear)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var donations = await _context.Donations
                .Where(d => d.UserId == user.Id && d.DonationDate.Year == taxYear)
                .ToListAsync();

            if (!donations.Any())
            {
                ModelState.AddModelError("", $"No donations found for the {taxYear} tax year.");
                return View();
            }

            ViewBag.TaxYear = taxYear;
            // Total and certificate number come from the shared Helpers NuGet library
            ViewBag.Total = DonationHelper.CalculateDonationTotal(donations.Select(d => d.Amount));
            ViewBag.DonationCount = donations.Count;
            ViewBag.DonorName = user.UserName;
            ViewBag.CertificateNumber = DonationHelper.FormatTaxCertificateNumber(StableReference(user.Id, taxYear));

            return View("CertificateView");
        }

        // Same donor + year always produces the same certificate number.
        private static string StableReference(string userId, int taxYear)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}-{taxYear}"));
            return Convert.ToHexString(hash)[..12];
        }
    }
}
