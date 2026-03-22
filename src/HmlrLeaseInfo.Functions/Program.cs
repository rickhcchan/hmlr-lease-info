using Azure.Data.Tables;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Parsing;
using HmlrLeaseInfo.Functions.Interfaces;
using HmlrLeaseInfo.Functions.Services;
using HmlrLeaseInfo.Infrastructure.Http;
using HmlrLeaseInfo.Infrastructure.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.Configure<HmlrApiSettings>(context.Configuration.GetSection("HmlrApi"));
        services.Configure<SyncOptions>(context.Configuration.GetSection("Sync"));

        var storageConnection = context.Configuration["AzureWebJobsStorage"]!;

        services.AddSingleton<ILeaseRepository>(_ =>
        {
            var tableClient = new TableClient(storageConnection, "Leases");
            tableClient.CreateIfNotExists();
            return new TableStorageLeaseRepository(tableClient);
        });

        services.AddSingleton<ISyncMetadataRepository>(_ =>
        {
            var tableClient = new TableClient(storageConnection, "Sync");
            tableClient.CreateIfNotExists();
            return new TableStorageSyncMetadataRepository(tableClient);
        });

        services.AddHttpClient<IHmlrClient, HmlrApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HmlrApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });
        services.AddSingleton<ILeaseParser, LeaseParser>();
        services.AddScoped<ISyncService, SyncService>();
    })
    .Build();

host.Run();
