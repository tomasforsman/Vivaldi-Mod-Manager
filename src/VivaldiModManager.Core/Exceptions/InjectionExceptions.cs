namespace VivaldiModManager.Core.Exceptions;

/// <summary>
/// Base exception for all injection-related errors.
/// </summary>
public class InjectionException : Exception
{
    /// <summary>
    /// Gets the installation ID where the injection error occurred, if applicable.
    /// </summary>
    public string? InstallationId { get; }

    /// <summary>
    /// Gets the operation that was being performed when the error occurred.
    /// </summary>
    public string? Operation { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionException"/> class.
    /// </summary>
    public InjectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InjectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InjectionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionException"/> class with installation and operation context.
    /// </summary>
    /// <param name="installationId">The installation ID where the error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    public InjectionException(string installationId, string operation, string message)
        : base($"Injection {operation} failed for installation '{installationId}': {message}")
    {
        InstallationId = installationId;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionException"/> class with installation, operation context, and inner exception.
    /// </summary>
    /// <param name="installationId">The installation ID where the error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InjectionException(string installationId, string operation, string message, Exception innerException)
        : base($"Injection {operation} failed for installation '{installationId}': {message}", innerException)
    {
        InstallationId = installationId;
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when injection validation fails.
/// </summary>
public class InjectionValidationException : InjectionException
{
    /// <summary>
    /// Gets the validation errors that occurred.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InjectionValidationException(string message)
        : base($"Injection validation failed: {message}")
    {
        ValidationErrors = new List<string> { message };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionValidationException"/> class with multiple validation errors.
    /// </summary>
    /// <param name="validationErrors">The list of validation errors.</param>
    public InjectionValidationException(IReadOnlyList<string> validationErrors)
        : base($"Injection validation failed with {validationErrors.Count} error(s): {string.Join("; ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionValidationException"/> class with installation context.
    /// </summary>
    /// <param name="installationId">The installation ID where the validation error occurred.</param>
    /// <param name="message">The validation error message.</param>
    public InjectionValidationException(string installationId, string message)
        : base(installationId, "validation", message)
    {
        ValidationErrors = new List<string> { message };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionValidationException"/> class with installation context and multiple errors.
    /// </summary>
    /// <param name="installationId">The installation ID where the validation errors occurred.</param>
    /// <param name="validationErrors">The list of validation errors.</param>
    public InjectionValidationException(string installationId, IReadOnlyList<string> validationErrors)
        : base(installationId, "validation", $"{validationErrors.Count} validation error(s): {string.Join("; ", validationErrors)}")
    {
        ValidationErrors = validationErrors;
    }
}

/// <summary>
/// Exception thrown when backup or restore operations fail.
/// </summary>
public class InjectionBackupException : InjectionException
{
    /// <summary>
    /// Gets the file path where the backup error occurred, if applicable.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionBackupException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InjectionBackupException(string message)
        : base($"Injection backup operation failed: {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionBackupException"/> class with file path context.
    /// </summary>
    /// <param name="filePath">The file path where the backup error occurred.</param>
    /// <param name="operation">The backup operation being performed.</param>
    /// <param name="message">The error message.</param>
    public InjectionBackupException(string filePath, string operation, string message)
        : base($"Injection backup {operation} failed for file '{filePath}': {message}")
    {
        FilePath = filePath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionBackupException"/> class with file path context and inner exception.
    /// </summary>
    /// <param name="filePath">The file path where the backup error occurred.</param>
    /// <param name="operation">The backup operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InjectionBackupException(string filePath, string operation, string message, Exception innerException)
        : base($"Injection backup {operation} failed for file '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionBackupException"/> class with installation and file context.
    /// </summary>
    /// <param name="installationId">The installation ID where the backup error occurred.</param>
    /// <param name="filePath">The file path where the backup error occurred.</param>
    /// <param name="operation">The backup operation being performed.</param>
    /// <param name="message">The error message.</param>
    public InjectionBackupException(string installationId, string filePath, string operation, string message)
        : base(installationId, $"backup {operation}", $"File '{filePath}': {message}")
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectionBackupException"/> class with installation, file context, and inner exception.
    /// </summary>
    /// <param name="installationId">The installation ID where the backup error occurred.</param>
    /// <param name="filePath">The file path where the backup error occurred.</param>
    /// <param name="operation">The backup operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InjectionBackupException(string installationId, string filePath, string operation, string message, Exception innerException)
        : base(installationId, $"backup {operation}", $"File '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
    }
}