namespace HmlrLeaseInfo.Infrastructure.Tests;

using System.Diagnostics;

/// <summary>
/// Starts and stops Azurite for integration tests.
/// Shared across all Table Storage test classes via xUnit collection fixture.
/// </summary>
public class AzuriteFixture : IAsyncLifetime
{
    private Process? _process;

    public async Task InitializeAsync()
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "azurite",
                Arguments = "--silent",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        _process.Start();

        // Wait for Azurite to be ready
        await WaitForAzuriteAsync();
    }

    public Task DisposeAsync()
    {
        if (_process is null)
            return Task.CompletedTask;

        try
        {
            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
        return Task.CompletedTask;
    }

    private static async Task WaitForAzuriteAsync()
    {
        using var httpClient = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                await httpClient.GetAsync("http://127.0.0.1:10002");
                return;
            }
            catch
            {
                await Task.Delay(200);
            }
        }
        throw new InvalidOperationException("Azurite failed to start within timeout.");
    }
}

[CollectionDefinition("Azurite")]
public class AzuriteCollection : ICollectionFixture<AzuriteFixture>;
