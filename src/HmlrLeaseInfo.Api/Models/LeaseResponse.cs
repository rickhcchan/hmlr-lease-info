namespace HmlrLeaseInfo.Api.Models;

/// <summary>
/// Envelope for non-200 responses (202 Processing, 404 Not Found).
/// </summary>
public record LeaseResponse(string Message, DateTime? LastSyncAt = null);
