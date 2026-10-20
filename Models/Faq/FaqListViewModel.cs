using WebWayCMS.TestHost.ViewComponents;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Render model for the <c>Faq</c> widget in list mode: the typed configuration plus the resolved FAQ list.
/// </summary>
public sealed class FaqListViewModel
{
    public FaqConfiguration Configuration { get; init; } = new();

    public IReadOnlyList<FaqViewModel> Faqs { get; init; } = Array.Empty<FaqViewModel>();

    public FaqViewModel? FeaturedFaq { get; init; }
}
