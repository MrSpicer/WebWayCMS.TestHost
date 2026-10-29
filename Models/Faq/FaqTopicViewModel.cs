using WebWayCMS.TestHost.ViewComponents;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Render model for the routable <c>FaqTopic</c> widget.
/// </summary>
public sealed class FaqTopicViewModel
{
    public FaqTopicConfiguration Configuration { get; init; } = new();

    public IReadOnlyList<FaqViewModel> Faqs { get; init; } = Array.Empty<FaqViewModel>();

    /// <summary>The <c>{topic}</c> route segment, as requested.</summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>The resolved topic's title; empty when no visible topic has that slug.</summary>
    public string TopicTitle { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    /// <summary>The page size actually applied, after the configured value's fallback.</summary>
    public int PageSize { get; init; }

    public int MatchCount { get; init; }
}
