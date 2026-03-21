namespace HmlrLeaseInfo.Api.Configuration;

/// <summary>
/// Azure Storage connection settings for Table Storage and Queue Storage.
/// </summary>
public class StorageSettings
{
    /// <summary>Connection string for Azure Storage (Azurite for local dev).</summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
}
