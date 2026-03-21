namespace HmlrLeaseInfo.Core.Interfaces;

using HmlrLeaseInfo.Core.Models;

/// <summary>
/// Parses raw HMLR schedule entries into structured lease data.
/// </summary>
public interface ILeaseParser
{
    /// <summary>
    /// Parses a raw notice of lease into its structured fields.
    /// </summary>
    ParsedNoticeOfLease Parse(RawNoticeOfLease raw);
}
