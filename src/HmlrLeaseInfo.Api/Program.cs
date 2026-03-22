using Azure.Data.Tables;
using Azure.Storage.Queues;
using HmlrLeaseInfo.Api.Configuration;
using HmlrLeaseInfo.Api.Interfaces;
using HmlrLeaseInfo.Api.Services;
using HmlrLeaseInfo.Core.Configuration;
using HmlrLeaseInfo.Core.Interfaces;
using HmlrLeaseInfo.Core.Parsing;
using HmlrLeaseInfo.Infrastructure.Http;
using HmlrLeaseInfo.Infrastructure.Storage;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<HmlrApiSettings>(builder.Configuration.GetSection("HmlrApi"));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection("Sync"));

builder.Services.AddHybridCache();

builder.Services.AddSingleton<ILeaseRepository>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
    var tableClient = new TableClient(settings.ConnectionString, "Leases");
    tableClient.CreateIfNotExists();
    return new TableStorageLeaseRepository(tableClient);
});

builder.Services.AddSingleton<ISyncMetadataRepository>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
    var tableClient = new TableClient(settings.ConnectionString, "Sync");
    tableClient.CreateIfNotExists();
    return new TableStorageSyncMetadataRepository(tableClient);
});

builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
    var queueClient = new QueueClient(settings.ConnectionString, "sync-requests");
    queueClient.CreateIfNotExists();
    return queueClient;
});

builder.Services.AddHttpClient<IHmlrClient, HmlrApiClient>();
builder.Services.AddSingleton<ILeaseParser, LeaseParser>();
builder.Services.AddScoped<ILeaseService, LeaseService>();

var app = builder.Build();

app.MapGet("/{titleNumber}", async (string titleNumber, ILeaseService leaseService, CancellationToken ct) =>
    await leaseService.GetLeaseAsync(titleNumber, ct));

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory integration tests.
/// </summary>
public partial class Program;
