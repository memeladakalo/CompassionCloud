using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CompassionCloud.Tests
{
    /// <summary>
    /// Selenium UI tests. The site must be running (locally or on Azure) before they are run.
    ///
    /// Configuration (environment variables, all optional):
    ///   COMPASSION_BASE_URL  - site root, default http://localhost:5031
    ///                          (set to https://compassioncloud.azurewebsites.net to test the live site)
    ///   HEADLESS             - "true" to run Chrome without a window
    ///   EMPLOYEE_EMAIL / EMPLOYEE_PASSWORD - default to the seeded demo employee
    ///
    /// Tagged Category=UI so the build pipeline can exclude them (no site/browser on the build agent).
    /// </summary>
    [Trait("Category", "UI")]
    public class SeleniumUiTests
    {
        private static string BaseUrl =>
            (Environment.GetEnvironmentVariable("COMPASSION_BASE_URL") ?? "http://localhost:5031").TrimEnd('/');

        private static ChromeDriver CreateDriver()
        {
            var options = new ChromeOptions { AcceptInsecureCertificates = true };
            if (string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "true", StringComparison.OrdinalIgnoreCase))
            {
                options.AddArgument("--headless=new");
                options.AddArgument("--window-size=1400,1000");
            }
            return new ChromeDriver(options);
        }

        // ---------- Scenario 1: donor makes an anonymous donation ----------
        [Fact]
        public void AnonymousDonation_Scenario()
        {
            using var driver = CreateDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            driver.Navigate().GoToUrl($"{BaseUrl}/Donation");

            driver.FindElement(By.Id("DonorName")).SendKeys("Selenium Donor");
            driver.FindElement(By.Id("DonorEmail")).SendKeys("selenium@test.com");

            var amount = driver.FindElement(By.Id("Amount"));
            amount.Clear();
            amount.SendKeys("500");

            // tick "Make this donation anonymous"
            var anon = driver.FindElement(By.Id("IsAnonymous"));
            if (!anon.Selected) anon.Click();

            // the submit button inside the donation form (not any button in the navbar)
            driver.FindElement(By.XPath("//form[.//input[@id='Amount']]//button[@type='submit']")).Click();

            wait.Until(d => d.Url.Contains("ThankYou", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("Thank You", driver.PageSource);
        }

        // ---------- Scenario 2: employee logs in and reaches the dashboard ----------
        [Fact]
        public void EmployeeLogin_Scenario()
        {
            var email = Environment.GetEnvironmentVariable("EMPLOYEE_EMAIL") ?? "employee@compassioncloud.com";
            var password = Environment.GetEnvironmentVariable("EMPLOYEE_PASSWORD") ?? "Employee@123";

            using var driver = CreateDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

            driver.FindElement(By.Id("Email")).SendKeys(email);
            driver.FindElement(By.Id("Password")).SendKeys(password);
            driver.FindElement(By.XPath("//button[@type='submit' and normalize-space()='Log In']")).Click();

            // AccountController sends users in the Employee role to /Employee/Dashboard
            wait.Until(d => d.Url.Contains("/Employee/Dashboard", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("Employee Dashboard", driver.PageSource);
        }
    }
}
