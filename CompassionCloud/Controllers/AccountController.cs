using CompassionCloud.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompassionCloud.Controllers
{
    public class AccountController : Controller
    {
        // Roles a visitor may choose for themselves. Employee/Admin accounts are created by the
        // organisation (seeded in RolesInitializer) - they can never be self-assigned.
        private static readonly string[] SelfRegisterRoles = { "Donor", "Volunteer" };

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                var user = await _userManager.FindByEmailAsync(model.Email);
                return await RedirectToDashboardAsync(user);
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError("AgreeToTerms", "You must accept the terms.");
                return View(model);
            }

            if (!SelfRegisterRoles.Contains(model.Role))
            {
                ModelState.AddModelError("Role", "Please choose Donor or Volunteer.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                return model.Role == "Volunteer"
                    ? RedirectToAction("Register", "Volunteer")
                    : RedirectToAction("Dashboard", "Donor");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // Send each role to a dashboard that actually exists.
        private async Task<IActionResult> RedirectToDashboardAsync(IdentityUser? user)
        {
            if (user == null) return RedirectToAction("Index", "Home");

            if (await _userManager.IsInRoleAsync(user, "Admin") ||
                await _userManager.IsInRoleAsync(user, "Employee"))
                return RedirectToAction("Dashboard", "Employee");

            if (await _userManager.IsInRoleAsync(user, "Volunteer"))
                return RedirectToAction("Dashboard", "Volunteer");

            return RedirectToAction("Dashboard", "Donor");
        }
    }

    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
