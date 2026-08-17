using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.Pages;

/// <summary>
/// Configuration for the <c>PageTestHub</c> page type. The <c>Intro</c> rich-text field doubles as
/// the visible demonstration of the unsanitized-page-config gap (rich text inside
/// <c>ConfigurationJson</c> is not sanitized on save).
/// </summary>
public class PageTestHubConfiguration
{
    [FormProperty(
        Label = "Intro",
        EditorType = EditorType.RichText,
        Order = 1,
        FormComponent = "RichText"
    )]
    public string? Intro { get; set; }

    [FormProperty(
        Label = "Show Diagnostics",
        EditorType = EditorType.Checkbox,
        Order = 2,
        FormComponent = "Checkbox"
    )]
    public bool ShowDiagnostics { get; set; }
}
