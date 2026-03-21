using HmlrLeaseInfo.Api.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// DI registrations will be added in Task 6

var app = builder.Build();

app.MapGet("/{titleNumber}", async (string titleNumber, ILeaseService leaseService, CancellationToken ct) =>
    await leaseService.GetLeaseAsync(titleNumber, ct));

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory integration tests.
/// </summary>
public partial class Program;
