namespace WebWayCMS.TestHost.Models.Faq;

public sealed class FaqTopicItemViewModel
{
    public Guid NodeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public DateTime ModificationDate { get; init; }
    public int FaqCount { get; set; }
}
