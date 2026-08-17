using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.Pages;

/// <summary>
/// Configuration for the <c>ZoneShowcasePage</c> page type. Deliberately small — the config-heavy
/// surface belongs to <see cref="ConfiguredPageConfiguration"/>.
/// </summary>
public class ZoneShowcasePageConfiguration
{
    [FormProperty(
        Label = "Heading",
        EditorType = EditorType.Text,
        Order = 1,
        FormComponent = "Text"
    )]
    public string Heading { get; set; } = string.Empty;

    [FormProperty(
        Label = "Show Diagnostics",
        EditorType = EditorType.Checkbox,
        Order = 2,
        FormComponent = "Checkbox"
    )]
    public bool ShowDiagnostics { get; set; }

    [FormProperty(
        Label = "Note",
        EditorType = EditorType.Text,
        Order = 3,
        FormComponent = "Text"
    )]
    public string? Note { get; set; }
}
