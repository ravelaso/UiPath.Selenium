namespace Ravelaso.UiPath.Selenium.Core;

/// <summary>
/// Defines the spatial relationship between an anchor element and a target element.
/// Used by the Anchor System to determine where to search for the target relative to the anchor.
/// </summary>
public enum Direction
{
    /// <summary>
    /// Target is positioned to the right of the anchor.
    /// Searches using following-sibling or position-based XPath.
    /// </summary>
    Right = 0,

    /// <summary>
    /// Target is positioned to the left of the anchor.
    /// Searches using preceding-sibling or position-based XPath.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Target is positioned below the anchor.
    /// Searches in descendants that are positioned vertically below.
    /// </summary>
    Below = 2,

    /// <summary>
    /// Target is positioned above the anchor.
    /// Searches in ancestors or preceding elements positioned vertically above.
    /// </summary>
    Above = 3,

    /// <summary>
    /// Target is in the same row as the anchor (HTML &lt;tr&gt; element).
    /// Searches within the same table row parent.
    /// </summary>
    SameRow = 4,

    /// <summary>
    /// Target is in the same container/parent as the anchor.
    /// Searches within the nearest common ancestor div or container.
    /// </summary>
    SameContainer = 5,

    /// <summary>
    /// Target is a direct or close child of the anchor.
    /// Searches in descendant elements, prioritizing direct children.
    /// </summary>
    Innermost = 6,

    /// <summary>
    /// Target can be anywhere in the document (fallback).
    /// Last resort search without positional constraints.
    /// </summary>
    Anywhere = 7
}
