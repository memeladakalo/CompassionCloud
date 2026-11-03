using System.Net;
using System.Text;
using CompassionCloud.Controllers;
using CompassionCloud.Data;
using CompassionCloud.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CompassionCloud.Tests
{
    [Trait("Category", "Integration")]
    public class DatabaseIntegrationTests
    {
        private static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        // ---- Integration test 1: entity -> database round trip ----
        [Fact]
        public void CanSaveAndRetrieveDonationFromDatabase()
        {
            using var context = NewContext();

            context.Donations.Add(new Donation
            {
                DonorName = "Integration Tester",
                DonorEmail = "test@test.com",
                Amount = 150.00m,
                Currency = "ZAR",
                DonationType = "One-time",
                ReferenceNumber = "TEST1234"
            });
            context.SaveChanges();

            var saved = context.Donations.FirstOrDefault(d => d.ReferenceNumber == "TEST1234");

            Assert.NotNull(saved);
            Assert.Equal(150.00m, saved!.Amount);
            Assert.Equal("Integration Tester", saved.DonorName);
        }

        // ---- Integration test 2: form (controller POST) -> Azure Function call -> database ----
        [Fact]
        public async Task DonationForm_Post_CallsFunctionAndPersistsDonation()
        {
            using var context = NewContext();

            const string certificateJson = "{\"certificate\":\"CC-TEST\"}";
            var handler = new FakeHandler(HttpStatusCode.OK, certificateJson);
            var controller = BuildController(context, handler);

            var form = new Donation
            {
                DonorName = "Form Donor",
                DonorEmail = "form@test.com",
                Amount = 500m,
                Currency = "ZAR",
                DonationType = "One-time",
                IsAnonymous = true
            };

            var result = await controller.Index(form);

            // 1. Controller redirects to the Thank You page
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ThankYou", redirect.ActionName);

            // 2. Donation was written to the database with a generated reference
            var saved = Assert.Single(context.Donations.ToList());
            Assert.Equal(500m, saved.Amount);
            Assert.Equal(8, saved.ReferenceNumber.Length);

            // 3. The Azure Function was called, anonymously, with the donation payload
            Assert.Equal(1, handler.CallCount);
            Assert.Contains("\"donorName\":\"Anonymous\"", handler.LastRequestBody);
            Assert.Contains("\"amount\":500", handler.LastRequestBody);

            // 4. The certificate text from the function's response is handed to the Thank You page
            Assert.Equal("CC-TEST", controller.TempData["TaxCertificate"]);
        }

        // ---- Integration test 3: the donation must still be saved if the Function is down ----
        [Fact]
        public async Task DonationForm_Post_FunctionFails_DonationStillSaved()
        {
            using var context = NewContext();
            var controller = BuildController(context, new FakeHandler(HttpStatusCode.InternalServerError, ""));

            await controller.Index(new Donation
            {
                DonorName = "Resilient Donor",
                DonorEmail = "r@test.com",
                Amount = 75m,
                Currency = "USD",
                DonationType = "Recurring"
            });

            Assert.Single(context.Donations.ToList());
        }

        // ---------------- helpers ----------------
        private static DonationController BuildController(ApplicationDbContext context, FakeHandler handler)
        {
            var userManager = new UserManager<IdentityUser>(
                new UserStore<IdentityUser>(context), null!, new PasswordHasher<IdentityUser>(),
                null!, null!, null!, null!, null!, null!);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Functions:TaxCertificateUrl"] = "http://fake-function.local/api/taxcertificate/generate"
                })
                .Build();

            var httpContext = new DefaultHttpContext();
            var controller = new DonationController(context, userManager, new FakeHttpClientFactory(handler), config)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new NullTempDataProvider())
            };
            return controller;
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public int CallCount { get; private set; }
            public string LastRequestBody { get; private set; } = "";

            public FakeHandler(HttpStatusCode status, string body) { _status = status; _body = body; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                CallCount++;
                LastRequestBody = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
            }
        }

        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpMessageHandler _handler;
            public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
            public HttpClient CreateClient(string name) => new HttpClient(_handler, disposeHandler: false);
        }

        private sealed class NullTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }
    }
}
