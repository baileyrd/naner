using System;

namespace Naner.Core.Events;

/// <summary>
/// Base class for all Naner events.
/// </summary>
public abstract class NanerEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a vendor installation starts.
/// </summary>
public class VendorInstallStartedEvent : NanerEvent
{
    public string VendorName { get; }
    public string? Version { get; }

    public VendorInstallStartedEvent(string vendorName, string? version = null)
    {
        VendorName = vendorName;
        Version = version;
    }
}

/// <summary>
/// Event raised when a vendor installation completes successfully.
/// </summary>
public class VendorInstallCompletedEvent : NanerEvent
{
    public string VendorName { get; }
    public string InstallPath { get; }
    public TimeSpan Duration { get; }

    public VendorInstallCompletedEvent(string vendorName, string installPath, TimeSpan duration)
    {
        VendorName = vendorName;
        InstallPath = installPath;
        Duration = duration;
    }
}

/// <summary>
/// Event raised when a vendor installation fails.
/// </summary>
public class VendorInstallFailedEvent : NanerEvent
{
    public string VendorName { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }

    public VendorInstallFailedEvent(string vendorName, string errorMessage, Exception? exception = null)
    {
        VendorName = vendorName;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}

/// <summary>
/// Event raised when a download starts.
/// </summary>
public class DownloadStartedEvent : NanerEvent
{
    public string Url { get; }
    public string FileName { get; }
    public long? TotalBytes { get; }

    public DownloadStartedEvent(string url, string fileName, long? totalBytes = null)
    {
        Url = url;
        FileName = fileName;
        TotalBytes = totalBytes;
    }
}

/// <summary>
/// Event raised periodically during download to report progress.
/// </summary>
public class DownloadProgressEvent : NanerEvent
{
    public string FileName { get; }
    public long BytesDownloaded { get; }
    public long TotalBytes { get; }
    public int PercentComplete { get; }

    public DownloadProgressEvent(string fileName, long bytesDownloaded, long totalBytes)
    {
        FileName = fileName;
        BytesDownloaded = bytesDownloaded;
        TotalBytes = totalBytes;
        PercentComplete = totalBytes > 0 ? (int)((bytesDownloaded * 100) / totalBytes) : 0;
    }
}

/// <summary>
/// Event raised when a download completes successfully.
/// </summary>
public class DownloadCompletedEvent : NanerEvent
{
    public string FileName { get; }
    public string LocalPath { get; }
    public long TotalBytes { get; }
    public TimeSpan Duration { get; }

    public DownloadCompletedEvent(string fileName, string localPath, long totalBytes, TimeSpan duration)
    {
        FileName = fileName;
        LocalPath = localPath;
        TotalBytes = totalBytes;
        Duration = duration;
    }
}

/// <summary>
/// Event raised when a download fails.
/// </summary>
public class DownloadFailedEvent : NanerEvent
{
    public string Url { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }

    public DownloadFailedEvent(string url, string errorMessage, Exception? exception = null)
    {
        Url = url;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}

/// <summary>
/// Event raised when archive extraction starts.
/// </summary>
public class ExtractionStartedEvent : NanerEvent
{
    public string ArchivePath { get; }
    public string DestinationPath { get; }

    public ExtractionStartedEvent(string archivePath, string destinationPath)
    {
        ArchivePath = archivePath;
        DestinationPath = destinationPath;
    }
}

/// <summary>
/// Event raised when archive extraction completes.
/// </summary>
public class ExtractionCompletedEvent : NanerEvent
{
    public string ArchivePath { get; }
    public string DestinationPath { get; }
    public TimeSpan Duration { get; }

    public ExtractionCompletedEvent(string archivePath, string destinationPath, TimeSpan duration)
    {
        ArchivePath = archivePath;
        DestinationPath = destinationPath;
        Duration = duration;
    }
}

/// <summary>
/// Event raised when configuration is loaded.
/// </summary>
public class ConfigurationLoadedEvent : NanerEvent
{
    public string ConfigPath { get; }
    public string Format { get; }

    public ConfigurationLoadedEvent(string configPath, string format)
    {
        ConfigPath = configPath;
        Format = format;
    }
}

/// <summary>
/// Event raised when a command is executed.
/// </summary>
public class CommandExecutedEvent : NanerEvent
{
    public string CommandName { get; }
    public string[] Arguments { get; }
    public int ExitCode { get; }
    public TimeSpan Duration { get; }

    public CommandExecutedEvent(string commandName, string[] arguments, int exitCode, TimeSpan duration)
    {
        CommandName = commandName;
        Arguments = arguments;
        ExitCode = exitCode;
        Duration = duration;
    }
}
