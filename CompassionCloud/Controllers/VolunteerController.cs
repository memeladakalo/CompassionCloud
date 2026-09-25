using CompassionCloud.Models;
using CompassionCloud.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CompassionCloud.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VolunteerController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new Volunteer();
            if (User.Identity.IsAuthenticated)
            {
                model.FullName = User.Identity.Name;
                model.Email = User.Identity.Name;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Volunteer volunteer)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    volunteer.UserId = user.Id;
                }

                volunteer.RegisteredDate = DateTime.Now;
                _context.Volunteers.Add(volunteer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for registering as a volunteer!";
                return RedirectToAction("Register");
            }

            if (User.Identity.IsAuthenticated)
            {
                volunteer.FullName = User.Identity.Name;
                volunteer.Email = User.Identity.Name;
            }

            return View(volunteer);
        }
    }
}