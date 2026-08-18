using WebWayCMS.Models;

namespace WebWayCMS.TestHost.Models.Faq;

public sealed class FaqViewModel : BaseContentViewModel
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public int HelpfulnessRating { get; init; }
}
