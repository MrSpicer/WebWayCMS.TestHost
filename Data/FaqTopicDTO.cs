using WebWayCMS.Data.Models;

namespace WebWayCMS.TestHost.Data;

/// <summary>
/// Host-owned content type that groups FAQs. A FAQ names its topic through a containment relation
/// (<see cref="Models.Faq.FaqUpsertViewModel.FaqTopicId"/>), so this table has no FAQ column and the
/// FAQ table has no topic column: the relation engine stores, validates, hides and cascades it.
/// </summary>
public record FaqTopicDTO : IVersionedContent
{
    public Guid VersionId { get; set; }
    public ContentVersion Version { get; set; } = new();

    public string Description { get; set; } = string.Empty;
}
