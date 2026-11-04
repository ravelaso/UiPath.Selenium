// ReSharper disable CheckNamespace

namespace Ravelaso.UiPath.Selenium.Core;

public partial class RavelasoWebClient
{
    /// <summary>
    /// Resolves an anchor using this client's driver.
    /// </summary>
    public Locator ResolveAnchor(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return timeout.HasValue
            ? anchor.WithTimeout(timeout.Value).Resolve(this)
            : anchor.Resolve(this);
    }

    public void Click(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        Click(ResolveAnchor(anchor, timeout), timeout);
    }

    public void ClickJs(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        ClickJs(ResolveAnchor(anchor, timeout), timeout);
    }

    public void ClickHidden(AnchorLocator anchor, Locator? hoverTarget = null, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        ClickHidden(ResolveAnchor(anchor, timeout), hoverTarget, timeout);
    }

    public void DoubleClick(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        DoubleClick(ResolveAnchor(anchor, timeout), timeout);
    }

    public void Type(AnchorLocator anchor, string text, bool clear = true, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        Type(ResolveAnchor(anchor, timeout), text, clear, timeout);
    }

    public string GetText(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return GetText(ResolveAnchor(anchor, timeout), timeout);
    }

    public string? GetAttribute(AnchorLocator anchor, string attributeName, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return GetAttribute(ResolveAnchor(anchor, timeout), attributeName, timeout);
    }

    public bool IsDisplayed(AnchorLocator anchor, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return IsDisplayed(ResolveAnchor(anchor, timeout), timeout);
    }

    public string DownloadFile(AnchorLocator anchor, string destinationPath, TimeSpan? beforeStepDelay = null, TimeSpan? timeout = null)
    {
        if (anchor == null)
            throw new ArgumentNullException(nameof(anchor));

        return DownloadFile(ResolveAnchor(anchor, timeout), destinationPath, timeout);
    }
}

