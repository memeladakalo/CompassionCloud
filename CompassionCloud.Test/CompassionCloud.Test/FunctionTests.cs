using System.Text;
using CompassionCloud.Functions.Functions;
using CompassionCloud.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompassionCloud.Tests
{
    /// <summary>
    /// Tests the Azure Functions directly (no host needed): happy paths AND negative tests.
    /// </summary>
    [Trait("Category", "Unit")]
    public class FunctionTests
    {
        private static HttpRequest RequestWithBody(string body)
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/json";
            return context.Request;
        }

        private sealed class FakeStore : IReliefLogStore
        {
            public List<string> Saved { get; } = new();
            public bool ShouldFail { get; set; }

            public Task SaveAsync(string json, CancellationToken cancellationToken = default)
            {
                if (ShouldFail) throw new InvalidOperationException("storage down");
                Saved.Add(json);
                return Task.CompletedTask;
            }
        }

        // ---------------- GenerateTaxCertificate ----------------
        private static GenerateTaxCertificate TaxFunction() => new(NullLogger<GenerateTaxCertificate>.Instance);

        [Fact]
        public async Task TaxCertificate_ValidRequest_ReturnsOkWithCertificate()
        {
            var result = await TaxFunction().Run(RequestWithBody(
                "{\"donorName\":\"Thandi\",\"donorEmail\":\"t@test.com\",\"amount\":250,\"currency\":\"ZAR\",\"donationType\":\"One-time\"}"));

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            Assert.Contains("TAX CERTIFICATE", json);
            Assert.Contains("Thandi", json);
        }

        [Theory]
        [InlineData("not json at all")]                                                       // invalid JSON
        [InlineData("{\"donorName\":\"\",\"amount\":100,\"currency\":\"ZAR\"}")]              // missing name
        [InlineData("{\"donorName\":\"A\",\"amount\":0,\"currency\":\"ZAR\"}")]               // zero amount
        [InlineData("{\"donorName\":\"A\",\"amount\":-5,\"currency\":\"ZAR\"}")]              // negative amount
        [InlineData("{\"donorName\":\"A\",\"amount\":10,\"currency\":\"XYZ\"}")]              // unsupported currency
        [InlineData("{\"donorName\":\"A\",\"donorEmail\":\"nope\",\"amount\":10,\"currency\":\"ZAR\"}")] // bad email
        public async Task TaxCertificate_InvalidRequest_ReturnsBadRequest(string body)
        {
            var result = await TaxFunction().Run(RequestWithBody(body));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------- LogReliefUpdate ----------------
        [Fact]
        public async Task LogReliefUpdate_ValidRequest_StoresBlobAndReturnsOk()
        {
            var store = new FakeStore();
            var function = new LogReliefUpdate(NullLogger<LogReliefUpdate>.Instance, store);

            var result = await function.Run(RequestWithBody(
                "{\"title\":\"Flood relief\",\"location\":\"Durban\",\"description\":\"Food parcels delivered\",\"postedBy\":\"employee@compassioncloud.com\"}"));

            Assert.IsType<OkObjectResult>(result);
            var saved = Assert.Single(store.Saved);
            Assert.Contains("Flood relief", saved);
            Assert.Contains("Durban", saved);
        }

        [Theory]
        [InlineData("{ broken json")]                          // invalid JSON
        [InlineData("{\"title\":\"\",\"location\":\"X\"}")]    // missing title
        [InlineData("{\"title\":\"T\",\"location\":\"\"}")]    // missing location
        public async Task LogReliefUpdate_InvalidRequest_ReturnsBadRequestAndStoresNothing(string body)
        {
            var store = new FakeStore();
            var function = new LogReliefUpdate(NullLogger<LogReliefUpdate>.Instance, store);

            var result = await function.Run(RequestWithBody(body));

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(store.Saved);
        }

        [Fact]
        public async Task LogReliefUpdate_StorageFailure_Returns500()
        {
            var function = new LogReliefUpdate(NullLogger<LogReliefUpdate>.Instance, new FakeStore { ShouldFail = true });

            var result = await function.Run(RequestWithBody("{\"title\":\"T\",\"location\":\"L\"}"));

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
    }
}
