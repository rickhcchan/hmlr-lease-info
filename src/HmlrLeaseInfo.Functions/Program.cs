using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Entry point for the Azure Functions isolated worker process.
/// </summary>
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
