namespace VivaldiModManager.Core.Exceptions;

/// <summary>
/// Base exception for all Vivaldi-related errors.
/// </summary>
public class VivaldiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiException"/> class.
    /// </summary>
    public VivaldiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public VivaldiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public VivaldiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when no Vivaldi installations are found on the system.
/// </summary>
public class VivaldiNotFoundException : VivaldiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiNotFoundException"/> class.
    /// </summary>
    public VivaldiNotFoundException()
        : base("No Vivaldi installations were found on this system.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VivaldiNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiNotFoundException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VivaldiNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when there's an error detecting Vivaldi installations.
/// </summary>
public class VivaldiDetectionException : VivaldiException
{
    /// <summary>
    /// Gets the path where the detection error occurred, if applicable.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Gets the operation that was being performed when the error occurred.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiDetectionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VivaldiDetectionException(string message)
        : base($"Vivaldi detection failed: {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiDetectionException"/> class with path information.
    /// </summary>
    /// <param name="path">The path where the detection error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    public VivaldiDetectionException(string path, string operation, string message)
        : base($"Vivaldi detection failed during {operation} operation on path '{path}': {message}")
    {
        Path = path;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VivaldiDetectionException"/> class with an inner exception.
    /// </summary>
    /// <param name="path">The path where the detection error occurred.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VivaldiDetectionException(string path, string operation, string message, Exception innerException)
        : base($"Vivaldi detection failed during {operation} operation on path '{path}': {message}", innerException)
    {
        Path = path;
        Operation = operation;
    }
}