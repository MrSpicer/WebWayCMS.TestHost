using WebWayCMS.Attributes;

namespace WebWayCMS.TestHost.ViewComponents;

/// <summary>
/// Configuration for the routable <c>FaqTopic</c> widget.
/// </summary>
public class FaqTopicConfiguration
{
    [FormProperty(
        Label = "Page Size",
        EditorType = EditorType.Number,
        Min = 1,
        Max = 50,
        Order = 1,
        FormComponent = "Number"
    )]
    // Nullable deliberately — see the note on FaqListConfiguration.MaxItems: a null from an empty
    // number field would otherwise fail deserialization and drop the whole configuration.
    public int? PageSize { get; set; } = 10;

    [FormProperty(
        Label = "View",
        EditorType = EditorType.ViewPicker,
        ViewComponentName = "FaqTopic",
        Order = 2,
        FormComponent = "ViewPicker"
    )]
    public string? ViewName { get; set; }

    [FormProperty(
        Label = "Empty Message",
        EditorType = EditorType.Text,
        Order = 3,
        FormComponent = "Text"
    )]
    public string? EmptyMessage { get; set; }
}
