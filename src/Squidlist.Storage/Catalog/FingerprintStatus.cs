namespace Squidlist.Storage.Catalog;

/// <summary>Whether recovery fingerprint data is usable for the last observed file.</summary>
public enum FingerprintStatus
{
    NotComputed = 0,
    Ready = 1,
    Stale = 2,
    Failed = 3
}
