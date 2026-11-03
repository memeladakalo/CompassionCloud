using CompassionCloud.Data;
using CompassionCloud.Models;
using CompassionCloud.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CompassionCloud.Controllers
{
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DonationController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index() => View(new Donation());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Donation donation)
        {
            if (!ModelState.IsValid) return View(donation);

            if (User.Identity!.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                donation.UserId = user?.Id;
            }

            // Reference number comes from the shared Helpers NuGet library
            donation.ReferenceNumber = DonationHelper.GenerateDonationReference();
            donation.DonationDate = DateTime.UtcNow;

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Call the Azure Function to generate a tax certificate
            try
            {
                var functionUrl = _configuration["Functions:TaxCertificateUrl"]
                    ?? "http://localhost:7071/api/taxcertificate/generate";

                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    donorName = donation.IsAnonymous ? "Anonymous" : donation.DonorName,
                    donorEmail = donation.DonorEmail,
                    amount = donation.Amount,
                    currency = donation.Currency,
                    donationType = donation.DonationType
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    TempData["TaxCertificate"] = ExtractCertificateText(responseBody);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the donation if the function fails
                Console.WriteLine($"Function call failed: {ex.Message}");
            }

            TempData["DonationId"] = donation.Id;
            return RedirectToAction("ThankYou");
        }

        // The function returns JSON; show only the printable certificate text if present.
        private static string ExtractCertificateText(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("certificate", out var cert) &&
                    cert.ValueKind == JsonValueKind.String)
                {
                    return cert.GetString() ?? responseBody;
                }
            }
            catch (JsonException) { }
            return responseBody;
        }

        [HttpGet]
        public IActionResult ThankYou()
        {
            if (TempData["DonationId"] == null) return RedirectToAction("Index");

            var id = (int)TempData["DonationId"];
            var donation = _context.Donations.Find(id);
            if (donation == null) return RedirectToAction("Index");

            ViewBag.TaxCertificate = TempData["TaxCertificate"]?.ToString();

            return View(donation);
        }
    }
}