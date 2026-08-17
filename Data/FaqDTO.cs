using WebWayCMS.Data.Models;

namespace WebWayCMS.TestHost.Data;

/// <summary>
/// Host-owned content type demonstrating the host-extensibility seam: a real EF table contributed
/// entirely from this project (no CMS source changes).
/// </summary>
public record FaqDTO : IVersionedContent
{
    public Guid VersionId { get; set; }
    public ContentVersion Version { get; set; } = new();

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
