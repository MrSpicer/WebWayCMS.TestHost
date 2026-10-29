namespace WebWayCMS.TestHost.Models.Faq;

public sealed class FaqItemViewModel
{
    public Guid NodeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public string TopicTitle { get; set; } = string.Empty;
    public DateTime CreationDate { get; init; }
    public DateTime ModificationDate { get; init; }
}
