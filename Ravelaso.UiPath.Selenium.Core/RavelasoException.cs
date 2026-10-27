using System;

namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Exception thrown by the Ravelaso.Selenium wrapper when an operation cannot be
/// completed (e.g. a hover target is not found, or a forced click does not take
/// effect). Wraps the underlying Selenium exception when available.
/// </summary>
public class RavelasoException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="RavelasoException"/>.
    /// </summary>
    public RavelasoException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of <see cref="RavelasoException"/> with an inner exception.
    /// </summary>
    public RavelasoException(string message, Exception innerException) : base(message, innerException) { }
}