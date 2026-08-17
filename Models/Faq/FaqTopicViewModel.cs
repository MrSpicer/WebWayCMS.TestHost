using WebWayCMS.TestHost.ViewComponents;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Render model for the routable <c>FaqTopic</c> widget.
/// </summary>
public sealed class FaqTopicViewModel
{
    public FaqTopicConfiguration Configuration { get; init; } = new();

    public IReadOnlyList<FaqViewModel> Faqs { get; init; } = Array.Empty<FaqViewModel>();

    public string Topic { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    /// <summary>The page size actually applied, after the configured value's fallback.</summary>
    public int PageSize { get; init; }

    public int MatchCount { get; init; }
}
