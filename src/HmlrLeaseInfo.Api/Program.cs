var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory integration tests.
/// </summary>
public partial class Program;
