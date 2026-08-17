using WebWayCMS.TestHost.ViewComponents;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Render model for the routable <c>FaqDetail</c> widget.
/// </summary>
public sealed class FaqDetailViewModel
{
    public FaqDetailConfiguration Configuration { get; init; } = new();

    public FaqViewModel? Faq { get; init; }

    /// <summary>How the FAQ was resolved: <c>route</c>, <c>config</c>, or <c>none</c>.</summary>
    public string Source { get; init; } = "none";

    public string Slug { get; init; } = string.Empty;
}
