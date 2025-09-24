namespace VivaldiModManager.Core.Exceptions;

/// <summary>
/// Base exception for all manifest-related errors.
/// </summary>
public class ManifestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestException"/> class.
    /// </summary>
    public ManifestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ManifestException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ManifestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a manifest file is not found.
/// </summary>
public class ManifestNotFoundException : ManifestException
{
    /// <summary>
    /// Gets the path to the manifest file that was not found.
    /// </summary>
    public string ManifestPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestNotFoundException"/> class.
    /// </summary>
    /// <param name="manifestPath">The path to the manifest file that was not found.</param>
    public ManifestNotFoundException(string manifestPath) 
        : base($"Manifest file not found at path: {manifestPath}")
    {
        ManifestPath = manifestPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="manifestPath">The path to the manifest file that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManifestNotFoundException(string manifestPath, Exception innerException)
        : base($"Manifest file not found at path: {manifestPath}", innerException)
    {
        ManifestPath = manifestPath;
    }
}

/// <summary>
/// Exception thrown when a manifest file is corrupted or invalid.
/// </summary>
public class ManifestCorruptedException : ManifestException
{
    /// <summary>
    /// Gets the path to the corrupted manifest file.
    /// </summary>
    public string ManifestPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestCorruptedException"/> class.
    /// </summary>
    /// <param name="manifestPath">The path to the corrupted manifest file.</param>
    /// <param name="message">The error message.</param>
    public ManifestCorruptedException(string manifestPath, string message)
        : base($"Manifest file is corrupted at path '{manifestPath}': {message}")
    {
        ManifestPath = manifestPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestCorruptedException"/> class with an inner exception.
    /// </summary>
    /// <param name="manifestPath">The path to the corrupted manifest file.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManifestCorruptedException(string manifestPath, string message, Exception innerException)
        : base($"Manifest file is corrupted at path '{manifestPath}': {message}", innerException)
    {
        ManifestPath = manifestPath;
    }
}

/// <summary>
/// Exception thrown when a manifest has an incompatible schema version.
/// </summary>
public class ManifestSchemaException : ManifestException
{
    /// <summary>
    /// Gets the current schema version found in the manifest.
    /// </summary>
    public int CurrentVersion { get; }

    /// <summary>
    /// Gets the expected schema version.
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestSchemaException"/> class.
    /// </summary>
    /// <param name="currentVersion">The current schema version found.</param>
    /// <param name="expectedVersion">The expected schema version.</param>
    public ManifestSchemaException(int currentVersion, int expectedVersion)
        : base($"Manifest schema version mismatch. Found version {currentVersion}, expected version {expectedVersion}.")
    {
        CurrentVersion = currentVersion;
        ExpectedVersion = expectedVersion;
    }
}

/// <summary>
/// Exception thrown when there's an error performing file I/O operations on manifest files.
/// </summary>
public class ManifestIOException : ManifestException
{
    /// <summary>
    /// Gets the file path where the I/O error occurred.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the operation that was being performed when the error occurred.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestIOException"/> class.
    /// </summary>
    /// <param name="filePath">The file path where the error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    public ManifestIOException(string filePath, string operation, string message)
        : base($"I/O error during {operation} operation on file '{filePath}': {message}")
    {
        FilePath = filePath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestIOException"/> class with an inner exception.
    /// </summary>
    /// <param name="filePath">The file path where the error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManifestIOException(string filePath, string operation, string message, Exception innerException)
        : base($"I/O error during {operation} operation on file '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
        Operation = operation;
    }
}