using System.Text.Json;
using CompassionCloud.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CompassionCloud.Functions.Functions
{
    public class LogReliefUpdate
    {
        private readonly ILogger<LogReliefUpdate> _logger;
        private readonly IReliefLogStore _store;

        public LogReliefUpdate(ILogger<LogReliefUpdate> logger, IReliefLogStore store)
        {
            _logger = logger;
            _store = store;
        }

        [Function("LogReliefUpdate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reliefupdates/log")]
            HttpRequest req)
        {
            _logger.LogInformation("LogReliefUpdate function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            ReliefUpdateLog? data;
            try
            {
                data = JsonSerializer.Deserialize<ReliefUpdateLog>(
                    requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Invalid JSON: {Message}", ex.Message);
                return new BadRequestObjectResult(new { error = "Invalid JSON payload." });
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Title))
                return new BadRequestObjectResult(new { error = "Title is required." });

            if (string.IsNullOrWhiteSpace(data.Location))
                return new BadRequestObjectResult(new { error = "Location is required." });

            if (data.Title.Length > 200)
                return new BadRequestObjectResult(new { error = "Title must be 200 characters or fewer." });

            data.LoggedAt = DateTime.UtcNow;

            try
            {
                await _store.SaveAsync(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write relief update to storage.");
                return new ObjectResult(new { error = "Could not write to storage." }) { StatusCode = 500 };
            }

            _logger.LogInformation("Relief update '{Title}' logged to Azure Storage.", data.Title);

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
