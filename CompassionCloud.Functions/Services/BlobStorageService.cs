using System.Text;
using Azure.Storage.Blobs;

namespace CompassionCloud.Functions.Services
{
    /// <summary>Writes each relief update as a JSON blob in the "relief-update-logs" container.</summary>
    public class BlobStorageService : IReliefLogStore
    {
        private const string ContainerName = "relief-update-logs";
        private readonly BlobContainerClient _container;

        public BlobStorageService()
        {
            // Same setting the Functions host uses; Azurite / storage emulator locally.
            var connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
            _container = new BlobContainerClient(connection, ContainerName);
        }

        public async Task SaveAsync(string json, CancellationToken cancellationToken = default)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}.json";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await _container.GetBlobClient(blobName).UploadAsync(stream, overwrite: false, cancellationToken);
        }
    }
}
