using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CompassionCloud.Services
{
    public static class RolesInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                logger.LogInformation("Starting role initialization...");

                // Check if database is available
                try
                {
                    var roleExists = await roleManager.RoleExistsAsync("Employee");
                    logger.LogInformation("Database connection successful!");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Database connection failed: {ex.Message}");
                    return; // Exit gracefully if database isn't ready
                }

                // ============================================================
                // UPDATED: Four roles now match the wireframe dashboards
                // ============================================================
                string[] roleNames = { "Donor", "Volunteer", "Employee", "Admin" };

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        logger.LogInformation($"Creating role: {roleName}");
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // ============================================================
                // SEED DEFAULT USERS (one per role, for easy testing/demo)
                // ============================================================

                // Default Employee (Project Lead / Coordinator)
                await CreateUserIfNotExists(
                    userManager, logger,
                    email: "employee@compassioncloud.com",
                    password: "Employee@123",
                    role: "Employee");

                // Default Admin (System Administrator)
                await CreateUserIfNotExists(
                    userManager, logger,
                    email: "admin@compassioncloud.com",
                    password: "Admin@123",
                    role: "Admin");

                // Default Donor (for testing the Donor Dashboard)
                await CreateUserIfNotExists(
                    userManager, logger,
                    email: "donor@compassioncloud.com",
                    password: "Donor@123",
                    role: "Donor");

                // Default Volunteer (for testing the Volunteer Dashboard)
                await CreateUserIfNotExists(
                    userManager, logger,
                    email: "volunteer@compassioncloud.com",
                    password: "Volunteer@123",
                    role: "Volunteer");

                logger.LogInformation("Role initialization completed.");
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the app
                Console.WriteLine($"Error initializing roles: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // ============================================================
        // HELPER METHOD — Avoids repeating user-creation code
        // ============================================================
        private static async Task CreateUserIfNotExists(
            UserManager<IdentityUser> userManager,
            ILogger<object> logger,
            string email,
            string password,
            string role)
        {
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                logger.LogInformation($"Creating default {role} user: {email}");

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation($"Default {role} user created successfully!");
                }
                else
                {
                    logger.LogError(
                        $"Failed to create {role} user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"{role} user ({email}) already exists.");

                // Ensure the user has the correct role (in case roles changed)
                if (!await userManager.IsInRoleAsync(existingUser, role))
                {
                    await userManager.AddToRoleAsync(existingUser, role);
                    logger.LogInformation($"Assigned {role} role to existing user {email}");
                }
            }
        }
    }
}