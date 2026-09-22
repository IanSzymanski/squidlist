namespace Squidlist.Storage.Contracts;

/// <summary>Classifies recoverable storage failures independently of platform exception types.</summary>
public enum StorageFailure
{
    RootUnavailable,
    AccessDenied,
    NotFound,
    CorruptData,
    UnsupportedVersion,
    Conflict,
    IoError
}

/// <summary>Operational storage failure. Message and inner exception are diagnostic, not UI copy.</summary>
public sealed class StorageException : Exception
{
    public StorageException(StorageFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(failure))
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }
        Failure = failure;
    }

    public StorageFailure Failure { get; }
}
