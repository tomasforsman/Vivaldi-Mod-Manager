namespace VivaldiModManager.Core.Exceptions;

/// <summary>
/// Base exception for all loader-related errors.
/// </summary>
public class LoaderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderException"/> class.
    /// </summary>
    public LoaderException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LoaderException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LoaderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when there's an error generating the loader JavaScript.
/// </summary>
public class LoaderGenerationException : LoaderException
{
    /// <summary>
    /// Gets the path where the loader was being generated.
    /// </summary>
    public string? LoaderPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderGenerationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public LoaderGenerationException(string message)
        : base($"Loader generation failed: {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderGenerationException"/> class with a loader path.
    /// </summary>
    /// <param name="loaderPath">The path where the loader was being generated.</param>
    /// <param name="message">The error message.</param>
    public LoaderGenerationException(string loaderPath, string message)
        : base($"Loader generation failed at path '{loaderPath}': {message}")
    {
        LoaderPath = loaderPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderGenerationException"/> class with an inner exception.
    /// </summary>
    /// <param name="loaderPath">The path where the loader was being generated.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public LoaderGenerationException(string loaderPath, string message, Exception innerException)
        : base($"Loader generation failed at path '{loaderPath}': {message}", innerException)
    {
        LoaderPath = loaderPath;
    }
}

/// <summary>
/// Exception thrown when loader content validation fails.
/// </summary>
public class LoaderValidationException : LoaderException
{
    /// <summary>
    /// Gets the validation errors that occurred.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderValidationException"/> class.
    /// </summary>
    /// <param name="validationErrors">The validation errors that occurred.</param>
    public LoaderValidationException(IEnumerable<string> validationErrors)
        : base($"Loader validation failed with {validationErrors?.Count() ?? 0} error(s)")
    {
        ValidationErrors = validationErrors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoaderValidationException"/> class with a single error.
    /// </summary>
    /// <param name="validationError">The validation error that occurred.</param>
    public LoaderValidationException(string validationError)
        : base($"Loader validation failed: {validationError}")
    {
        ValidationErrors = new List<string> { validationError }.AsReadOnly();
    }
}