using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CompassionCloud.Functions.Functions
{
    public class LogReliefUpdate
    {
        private readonly ILogger<LogReliefUpdate> _logger;

        public LogReliefUpdate(ILogger<LogReliefUpdate> logger)
        {
            _logger = logger;
        }

        [Function("LogReliefUpdate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reliefupdates/log")]
            HttpRequest req,
            [Blob("relief-update-logs/{rand-guid}.json", FileAccess.Write, Connection = "AzureWebJobsStorage")]
            Stream outputBlob)
        {
            _logger.LogInformation("LogReliefUpdate function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            ReliefUpdateLog? data;
            try
            {
                data = JsonSerializer.Deserialize<ReliefUpdateLog>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"Invalid JSON: {ex.Message}");
                return new BadRequestObjectResult(new { error = "Invalid JSON payload." });
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Title))
            {
                return new BadRequestObjectResult(new { error = "Title is required." });
            }

            // Add server-side metadata
            data.LoggedAt = DateTime.UtcNow;

            // Write the update to blob storage
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            await outputBlob.WriteAsync(bytes);

            _logger.LogInformation($"Relief update '{data.Title}' logged to Azure Storage.");

            return new OkObjectResult(new
            {
                success = true,
                message = "Relief update logged to Azure Storage.",
                title = data.Title,
                loggedAt = data.LoggedAt
            });
        }
    }

    public class ReliefUpdateLog
    {
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PostedBy { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }
}