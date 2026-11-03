using CompassionCloud.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    // ASP.NET Core integration: lets functions use HttpRequest / IActionResult
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IReliefLogStore, BlobStorageService>();
    })
    .Build();

host.Run();
