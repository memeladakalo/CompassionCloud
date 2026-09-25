using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CompassionCloud.Functions.Functions
{
    public class GenerateTaxCertificate
    {
        private readonly ILogger<GenerateTaxCertificate> _logger;

        public GenerateTaxCertificate(ILogger<GenerateTaxCertificate> logger)
        {
            _logger = logger;
        }

        [Function("GenerateTaxCertificate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "taxcertificate/generate")]
            HttpRequest req)
        {
            _logger.LogInformation("GenerateTaxCertificate function triggered.");

            // 1. Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            TaxCertificateRequest? data;
            try
            {
                data = JsonSerializer.Deserialize<TaxCertificateRequest>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"Invalid JSON: {ex.Message}");
                return new BadRequestObjectResult(new { error = "Invalid JSON payload." });
            }

            if (data == null || string.IsNullOrWhiteSpace(data.DonorName) || data.Amount <= 0)
            {
                return new BadRequestObjectResult(new { error = "DonorName and Amount are required." });
            }

            // 2. Generate a dummy tax certificate (text placeholder)
            string referenceNumber = Guid.NewGuid().ToString("N")[..8].ToUpper();
            string certificateContent =
                $"""
                ================================================
                        COMPASSION CLOUD — TAX CERTIFICATE
                ================================================
                Reference Number : {referenceNumber}
                Donor Name       : {data.DonorName}
                Donor Email      : {data.DonorEmail}
                Donation Amount  : {data.Currency} {data.Amount:N2}
                Donation Type    : {data.DonationType}
                Date Issued      : {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC
                ------------------------------------------------
                This certifies that the above donation was made
                to Compassion Cloud in partnership with the
                Gift of the Givers Foundation.
                ================================================
                """;

            _logger.LogInformation($"Tax certificate {referenceNumber} generated for {data.DonorName}.");

            // 3. Return the certificate
            return new OkObjectResult(new
            {
                success = true,
                referenceNumber,
                donorName = data.DonorName,
                amount = data.Amount,
                currency = data.Currency,
                issuedAt = DateTime.UtcNow,
                certificate = certificateContent
            });
        }
    }

    public class TaxCertificateRequest
    {
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string DonationType { get; set; } = "One-time";
    }
}